using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PoshCode.Controls
{
    /// <summary>
    /// The ConsoleControl is a <see cref="FlowDocumentScrollViewer"/> where all input goes to a sub-textbox after the "prompt"
    /// </summary>
    public partial class ConsoleControl : FlowDocumentScrollViewer, ISupportInitialize, IDisposable
    {
        public static TraceSource TabExpansionTrace = new TraceSource("TabExpansion");
        //static readonly ConsoleBrushes ConsoleBrushes = new ConsoleBrushes();

        static ConsoleControl()
        {
            InitializeCommands();
            // initialize the brushes on our thread...
            Dispatcher.CurrentDispatcher.InvokeAsync(ConsoleBrushes.Refresh);

            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConsoleControl), new FrameworkPropertyMetadata(typeof(ConsoleControl)));
        }


        bool _waitingForKey;
        LinkedList<TextCompositionEventArgs> _textBuffer = new LinkedList<TextCompositionEventArgs>();
        readonly Queue<KeyInfo> _inputBuffer = new Queue<KeyInfo>();

        private readonly RichTextBox _commandBox;
        private readonly PasswordBox _passwordBox;
        private readonly InlineUIContainer _commandContainer;
        // TODO: this should be internal
        public Paragraph Current { get; private set; }
        // TODO: this should be internal
        public Paragraph Next { get; private set; }

        

        public ConsoleControl()
        {
            _commandBox = new RichTextBox
            {
                IsEnabled = true,
                Focusable = true,
                AcceptsTab = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Padding = new Thickness(0.0)
            };
            _popup = new PopupMenu(this);
            _expansion = new TabExpansion();
            _cmdHistory = new CommandHistory();

            // Add the popup to the logical branch of the console so keystrokes can be
            // processed from the popup by the console for the tab-complete scenario.
            // E.G.: $Host.Pri[tab].  => "$Host.PrivateData." instead of swallowing the period.
            AddLogicalChild(_popup);

            // _commandBox.Document.TextAlignment = TextAlignment.Left;
            _passwordBox = new PasswordBox
            {
                                  IsEnabled = true,
                                  Focusable = true
                              };
            _commandBox.PreviewKeyDown += _commandBox_PreviewKeyDown;
            _passwordBox.PreviewKeyDown += _passwordBox_PreviewKeyDown;

            _commandContainer = new InlineUIContainer(_commandBox) { BaselineAlignment = BaselineAlignment.Top }; //.TextTop

            //ScrollViewer.SizeChanged += new SizeChangedEventHandler(ScrollViewer_SizeChanged);

            this.Dispatcher.ShutdownStarted += (sender, args) =>
            {
                Dispose();
            };
        }


        private static PSInvalidCastException TryConvertTo(Type type, string input, out object output)
        {
            // default to string, that seems to be what PowerShell does
            output = input;
            try
            {
                output = LanguagePrimitives.ConvertTo(input, type, CultureInfo.InvariantCulture);
                return null;
            }
            catch (PSInvalidCastException ice)
            {
                // Write(_brushes.ErrorForeground, _brushes.ErrorBackground, ice.Message );
                return ice;
            }
        }
        void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPrompt();
        }

        void ISupportInitialize.EndInit()
        {
            // Pre-call the base init code, so it will be finished ...
            base.EndInit();
            UpdateCharacterWidth();

            //// Initialize the document ...

            Current = new Paragraph { ClearFloaters = WrapDirection.Both };

            Document.Blocks.Add(Current);
            // We need to crush the PagePadding so that the "Width" values work...
            Document.PagePadding = new Thickness(0.0);
            // Default the text alignment correctly
            Document.TextAlignment = TextAlignment.Left;
            //   IsOptimalParagraphEnabled = true,
            //};

            // create the prompt, and stick the command block in it
            Next = new Paragraph();
            Document.Blocks.Add(Next);
            Next.Inlines.Add(_commandContainer);
            _commandContainer.Focus();

            // we have to (manually) bind the document and _commandBox values to the "real" ones...
            BindingOperations.SetBinding(Document, FlowDocument.FontFamilyProperty, new Binding("FontFamily") { Source = this });
            BindingOperations.SetBinding(Document, FlowDocument.FontSizeProperty, new Binding("FontSize") { Source = this });

            BindingOperations.SetBinding(Document, FlowDocument.BackgroundProperty, new Binding("Background") { Source = this });
            BindingOperations.SetBinding(Document, FlowDocument.ForegroundProperty, new Binding("Foreground") { Source = this });

            // BindingOperations.SetBinding(_commandBox, Control.BackgroundProperty, new Binding("Background") { Source = this });
            // BindingOperations.SetBinding(_commandBox, Control.ForegroundProperty, new Binding("Foreground") { Source = this });

            // find the ScrollViewer, but first, make sure the templates are applied (why didn't this already happen?)
            ApplyTemplate();
            _scrollViewer = Template.FindName("PART_ContentHost", this) as ScrollViewer;
        }

        ScrollViewer _scrollViewer;
        private ScrollViewer ScrollViewer
        {
            get { return _scrollViewer ?? (_scrollViewer = Template.FindName("PART_ContentHost", this) as ScrollViewer); }
        }

        //private void NewParagraph()
        //{

        //}
        public void Prompt(string prompt)
        {
            //Dispatcher.ExitAllFrames();
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
            {
                if (Next != null)
                {
                    var insert = new Run(prompt)
                    {
                        Background = Background,
                        Foreground = Foreground
                    };
                    Next.Inlines.Add(insert);
                    SetPrompt();
                }
            });

        }

        public KeyInfo ReadKey(ReadKeyOptions options)
        {
            if ((options & (ReadKeyOptions.IncludeKeyUp | ReadKeyOptions.IncludeKeyDown)) == 0)
            {
                throw new MethodInvocationException("Cannot read key options. To read options either IncludeKeyDown, IncludeKeyUp or both must be set.");
            }

            while (true)
            {
                if (_inputBuffer.Count == 0)
                {
                    Dispatcher.BeginInvoke((Action)(SetPrompt));
                    _waitingForKey = true;
                    _gotInputKey.Reset();
                    _gotInputKey.WaitOne();
                    _waitingForKey = false;
                }
                else
                {

                    var ki = _inputBuffer.Dequeue();
                    if (ki.Character != 0)
                    {
                        Dispatcher.BeginInvoke(
                           (Action)(() => ShouldEcho(ki.Character, (options & ReadKeyOptions.NoEcho) == 0)));
                    }

                    if ((((options & ReadKeyOptions.IncludeKeyDown) > 0) && ki.KeyDown) ||
                        (((options & ReadKeyOptions.IncludeKeyUp) > 0) && !ki.KeyDown))
                    {
                        return ki;
                    }
                }
            }
        }

        private void ShouldEcho(char ch, bool echo)
        {
            var cmd = CurrentCommand;
            if (cmd.Length > 0)
            {
                if (ch == cmd[0])
                {
                    // emulate NoEcho by UN-echoing...
                    CurrentCommand = cmd.Length > 1 ? cmd.Substring(1) : "";
                    // if we're NOT NoEcho, then re-echo it:
                    if (echo)
                    {
                        Write(null, null, new string(ch, 1));
                    }
                }
            }
        }

        /// <summary>
        /// Implements the CLS command the way most command-lines do:
        /// Scroll the Window until the prompt is at the top ...
        /// (as opposed to clearing the screen and leaving the prompt at the bottom)
        /// </summary>        
        public void ClearScreen()
        {
            CompleteBackgroundWorkItems();

            if (Dispatcher.CheckAccess())
            {
                Current.Inlines.Remove(_commandContainer);
                Document.Blocks.Clear();
                //new TextRange(Document.ContentStart, Document.ContentEnd).Text = String.Empty;

                Current = Next = new Paragraph(_commandContainer);
                Document.Blocks.Add(Next);
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    Current.Inlines.Remove(_commandContainer);
                    Document.Blocks.Clear();
                    //new TextRange(Document.ContentStart, Document.ContentEnd).Text = String.Empty;

                    Current = Next = new Paragraph(_commandContainer);
                    Document.Blocks.Add(Next);
                }));
            }
        }

        #region Should Be Internal
        // TODO: this should be internal
        public void SetPrompt()
        {
            Dispatcher.VerifyAccess();
            // the problem is, the prompt might have used Write-Host
            // so we need to move the _commandContainer to the end.
            lock (_commandContainer)
            {
                Next.Inlines.Remove(_commandContainer);

                ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10,
                 ScrollViewer.ViewportWidth - Next.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
                Next.Inlines.Add(_commandContainer);
            }

            UpdateLayout();
            //ScrollViewer.ScrollToBottom();
            _commandContainer.Child.Focus(); // Notice this is "whichever" is active ;)
        }

        public void FlushInputBuffer()
        {
            CompleteBackgroundWorkItems();
            if (Dispatcher.CheckAccess())
            {
                _commandBox.Document.Blocks.Clear();
                _passwordBox.Clear();
                _inputBuffer.Clear();
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    _commandBox.Document.Blocks.Clear();
                    _passwordBox.Clear();
                    _inputBuffer.Clear();
                }));
            }
        }

        #endregion Should Be Internal

        #region Color Dependency Properties
        /// <summary>
        /// Get and set the background color of text to be written.
        /// This maps pretty directly onto the corresponding .NET Console property.
        /// </summary>
        public ConsoleColor BackgroundColor
        {
            get
            {
                return (ConsoleColor)GetValue(BackgroundColorProperty);
            }
            set
            {
                SetValue(BackgroundColorProperty, value);
            }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(ConsoleColor), typeof(ConsoleControl),
            new FrameworkPropertyMetadata(ConsoleColor.Black, FrameworkPropertyMetadataOptions.None,
            BackgroundColorPropertyChanged));

        private static void BackgroundColorPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            // put code here to handle the property changed for BackgroundColor
            var consoleControlObj = depObj as ConsoleControl;
            if (consoleControlObj != null)
            {
                consoleControlObj.Background = e.NewValue != DependencyProperty.UnsetValue
                                                    ? ConsoleBrushes.BrushFromConsoleColor((ConsoleColor)e.NewValue) 
                                                    : ConsoleBrushes.DefaultBackground;
            }
        }


        /// <summary>
        /// Get and set the Foreground color of text to be written.
        /// This maps pretty directly onto the corresponding .NET Console property.
        /// </summary>
        public ConsoleColor ForegroundColor
        {
            get
            {
                return (ConsoleColor)GetValue(ForegroundColorProperty);
            }
            set
            {
                SetValue(ForegroundColorProperty, value);
            }
        }

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register("ForegroundColor", typeof(ConsoleColor), typeof(ConsoleControl),
            new FrameworkPropertyMetadata(ConsoleColor.White, FrameworkPropertyMetadataOptions.None,
            ForegroundColorPropertyChanged));

        private static void ForegroundColorPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {

            // put code here to handle the property changed for ForegroundColor
            var consoleControlObj = depObj as ConsoleControl;
            if (consoleControlObj != null)
            {
                consoleControlObj.Foreground = e.NewValue != DependencyProperty.UnsetValue 
                                                    ? ConsoleBrushes.BrushFromConsoleColor((ConsoleColor)e.NewValue) 
                                                    : ConsoleBrushes.DefaultForeground;
            }
        }

        #endregion Color Dependency properties


        public void Write(string message, Block target = null)
        {
            //if (target == null) target = Current;
            // Write is Dispatcher checked
            Write(null, null, message, target);
        }

        [SuppressMessage("RefactoringEssentials", "RECS0026", Justification = "Creating a Run with the target set puts it in the document automatically")]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void Write(Brush foreground, Brush background, string text, Block target = null)
        {
            //if (target == null) target = Current;

            Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
            {
                // handle null values so that other methods don't have to 
                // use Dispatchers just to look up a color
                if (foreground == null) foreground = Foreground;
                if (background == null) background = Background;//Brushes.Transparent;

                if (target == null) target = Current;

                // Creating the run with the target set puts it in the document automatically.
                new Run(text, target.ContentEnd)
                {
                    Background = background,
                    Foreground = foreground
                };
                ScrollViewer.ScrollToBottom();
                // _commandContainer.BringIntoView();
            });
        }

        [SuppressMessage("RefactoringEssentials", "RECS0026", Justification = "Creating a Run with the target set puts it in the document automatically")]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void Write(ConsoleColor foreground, ConsoleColor background, string text, Block target = null)
        {
            if (target == null) target = Current;

            Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
            {
                // Creating the run with the target set puts it in the document automatically.
                new Run(text, target.ContentEnd)
                {
                    Background = (BackgroundColor == background) ? ConsoleBrushes.Transparent : ConsoleBrushes.BrushFromConsoleColor(background),
                    Foreground = ConsoleBrushes.BrushFromConsoleColor(foreground)
                };
                ScrollViewer.ScrollToBottom();
            });
        }


        /// <summary>
        /// Provides a way for scripts to request user input ...
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            Dispatcher.Invoke(() =>
            {
                lock (_commandContainer)
                {
                    UpdateLayout();
                    Next.Inlines.Remove(_commandContainer);
                    ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - Current.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
                    Current.Inlines.Add(_commandContainer);
                    UpdateLayout();
                    _commandContainer.Child.Focus();
                }
            }, DispatcherPriority.Render);
            Thread.Sleep(0);
            WaitingForInput = true;
            _gotInputLine.Reset();
            _gotInputLine.WaitOne();
            WaitingForInput = false;

            Dispatcher.Invoke(() =>
            {
                lock (_commandContainer)
                {
                    Current.Inlines.Remove(_commandContainer);
                    ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - Next.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
                    Next.Inlines.Add(_commandContainer);
                    _commandContainer.Child.Focus();
                    UpdateLayout();
                }
            }, DispatcherPriority.Render);
            return _lastInputString ?? String.Empty;
        }

        public SecureString ReadLineAsSecureString()
        {
            Dispatcher.Invoke(() =>
            {
                lock (_commandContainer)
                {
                    UpdateLayout();
                    _commandContainer.Child = _passwordBox;
                    Next.Inlines.Remove(_commandContainer);
                    ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - Current.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
                    Current.Inlines.Add(_commandContainer);
                    UpdateLayout();
                    _commandContainer.Child.Focus();
                }
            }, DispatcherPriority.Render);

            Thread.Sleep(0);
            WaitingForInput = true;
            _gotInputLine.Reset();
            _gotInputLine.WaitOne();
            WaitingForInput = false;

            Dispatcher.Invoke(() =>
            {
                lock (_commandContainer)
                {
                    _commandContainer.Child = _commandBox;
                    Current.Inlines.Remove(_commandContainer);
                    ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - Next.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
                    Next.Inlines.Add(_commandContainer);
                    _commandContainer.Child.Focus();
                    UpdateLayout();
                }
            }, DispatcherPriority.Render);

            return _lastPassword;
        }

        private readonly AutoResetEvent _gotInputKey = new AutoResetEvent(false);
        private readonly AutoResetEvent _gotInputLine = new AutoResetEvent(false);
        private string _lastInputString;
        private SecureString _lastPassword;

        public bool WaitingForInput;

        // the PopupMenu uses these two things...
        private TabExpansion _expansion;
        private CommandHistory _cmdHistory;
        private readonly PopupMenu _popup;
        private DateTime _tabTime;

        public CommandHistory History
        {
            get { return _cmdHistory; }
            set { _cmdHistory = value; }
        }

        public TabExpansion Expander
        {
            get { return _expansion; }
            set { _expansion = value; }
        }


        /// <summary>Handles the CommandEntered event of the Console buffer</summary>
        /// <param name="command">The command.</param>
        protected virtual void OnCommand(CommandEventArgs command)
        {
            if (Command == null) return;

            Command(this, command);
            _cmdHistory.Add(command.Command.TrimEnd());
            Trace.WriteLine("OnCommand, clearing KeyInfo queue.");
        }

        public int CurrentCommandCursorPos
        {
            get
            {
                return _commandBox.CaretPosition.GetTextRunLength(LogicalDirection.Backward);
            }
            set
            {
                _commandBox.CaretPosition = _commandBox.Document.ContentStart.GetPositionAtOffset(value);                
            }
        }

        public string CurrentCommandPreCursor
        {
            get
            {
                var preCursor = new TextRange(_commandBox.Document.ContentStart, _commandBox.CaretPosition);
                return preCursor.Text.TrimEnd('\n', '\r');
            }
            set
            {
                var preCursor = new TextRange(_commandBox.Document.ContentStart, _commandBox.CaretPosition);
                // TODO: re-parse and syntax highlight
                preCursor.Text = value.TrimEnd('\n', '\r');
            }
        }
        public string CurrentCommandPostCursor
        {
            get
            {
                var postCursor = new TextRange(_commandBox.CaretPosition, _commandBox.Document.ContentEnd);
                return postCursor.Text.TrimEnd('\n', '\r');
            }
            set
            {
                var postCursor = new TextRange(_commandBox.CaretPosition, _commandBox.Document.ContentEnd);
                // TODO: re-parse and syntax highlight
                postCursor.Text = value.TrimEnd('\n', '\r');

            }
        }

        public string CurrentCommand
        {
            get
            {
                var all = new TextRange(_commandBox.Document.ContentStart, _commandBox.Document.ContentEnd);
                return all.Text.TrimEnd('\n', '\r');
            }
            set
            {
                _commandBox.Document.Blocks.Clear();
                // TODO: re-parse and syntax highlight
                _commandBox.Document.ContentStart.InsertTextInRun(value.TrimEnd('\n', '\r'));
                _commandBox.CaretPosition = _commandBox.Document.ContentEnd;
            }
        }

        private int CurrentCommandLineCountPreCursor
        {
            get
            {
                var lineCount = _commandBox.Document.Blocks.Count;
                if (lineCount > 0)
                {
                    _commandBox.CaretPosition.GetLineStartPosition(int.MinValue, out lineCount);
                    lineCount--;
                }
                else { lineCount = 1; }
                return Math.Abs(lineCount);
            }
        }
        private int CurrentCommandLineCountPostCursor
        {
            get
            {
                var lineCount = _commandBox.Document.Blocks.Count;
                if (lineCount > 0)
                {
                    _commandBox.CaretPosition.GetLineStartPosition(int.MaxValue, out lineCount);
                    lineCount++; // because we're about to be on the next line ...
                }
                else { lineCount = 1; }
                return Math.Abs(lineCount);
            }
        }
        private int CurrentCommandLineCount
        {
            get
            {
                var lineCount = _commandBox.Document.Blocks.Count;
                if (lineCount > 0)
                {
                    _commandBox.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(int.MaxValue, out lineCount);
                    lineCount++;
                }
                else { lineCount = 1; }
                return lineCount;
            }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }


        public Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            if (!string.IsNullOrEmpty(caption))
                Write(caption + "\n");
            if (!string.IsNullOrEmpty(message))
                Write(message + "\n");

            var results = new Dictionary<string, PSObject>();
            foreach (var fd in descriptions)
            {
                Type type = Type.GetType(fd.ParameterAssemblyFullName);

                string prompt = string.IsNullOrEmpty(fd.Label) ? fd.Name : fd.Label;

                if (type != null && type.IsArray)
                {
                    type = type.GetElementType();
                    var output = new List<PSObject>();
                    int count = 0;
                    do
                    {
                        PSObject single = GetSingle(caption, message, string.Format("{0}[{1}]", prompt, count++),
                                                      fd.HelpMessage, fd.DefaultValue, type);
                        if (single == null) break;

                        if (!(single.BaseObject is string) || ((string)single.BaseObject).Length > 0)
                        {
                            output.Add(single);
                        }
                        else break;
                    } while (true);

                    results[fd.Name] = PSObject.AsPSObject(output.ToArray());
                }
                else
                {
                    results[fd.Name] = GetSingle(caption, message, prompt, fd.HelpMessage, fd.DefaultValue, type);
                }

            }
            return results;
        }
        private PSObject GetSingle(string caption, string message, string prompt, string help, PSObject psDefault, Type type)
        {
            if (null != type && type == typeof(PSCredential))
            {
                return PSObject.AsPSObject(PromptForCredentialInline(caption, message, String.Empty, prompt));
            }

            while (true)
            {
                // TODO: Only show the help message if they type '?' as their entry something, in which case show help and re-prompt.
                if (!String.IsNullOrEmpty(help))
                    Write(ConsoleBrushes.ConsoleColorFromBrush(ConsoleBrushes.VerboseForeground), ConsoleBrushes.ConsoleColorFromBrush(ConsoleBrushes.VerboseBackground), help + "\n");

                Write($"{prompt}: ");

                if (null != type && typeof(SecureString) == type)
                {
                    var userData = ReadLineAsSecureString() ?? new SecureString();
                    return PSObject.AsPSObject(userData);
                } // Note: This doesn't look the way it does in PowerShell, but it should work :)
                else
                {
                    if (psDefault != null && psDefault.ToString().Length > 0)
                    {
                        if (Dispatcher.CheckAccess())
                        {
                            CurrentCommand = psDefault.ToString();
                            _commandBox.SelectAll();
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action<string>)(def =>
                            {
                                CurrentCommand = def;
                                _commandBox.SelectAll();
                            }), psDefault.ToString());
                        }
                    }

                    var userData = ReadLine();

                    if (type != null && userData.Length > 0)
                    {
                        object output;
                        var ice = TryConvertTo(type, userData, out output);
                        // Special exceptions that happen when casting to numbers and such ...
                        if (ice == null)
                        {
                            return PSObject.AsPSObject(output);
                        }
                        if ((ice.InnerException is FormatException) || (ice.InnerException is OverflowException))
                        {
                            Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground,
                                $@"Cannot recognize ""{userData}"" as a {type.FullName} due to a format error.\n");
                        }
                        else
                        {
                            return PSObject.AsPSObject(String.Empty);
                        }
                    }
                    else if (userData.Length == 0)
                    {
                        return PSObject.AsPSObject(String.Empty);
                    }
                    else return PSObject.AsPSObject(userData);
                }
            }
        }

        public int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            // Write the caption and message strings in Blue.
            Write(ConsoleColor.Blue, ConsoleColor.Black, caption + "\n" + message + "\n");

            // Convert the choice collection into something that's a little easier to work with
            // See the BuildHotkeysAndPlainLabels method for details.
            var promptData = BuildHotkeysAndPlainLabels(choices, true);


            // Loop reading prompts until a match is made, the default is
            // chosen or the loop is interrupted with ctrl-C.
            while (true)
            {

                // Format the overall choice prompt string to display...
                for (int element = 0; element < promptData.GetLength(1); element++)
                {
                    if (element == defaultChoice)
                    {
                        Write(ConsoleBrushes.VerboseForeground, ConsoleBrushes.VerboseBackground,
                            $"[{promptData[0, element]}] {promptData[1, element]}  ");
                    }
                    else
                    {
                        Write(null, null, $"[{promptData[0, element]}] {promptData[1, element]}  ");
                    }
                }
                Write(null, null, $"(default is \"{promptData[0, defaultChoice]}\"):");

                string data = ReadLine().Trim().ToUpper();

                // If the choice string was empty, use the default selection.
                if (data.Length == 0)
                    return defaultChoice;

                // See if the selection matched and return the
                // corresponding index if it did...
                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i][0] == data[0])
                        return i;
                }

                // If they picked the very last thing in the list, they want help
                if (promptData.GetLength(1) > choices.Count && promptData[0, choices.Count] == data)
                {
                    // Show help
                    foreach (var choice in choices)
                    {
                        Write($"{choice.Label.Replace("&", "")} - {choice.HelpMessage}\n");
                    }
                }
                else
                {
                    Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, "Invalid choice: " + data + "\n");
                }
            }

        }

        public PSCredential PromptForCredentialInline(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes = PSCredentialTypes.Generic, PSCredentialUIOptions options = PSCredentialUIOptions.None)
        {

            Collection<FieldDescription> fields;

            // NOTE: I'm not sure this is the right action for the PromptForCredential targetName
            if (!String.IsNullOrEmpty(targetName))
            {
                caption = $"Credential for {targetName}\n\n{caption}";
            }

            if ((options & PSCredentialUIOptions.ReadOnlyUserName) == PSCredentialUIOptions.Default)
            {
                var user = new FieldDescription("User");
                user.SetParameterType(typeof(string));
                user.Label = "Username";
                user.DefaultValue = PSObject.AsPSObject(userName);
                user.IsMandatory = true;

                do
                {
                    fields = new Collection<FieldDescription>(new[] { user });
                    var login = Prompt(caption, message, fields);
                    userName = login["User"].BaseObject as string;
                } while (userName != null && userName.Length == 0);
            }

            // I think this is all I can do with the allowedCredentialTypes
            // domain required
            if (allowedCredentialTypes > PSCredentialTypes.Generic)
            {
                // and no domain
                if (userName != null && userName.IndexOfAny(new[] { '\\', '@' }) < 0)
                {
                    userName = $"{targetName}\\{userName}";
                }
            }

            var pass = new FieldDescription("Password");
            pass.SetParameterType(typeof(SecureString));
            pass.Label = "Password for " + userName;
            pass.IsMandatory = true;

            fields = new Collection<FieldDescription>(new[] { pass });
            var password = Prompt(String.Empty, String.Empty, fields);

            // TODO: I'm not sure what to do with the PSCredentialUIOptions options, because PowerShell.exe ignores them
            return new PSCredential(userName, (SecureString)password["Password"].BaseObject);
        }

        /// <summary>
        /// Parse a string containing a hotkey character.
        /// 
        /// Take a string of the form: 
        /// "Yes to &amp;all"
        /// And return a two-dimensional array split out as
        ///    "A", "Yes to all".
        /// </summary>
        /// <param name="input">The string to process</param>
        /// <returns>
        /// A two dimensional array containing the parsed components.
        /// </returns>
        private static string[] GetHotkeyAndLabel(string input)
        {
            // ReSharper disable SuggestUseVarKeywordEvident
            string[] result = { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            // ReSharper restore SuggestUseVarKeywordEvident
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0)
                    result[0] = fragments[1][0].ToString().ToUpper();
                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else
            {
                result[1] = input;
            }
            return result;
        }

        /// <summary>
        /// This is a private worker function that splits out the
        /// accelerator keys from the menu and builds a two dimentional 
        /// array with the first access containing the
        /// accelerator and the second containing the label string
        /// with &amp; removed.
        /// </summary>
        /// <param name="choices">The choice collection to process</param>
        /// <param name="addHelp">Add the 'Help' prompt </param>
        /// <returns>
        /// A two dimensional array containing the accelerator characters
        /// and the cleaned-up labels</returns>
        private static string[,] BuildHotkeysAndPlainLabels(Collection<ChoiceDescription> choices, bool addHelp)
        {
            // Allocate the result array
            var count = addHelp ? choices.Count + 1 : choices.Count;
            var hotkeysAndPlainLabels = new string[2, count];

            for (var i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);

                if (addHelp && hotkeyAndLabel[0] == "?")
                {
                    addHelp = false;
                }
                hotkeysAndPlainLabels[0, i] = hotkeyAndLabel[0];
                hotkeysAndPlainLabels[1, i] = hotkeyAndLabel[1];
            }

            if (addHelp)
            {
                hotkeysAndPlainLabels[0, count - 1] = "?";
                hotkeysAndPlainLabels[1, count - 1] = "Help";  // TODO: Internationalization?
            }

            return hotkeysAndPlainLabels;
        }


        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        // TODO: Check if other properties ought to be backed by dependency properties so they can be bound      
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ConsoleControl), new UIPropertyMetadata("WPF Rich Console"));

        private string _searchString = String.Empty;
        private IEnumerable<TextRange> _searchHits = new TextRange[] { };
        /// <summary>
        /// Find all instances of the given text
        /// </summary>
        /// <param name="input">text to search for (null or empty clears search)</param>
        /// <returns>True if results were found, False otherwise</returns>
        public bool FindText(String input)
        {
            if (string.IsNullOrEmpty(input) || !_searchString.Equals(input, StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var found in _searchHits)
                {
                    found.ClearAllProperties();
                }
                _searchString = input;
            }

            bool result = false;
            if (!string.IsNullOrEmpty(input))
            {
                // Start at the beginning and ...
                _searchHits = DocumentHelper.FindText(Document, input, DocumentHelper.FindFlags.None, CultureInfo.CurrentCulture);
                foreach (var found in _searchHits)
                {
                    result = true;
                    found.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
                    found.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                }
            }
            return result;
        }


        /// <summary>
        /// Find the input string within the document, starting at the specified position.
        /// </summary>
        /// <param name="position">the current text position</param>
        /// <param name="input">input text</param>
        /// <returns>An <see cref="TextRange"/> representing the (next) matching string within the text container. Null if there are no matches.</returns>
        public TextRange FindNext(ref TextPointer position, String input)
        {
            FindText(input);

            foreach (var result in _searchHits)
            {
                if (position.CompareTo(result.End) < 0)
                {
                    position = result.Start;
                    double top = PointToScreen(position.GetLineStartPosition(0).GetCharacterRect(LogicalDirection.Forward).TopLeft).Y + PointFromScreen(new Point(0, 0)).Y;
                    Trace.WriteLine($" Top: {top}, CharOffset: {position}");
                    ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + top);
                    position = result.End;
                    return result;
                }
            }
            return null;
        }



        /// <summary>
        /// An ugly internal search helper (which uses reflection, and "just works")
        /// </summary>
        private static class DocumentHelper
        {
            private static MethodInfo findMethod;
            [Flags]
            public enum FindFlags
            {
                None = 0,
                MatchCase = 1,
                FindInReverse = 2,
                FindWholeWordsOnly = 4,
                MatchDiacritics = 8,
                MatchKashida = 16,
                MatchAlefHamza = 32
            }

            public static TextRange FindText(TextPointer findContainerStartPosition, TextPointer findContainerEndPosition, String input, FindFlags flags, CultureInfo cultureInfo)
            {
                TextRange textRange = null;
                if (findContainerStartPosition.CompareTo(findContainerEndPosition) < 0)
                {
                    try
                    {
                        if (findMethod == null)
                        {
                            findMethod = typeof(FrameworkElement).Assembly.GetType("System.Windows.Documents.TextFindEngine").
                                   GetMethod("Find", BindingFlags.Static | BindingFlags.Public);
                        }

                        textRange = findMethod.Invoke(null,
                           new object[] { findContainerStartPosition,
                                    findContainerEndPosition,
                                    input, 
                                    flags, 
                                    CultureInfo.CurrentCulture 
                     }) as TextRange;
                    }
                    catch (ApplicationException)
                    {
                        textRange = null;
                    }
                }

                return textRange;
            }

            public static IEnumerable<TextRange> FindText(FlowDocument document, String input, FindFlags flags, CultureInfo cultureInfo)
            {
                TextPointer start = document.ContentStart;
                TextPointer end = document.ContentEnd;
                TextRange last = null;

                try
                {
                    if (findMethod == null)
                    {
                        findMethod = typeof(FrameworkElement).Assembly
                                        .GetType("System.Windows.Documents.TextFindEngine")
                                        .GetMethod("Find", BindingFlags.Static | BindingFlags.Public);
                    }
                }
                catch (ApplicationException)
                {
                    last = null;
                }

                while (findMethod != null && start.CompareTo(end) < 0)
                {
                    try
                    {
                        var parameters = new object[] { start, end, input, flags, CultureInfo.CurrentCulture };
                        last = findMethod.Invoke(null, parameters) as TextRange;
                    }
                    catch (ApplicationException)
                    {
                        last = null;
                    }

                    if (last == null)
                        yield break;

                    yield return last;
                    start = last.End;
                }
            }
        }

        ~ConsoleControl()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                _gotInputKey.Dispose();
                _gotInputLine.Dispose();

            }
            // free native resources (if there are any)
        }
    }
}
