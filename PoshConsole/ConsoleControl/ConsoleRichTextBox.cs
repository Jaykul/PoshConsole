using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Management.Automation.Host;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Management.Automation;
using System.Threading;
using System.Collections.ObjectModel;
using System.Text;
using System.Globalization;
using PoshConsole.Controls;

namespace PoshConsole.Controls
{
    public delegate object Invoke();
    public delegate void BeginInvoke();
    public delegate void WriteOutputDelegate(Brush foreground, Brush background, string text);

    /// <summary>
    /// A derivative of RichTextBox ...
    /// Allow input only at the bottom, in plain text ... 
    /// but do context-sensitive highlighting or "error" highlighting?
    /// <remarks>
    /// Ultimately intended for use as a PowerShell console
    /// </remarks>
    /// </summary>
    public partial class ConsoleRichTextBox : RichTextBox, IPoshConsoleControl, IPSRawConsole  //, IPSConsole, IConsoleControlBuffered
    {
        // events and such ...

        static ConsoleBrushList _consoleBrushes = new ConsoleBrushList();


        private TabExpansion _expansion;
        private CommandHistory _cmdHistory;
        private PopupMenu _popup;

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

        /// <summary>
        /// Static initialization of the <see cref="ConsoleRichTextBox"/> class.
        /// </summary>
        static ConsoleRichTextBox()
        {
            NullOutCommands();

            RegisterClipboardCommands();

            CommandManager.RegisterClassCommandBinding(typeof(ConsoleRichTextBox),
                new CommandBinding(ApplicationCommands.Stop,
                new ExecutedRoutedEventHandler(OnApplicationStop)));

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleRichTextBox"/> class.
        /// </summary>
        public ConsoleRichTextBox()
            : base()
        {
            //this.myHistory = new List<string>();
            _expansion = new TabExpansion();
            _cmdHistory = new CommandHistory();

            // add a do-nothing delegate so we don't have to test for it
            ProcessCommand += new CommandHandler(delegate(string cmd) { });

            DataObject.AddPastingHandler(this, OnDataObjectPasting);

            // Set the FontFamily from the settings by hand ... kinda weird, but it seems to work.
            FontFamily = new FontFamily(new Uri("pack://application:,,,/PoshConsole;component/poshconsole.xaml"), Properties.Settings.Default.FontFamily.Source + ",/FontLibrary;Component/#Bitstream Vera Sans Mono, Global Monospace");

            Properties.Colors.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ColorsPropertyChanged);
            Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SettingsPropertyChanged);

            _popup = new PopupMenu(this);
            // Add the popup to the logical branch of the console so keystrokes can be
            // processed from the popup by the console for the tab-complete scenario.
            // E.G.: $Host.Pri[tab].  => "$Host.PrivateData." instead of swallowing the period.
            AddLogicalChild(_popup);
        }

        /// <summary>
        /// Handle the Settings PropertyChange event for fonts
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "FontFamily":
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        FontFamily = new FontFamily(new Uri("pack://application:,,,/PoshConsole;component/poshconsole.xaml"), Properties.Settings.Default.FontFamily.Source + ",/FontLibrary;Component/#Bitstream Vera Sans Mono,Global Monospace");
                    });
                    break;
                case "FontSize":
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        FontSize = Properties.Settings.Default.FontSize;
                    });
                    break;
            }
        }


        ///// <summary>
        ///// Raises the <see cref="E:System.Windows.FrameworkElement.Initialized"></see> event. 
        ///// This method is invoked whenever <see cref="P:System.Windows.FrameworkElement.IsInitialized"></see> is set to true internally.
        ///// </summary>
        ///// <param name="e">The <see cref="T:System.Windows.RoutedEventArgs"></see> that contains the event data.</param>
        //protected override void OnInitialized(EventArgs e)
        //{
        //    base.OnInitialized(e);
        //}


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
            set {
                SetValue(BackgroundColorProperty, value);
            }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(ConsoleColor), typeof(ConsoleRichTextBox),
            new FrameworkPropertyMetadata(ConsoleColor.Black, FrameworkPropertyMetadataOptions.None,
            new PropertyChangedCallback(BackgroundColorPropertyChanged)));

        private static void BackgroundColorPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            // put code here to handle the property changed for BackgroundColor
            ConsoleRichTextBox ConsoleRichTextBoxObj = depObj as ConsoleRichTextBox;
            if (ConsoleRichTextBoxObj != null)
            {
                ConsoleRichTextBoxObj.Background = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)e.NewValue);
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
            DependencyProperty.Register("ForegroundColor", typeof(ConsoleColor), typeof(ConsoleRichTextBox),
            new FrameworkPropertyMetadata(ConsoleColor.White, FrameworkPropertyMetadataOptions.None,
            new PropertyChangedCallback(ForegroundColorPropertyChanged)));

        private static void ForegroundColorPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            // put code here to handle the property changed for ForegroundColor
            ConsoleRichTextBox ConsoleRichTextBoxObj = depObj as ConsoleRichTextBox;
            if (ConsoleRichTextBoxObj != null)
            {
                ConsoleRichTextBoxObj.Foreground = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)e.NewValue);
            }
        }


        /// <summary>
        /// Registers the clipboard commands.
        /// <remarks>
        /// Command handlers for Cut/Copy/Paste:
        /// <list type="">
        /// <item>Paste automatically goes at the bottom (in the "command" line)</item>
        /// <item>And we only paste plain text</item>
        /// <item>CUT only works in the command line</item>
        /// </list>
        /// </remarks>
        /// </summary>
        private static void RegisterClipboardCommands()
        {

            CommandManager.RegisterClassCommandBinding(typeof(ConsoleRichTextBox),
                new CommandBinding(ApplicationCommands.Cut,
                new ExecutedRoutedEventHandler(OnCut),
                new CanExecuteRoutedEventHandler(OnCanExecuteCut)));

            //CommandManager.RegisterClassCommandBinding(typeof(ConsoleRichTextBox),
            //    new CommandBinding(ApplicationCommands.Copy,
            //    new ExecutedRoutedEventHandler(OnCopy),
            //    new CanExecuteRoutedEventHandler(OnCanExecuteCopy)));

            CommandManager.RegisterClassCommandBinding(typeof(ConsoleRichTextBox),
                new CommandBinding(ApplicationCommands.Paste,
                new ExecutedRoutedEventHandler(OnPaste),
                new CanExecuteRoutedEventHandler(OnCanExecutePaste)));

            // TODO: handle dragging & dropping text
        }

        /// <summary>
        /// Null out the formatting commands.
        /// </summary>
        static void NullOutCommands()
        {
            // Disable all formatting by ... not doing anything.
            foreach (RoutedUICommand command in _formattingCommands)
            {
                CommandManager.RegisterClassCommandBinding(typeof(ConsoleRichTextBox),
                    new CommandBinding(command, new ExecutedRoutedEventHandler(OnIgnoredCommand),
                    new CanExecuteRoutedEventHandler(OnCanIgnoreCommand)));
            }
        }

        /// <summary>
        /// Clears the undo buffer by toggling it on and off.
        /// </summary>
        private void ClearUndoBuffer()
        {
            IsUndoEnabled = false;
            IsUndoEnabled = true;
        }

         //------------------------------------------------------
        //
        //  Event Handlers
        //
        //------------------------------------------------------

        //#region Event Handlers

        /// <summary>
        /// Called to check if we can "execute" a command we're ignoring.
        /// Of course, this is always true, since we're always prepared to ignore things...
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="args">The <see cref="System.Windows.Input.CanExecuteRoutedEventArgs"/> instance containing the event data.</param>
        private static void OnCanIgnoreCommand(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        /// <summary>
        /// Called when a command is to be ignored.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private static void OnIgnoredCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true; // Actively ignore it, to make sure the base class doesn't handle it.
        }

        private bool CaretInCommand
        {
            get
            {
                return _promptEnd.IsInSameDocument(Document.ContentEnd) && _promptEnd.GetOffsetToPosition(CaretPosition) >= 1;
            }
        }



        private TextPointer CommandStart
        {
            get
            {
                TextPointer cs = null;
                 
                if (_promptEnd != null)
                {
                    cs = _promptEnd.GetNextInsertionPosition(LogicalDirection.Forward);
                }
                if (cs == null)
                {
                    if (Document.Blocks.LastBlock is Paragraph && (Document.Blocks.LastBlock as Paragraph).Inlines.LastInline != null)
                    {
                        cs = (Document.Blocks.LastBlock as Paragraph).Inlines.LastInline.ContentStart;
                    }
                    else
                    {
                        cs = Document.Blocks.LastBlock.ContentStart;
                    }
                }
                return cs;
            }
        }

        public string CurrentCommand
        {
            get
            {
                // Because we are processing commands based on the document content ...
                // We require a "Yield" to make sure the document has a chance to process
                // any remaining keypresses before we read the command from it.
                System.Threading.Thread.Sleep(0);

                TextRange cmd = new TextRange(CommandStart, _currentParagraph.ContentEnd);
                // Run cmd = _currentParagraph.Inlines.LastInline as Run;

                if (cmd != null)
                {
                    return cmd.Text; //cr.Text
                }
                else
                    return String.Empty;
            }
            set
            {
                //Run cmd = _currentParagraph.Inlines.LastInline as Run;
                TextRange cmd = new TextRange(CommandStart, _currentParagraph.ContentEnd);
                if (cmd != null)
                {
                    cmd.Text = string.Empty;
                }

                Run command = new Run(value, _currentParagraph.ContentEnd.GetInsertionPosition(LogicalDirection.Forward));

                // EndOfPrompt.DeleteTextInRun(EndOfPrompt.GetTextRunLength(LogicalDirection.Forward));
                //((Run)commandStart.Paragraph.Inlines.LastInline)
                //commandStart.DeleteTextInRun(commandStart.GetTextRunLength(LogicalDirection.Forward));
                //commandStart.InsertTextInRun(value);

                CaretPosition = Document.ContentEnd;
            }
        }




        //
        //private string lastWord, tabbing = null;
        //private int tabbingCount = -1;
        //private List<string> completions = null;
        //private int historyIndex = -1;
        ///// <summary>
        ///// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyDown"></see>�attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        ///// </summary>
        ///// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs"></see> that contains the event data.</param>
        //protected override void OnPreviewKeyDown(KeyEventArgs e)
        //{
        //    Trace.TraceInformation("Entering OnPreviewKeyDown:");
        //    Trace.Indent();
        //    Trace.WriteLine("Event:  {0}" + e.RoutedEvent);
        //    Trace.WriteLine("Key:    {0}" + e.Key);
        //    Trace.WriteLine("Source: {0}" + e.Source);
        //    if (null == commandStart)
        //    {
        //        base.OnPreviewKeyDown(e);
        //        Trace.Unindent();
        //        Trace.TraceInformation("Exiting OnPreviewKeyDown:");
        //        return;
        //    }
        //    if (e.Source == intellisense)
        //    {
        //        base.OnPreviewKeyDown(e);
        //        Trace.Unindent();
        //        Trace.TraceInformation("Exiting OnPreviewKeyDown:");
        //        return;
        //    }

        //    if (currentParagraph.Inlines.Count < promptInlines)
        //    {
        //        SetPrompt();
        //        promptInlines = currentParagraph.Inlines.Count;
        //    }

        //    bool inPrompt = CaretInCommand;
        //    // happens when starting up with a slow profile script
        //    switch (e.Key)
        //    {
        //        case Key.Tab:
        //            {
        //                if (inPrompt)
        //                {
        //                    OnTabComplete(((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) ? -1 : 1);
        //                }
        //                e.Handled = true;
        //            } break;
        //        case Key.F7:
        //            {
        //                popupClosing = new EventHandler(History_Closed);
        //                popup.Closed += popupClosing;
        //                ShowPopup(myHistory, true, Properties.Settings.Default.HistoryMenuFilterDupes);
        //            } break;
        //        case Key.Up:
        //            {
        //                if (inPrompt && ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != ModifierKeys.Control))
        //                {
        //                    OnNavigateHistory(+1);
        //                    e.Handled = true;
        //                }
        //            } break;
        //        case Key.Down:
        //            {
        //                if (inPrompt && ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != ModifierKeys.Control))
        //                {
        //                    OnNavigateHistory(-1);
        //                    e.Handled = true;
        //                }
        //            } break;

        //        case Key.Escape:
        //            {
        //                historyIndex = -1;
        //                CurrentCommand = "";
        //            } break;
        //        case Key.Return:
        //            {
        //                IsUndoEnabled = false;
        //                IsUndoEnabled = true;
                        
        //                // keep them in the same (relative) place in the history buffer.
        //                if( historyIndex != 0 ) historyIndex++; 

        //                // the "EndOfOutput" marker gets stuck just before the last character of the prompt...
        //                string cmd = CurrentCommand;
        //                //TextRange tr = new TextRange(currentParagraph.ContentStart.GetPositionAtOffset(promptLength), currentParagraph.ContentEnd);
        //                //string cmd = EndOfPrompt.GetTextInRun(LogicalDirection.Forward);
        //                currentParagraph.ContentEnd.InsertLineBreak();
        //                myHistory.Add(cmd);
        //                OnCommand(cmd);
        //                // let the {ENTER} through...
        //                e.Handled = true;
        //            } break;
        //        case Key.Left:
        //            {
        //                // cancel the "left" if we're at the left edge of the prompt
        //                if (commandStart.GetOffsetToPosition(CaretPosition) >= 0 &&
        //                    commandStart.GetOffsetToPosition(CaretPosition.GetNextInsertionPosition(LogicalDirection.Backward)) < 0)
        //                {
        //                    e.Handled = true;
        //                }
        //            } break;

        //        case Key.Home:
        //            {
        //                // if we're in the command string ... handle it ourselves 
        //                if (inPrompt)
        //                {
        //                    if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        //                    {
        //                        Selection.Select(CaretPosition, commandStart.GetInsertionPosition(LogicalDirection.Forward));
        //                    }
        //                    else
        //                    {
        //                        // TODO: if Control, goto top ... e.KeyboardDevice.Modifiers
        //                        //CaretPosition = commandStart = Document.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);

        //                        CaretPosition = commandStart.GetInsertionPosition(LogicalDirection.Forward);//currentParagraph.ContentStart.GetPositionAtOffset(promptLength);
        //                    }
        //                    e.Handled = true;
        //                }
        //            } break;
        //        case Key.End:
        //            {
        //                // shift + ctrl
        //                if (((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        //                    && ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
        //                {
        //                    Selection.Select(Selection.Start, CaretPosition.DocumentEnd);
        //                }
        //                else if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        //                {
        //                    CaretPosition = CaretPosition.DocumentEnd;
        //                }
        //                else if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        //                {
        //                    Selection.Select(Selection.Start, (CaretPosition.GetLineStartPosition(1) ?? CaretPosition.DocumentEnd).GetInsertionPosition(LogicalDirection.Backward));
        //                }
        //                else
        //                {
        //                    CaretPosition = (CaretPosition.GetLineStartPosition(1) ?? CaretPosition.DocumentEnd).GetNextInsertionPosition(LogicalDirection.Backward);
        //                }

        //                e.Handled = true;
        //            } break;
        //        case Key.Back:
        //            goto case Key.Delete;
        //        case Key.Delete:
        //            {
        //                if (!Selection.IsEmpty)
        //                {
        //                    if (Selection.Start.GetOffsetToPosition(commandStart) >= 0)
        //                    {
        //                        Selection.Select(commandStart.GetInsertionPosition(LogicalDirection.Forward), Selection.End);
        //                    }
        //                    if (Selection.End.GetOffsetToPosition(commandStart) >= 0)
        //                    {
        //                        Selection.Select(Selection.Start, commandStart.GetInsertionPosition(LogicalDirection.Forward));
        //                    }
        //                    Selection.Text = string.Empty;
        //                    e.Handled = true;
        //                }
        //                else
        //                {
        //                    int offset = 0;
        //                    if (currentParagraph.Inlines.Count > promptInlines)
        //                    {
        //                        offset = currentParagraph.Inlines.LastInline.ElementStart.GetOffsetToPosition(CaretPosition);
        //                    }
        //                    else
        //                    {
        //                        offset = commandStart.GetInsertionPosition(LogicalDirection.Forward).GetOffsetToPosition(CaretPosition);
        //                    }
        //                    if (offset < 0 || (offset == 0 && e.Key == Key.Back) || CurrentCommand.Length <= 0)
        //                    {
        //                        e.Handled = true;
        //                    }
        //                }
        //            } break;
        //        //// we're only handling this to avoid the default handler when you try to copy
        //        //// since that would de-select the text
        //        //case Key.X: goto case Key.C;
        //        //case Key.C:
        //        //    {
        //        //        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        //        //        {
        //        //            goto default;
        //        //        }
        //        //    } break;

        //        // here's a few keys we want the base control to handle:
        //        case Key.Right: break;
        //        case Key.RightAlt: break;
        //        case Key.LeftAlt: break;
        //        case Key.RightCtrl: break;
        //        case Key.LeftCtrl: break;
        //        case Key.RightShift: break;
        //        case Key.LeftShift: break;
        //        case Key.RWin: break;
        //        case Key.LWin: break;
        //        case Key.CapsLock: break;
        //        case Key.Insert: break;
        //        case Key.NumLock: break;
        //        case Key.PageUp: break;
        //        case Key.PageDown: break;

        //        // if a key isn't in the list above, then make sure we're in the prompt before we let it through
        //        default:
        //            {
        //                tabbing = null;
        //                tabbingCount = -1;

        //                //System.Diagnostics.Debug.WriteLine(CaretPosition.GetOffsetToPosition(EndOfOutput));
        //                if (commandStart == null || CaretPosition.GetOffsetToPosition(commandStart) > 0)
        //                {
        //                    CaretPosition = Document.ContentEnd.GetInsertionPosition(LogicalDirection.Forward);
        //                }
        //                // if they type anything, they're not using the history buffer.
        //                historyIndex = -1;
        //                // if they type anything, they're no longer using the autocopy
        //                autoCopy = false;
        //            } break;
        //        #endregion
        //    }
        //    base.OnPreviewKeyDown(e);
        //    Trace.Unindent();
        //    Trace.TraceInformation("Exiting OnPreviewKeyDown:");
        //}

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        ///// <summary>
        ///// Helper to apply formatting properties to matching words in the document.
        ///// </summary>
        //private void FormatWords()
        //{
        //    // Applying formatting properties, triggers another TextChangedEvent. Remove event handler temporarily.
        //    this.TextChanged -= this.TextChangedEventHandler;

        //    // Add formatting for matching words.
        //    foreach (Word word in _words)
        //    {
        //        TextRange range = new TextRange(word.Start, word.End);
        //        range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
        //        range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
        //    }
        //    _words.Clear();

        //    // Add TextChanged handler back.
        //    this.TextChanged += this.TextChangedEventHandler;
        //}

        ///// <summary>
        ///// Scans passed Run's text, for any matching words from dictionary.
        ///// </summary>
        //private void AddMatchingWordsInRun(Run run)
        //{
        //    string runText = run.Text;

        //    int wordStartIndex = 0;
        //    int wordEndIndex = 0;
        //    for (int i = 0; i < runText.Length; i++)
        //    {
        //        if (Char.IsWhiteSpace(runText[i]))
        //        {
        //            if (i > 0 && !Char.IsWhiteSpace(runText[i - 1]))
        //            {
        //                wordEndIndex = i - 1;
        //                string wordInRun = runText.Substring(wordStartIndex, wordEndIndex - wordStartIndex + 1);

        //                if (keywords1.ContainsKey(wordInRun))
        //                {
        //                    TextPointer wordStart = run.ContentStart.GetPositionAtOffset(wordStartIndex, LogicalDirection.Forward);
        //                    TextPointer wordEnd = run.ContentStart.GetPositionAtOffset(wordEndIndex + 1, LogicalDirection.Backward);
        //                    _words.Add(new Word(wordStart, wordEnd));
        //                }
        //            }
        //            wordStartIndex = i + 1;
        //        }
        //    }

        //    // Check if the last word in the Run is a matching word.
        //    string lastWordInRun = runText.Substring(wordStartIndex, runText.Length - wordStartIndex);
        //    if (keywords1.ContainsKey(lastWordInRun))
        //    {
        //        TextPointer wordStart = run.ContentStart.GetPositionAtOffset(wordStartIndex, LogicalDirection.Forward);
        //        TextPointer wordEnd = run.ContentStart.GetPositionAtOffset(runText.Length, LogicalDirection.Backward);
        //        _words.Add(new Word(wordStart, wordEnd));
        //    }
        //}

        #endregion

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        /// <summary>
        /// This class encapsulates a matching word by two TextPointer positions, 
        /// start and end, with forward and backward gravities respectively.
        /// </summary>
        private class Word
        {
            
		#region [rgn] Fields (2)

		private readonly TextPointer _wordEnd;
		private readonly TextPointer _wordStart;

		#endregion [rgn]

		#region [rgn] Constructors (1)

		public Word(TextPointer wordStart, TextPointer wordEnd)
            {
                _wordStart = wordStart.GetPositionAtOffset(0, LogicalDirection.Forward);
                _wordEnd = wordEnd.GetPositionAtOffset(0, LogicalDirection.Backward);
            }
		
		#endregion [rgn]

		#region [rgn] Properties (2)

		public TextPointer End
            {
                get
                {
                    return _wordEnd;
                }
            }
		
		public TextPointer Start
            {
                get
                {
                    return _wordStart;
                }
            }
		
		#endregion [rgn]

        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------

        #region Private Members

        //private List<string> myHistory;
        //public List<string> CommandHistory
        //{
        //    get { return myHistory; }
        //    set { myHistory = value; }
        //}


        // Static list of editing formatting commands. In the ctor we disable all these commands.
        private static readonly RoutedUICommand[] _formattingCommands = new RoutedUICommand[]
            {
                EditingCommands.ToggleBold,
                EditingCommands.ToggleItalic,
                EditingCommands.ToggleUnderline,
                EditingCommands.ToggleSubscript,
                EditingCommands.ToggleSuperscript,
                EditingCommands.IncreaseFontSize,
                EditingCommands.DecreaseFontSize,
                EditingCommands.ToggleBullets,
                EditingCommands.ToggleNumbering,
                EditingCommands.AlignCenter,
                EditingCommands.AlignJustify,
                EditingCommands.AlignRight,
                EditingCommands.AlignLeft,              
            };

        #endregion Private Members

        int _promptInlines = 0;
        TextPointer _promptEnd = null;

        public delegate void EndOutputDelegate();
        public delegate void PromptDelegate(string prompt);
        public delegate void ColoredPromptDelegate(ConsoleColor foreground, ConsoleColor background, string prompt);
        /// <summary>
        /// Prompts with the default colors
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        public void Prompt(string prompt)
        {
            Dispatcher.ExitAllFrames();
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (BeginInvoke)delegate
            {
                Write(_consoleBrushes.DefaultForeground, _consoleBrushes.Transparent, prompt);
                ////TextRange prmpt = new TextRange( _currentParagraph.ContentStart, _currentParagraph.ContentEnd );
                //_promptEnd = _currentParagraph.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
                SetPrompt();
                LimitBuffer();
            });

        }

        private void LimitBuffer()
        {
            while ( (Document.Blocks.Count > Properties.Settings.Default.MaxBufferCommands)
                || (new TextRange(Document.ContentStart,Document.ContentEnd).Text.LineCount() > Properties.Settings.Default.MaxBufferLines))
            {
                Document.Blocks.Remove(Document.Blocks.FirstBlock);
            }
        }

        private bool _running = true; // starts "running" because it's processing the profile (until the first prompt is written)
        public bool IsRunning { get { return _running; } }

        private void FixPrompt()
        {
            if (!_promptEnd.IsInSameDocument(Document.ContentEnd) )
            {
                TrimOutput();
                Prompt(">");
            }
        }


        private void SetPrompt()
        {
            BeginChange();
            _promptEnd = _currentParagraph.ContentEnd.GetPositionAtOffset(-1).GetNextInsertionPosition(LogicalDirection.Backward);
            // this is the run that the user will type their command into...
            Run command = new Run("", _currentParagraph.ContentEnd.GetInsertionPosition(LogicalDirection.Forward)); // , Document.ContentEnd
            //// it's VITAL that this Run "look" different than the previous one
            //// otherwise if you backspace the last character it merges into the previous output
            //command.Background = Background; 
            //command.Foreground = Foreground;
            EndChange();
            //_promptInlines = _currentParagraph.Inlines.Count;

            // toggle undo to prevent "undo"ing past this point.
            IsUndoEnabled = false;
            IsUndoEnabled = true;

            CaretPosition = Document.ContentEnd;

            _running = false;
            //Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (BeginInvoke)delegate
            //{
            while (!_running && (_keyBuffer.Count > 0))
            {
                ReplayableKeyEventArgs k = _keyBuffer.Dequeue();
                k.KeyEventArgs.Handled = false;
                InputManager.Current.ProcessInput(k.KeyEventArgs);
                if (k.TextCompositionEventArgs != null)
                {
                    //k.TextCompositionEventArgs.Handled = false;
                    InputManager.Current.ProcessInput(k.TextCompositionEventArgs);
                }
            }
            //});
        }

        Paragraph _currentParagraph = null;

       
        private void Write(Brush foreground, Brush background, string text)
        {
            BeginChange();
            // handle null values - so that other Write* methods don't have to deal with Dispatchers just to look up a color
            if (foreground == null) foreground = Foreground;
            if (background == null) background = Background;//Brushes.Transparent;

            if (_currentParagraph == null)
            {
                _currentParagraph = new Paragraph();
                Document.Blocks.Add(_currentParagraph);
            }

            //TextPointer currentPos = CaretPosition;

            string[] ansis = text.Split(new string[] { "\x1B[" }, StringSplitOptions.None);

            if (ansis.Length == 1)
            {
                Run insert = new Run(text, _currentParagraph.ContentEnd);
                insert.Background = background;
                insert.Foreground = foreground;
            }
            #region Escape Sequences
            else
            {
                // we want ansi excaped color changes to be "sticky"
                Brush bg = background;
                Brush fg = foreground;

                Boolean first = true;
                foreach (string ansi in ansis)
                {
                    if (first && ansi.Length > 0)
                    {
                        Run insert = new Run(ansi, _currentParagraph.ContentEnd);
                        insert.Background = bg;
                        insert.Foreground = fg;
                    }
                    else if (ansi.Length > 0)
                    {
                        // bool bg = false;
                        int m1 = ansi.IndexOf('m');
                        int m2 = ansi.IndexOf(']');
                        int split = -1;
                        if (m1 > 0 && (m2 < 0 || m1 < m2))
                        {
                            split = m1;
                        }
                        else if (m2 > 0)
                        {
                            split = m2;
                        }
                        bool dark = true, back = false;
                        Run insert = new Run(ansi.Substring(split + 1), _currentParagraph.ContentEnd);
                        insert.Foreground = fg;
                        insert.Background = bg;
                        if (split > 0)
                        {
                            foreach (string code in ansi.Substring(0, split).Split(';'))
                            {
                                switch (code.ToUpper())
                                {
                                    case "0": goto case "RESET";// RESET
                                    case "CLEAR": goto case "RESET";
                                    case "RESET":
                                        insert.Background = bg = Brushes.Transparent;
                                        insert.Foreground = Foreground;
                                        insert.FontStyle = FontStyles.Normal;
                                        insert.FontWeight = FontWeights.Normal;
                                        insert.TextDecorations.Clear();
                                        break;
                                    case "1":
                                        dark = false;
                                        break;
                                    case "2":
                                        dark = true;
                                        break;
                                    case "4": goto case "UNDERLINE";
                                    case "UNDERLINE":
                                        insert.TextDecorations.Add(TextDecorations.Underline);
                                        break;
                                    case "5": // blink 
                                        goto case "ITALIC";
                                    case "ITALIC":
                                        insert.FontStyle = FontStyles.Italic;
                                        break;
                                    //case "6": // blink faster
                                    case "7": // inverse
                                        goto case "BOLD";
                                    case "BOLD":
                                        insert.FontWeight = FontWeights.Bold;
                                        break;
                                    case "8": // hidden (uhm, no)
                                        goto case "THIN";
                                    case "THIN":
                                        insert.FontWeight = FontWeights.Thin;
                                        // insert.Foreground = ConsoleBrushes.Transparent;
                                        break;

                                    #region ANSI COLOR SEQUENCES 30-37 and 40-47


                                    case "30":
                                        insert.Foreground = (dark) ? _consoleBrushes.Black : _consoleBrushes.DarkGray;
                                        break;
                                    case "40":
                                        insert.Background = (dark) ? _consoleBrushes.Black : _consoleBrushes.DarkGray;
                                        break;

                                    case "31":
                                        insert.Foreground = (dark) ? _consoleBrushes.DarkRed : _consoleBrushes.Red;
                                        break;
                                    case "41":
                                        insert.Background = (dark) ? _consoleBrushes.DarkRed : _consoleBrushes.Red;
                                        break;

                                    case "32":
                                        insert.Foreground = (dark) ? _consoleBrushes.DarkGreen : _consoleBrushes.Green;
                                        break;
                                    case "42":
                                        insert.Background = (dark) ? _consoleBrushes.DarkGreen : _consoleBrushes.Green;
                                        break;

                                    case "33":
                                        insert.Foreground = (dark) ? _consoleBrushes.DarkYellow : _consoleBrushes.Yellow;
                                        break;
                                    case "43":
                                        insert.Background = (dark) ? _consoleBrushes.DarkYellow : _consoleBrushes.Yellow;
                                        break;

                                    case "34":
                                        insert.Foreground = (dark) ? _consoleBrushes.DarkBlue : _consoleBrushes.Blue;
                                        break;
                                    case "44":
                                        insert.Background = (dark) ? _consoleBrushes.DarkBlue : _consoleBrushes.Blue;
                                        break;

                                    case "35":
                                        insert.Foreground = (dark) ? _consoleBrushes.DarkMagenta : _consoleBrushes.Magenta;
                                        break;
                                    case "45":
                                        insert.Background = (dark) ? _consoleBrushes.DarkMagenta : _consoleBrushes.Magenta;
                                        break;

                                    case "36":
                                        insert.Foreground = (dark) ? _consoleBrushes.DarkCyan : _consoleBrushes.Cyan;
                                        break;
                                    case "46":
                                        insert.Background = (dark) ? _consoleBrushes.DarkCyan : _consoleBrushes.Cyan;
                                        break;

                                    case "37":
                                        insert.Foreground = (dark) ? _consoleBrushes.White : _consoleBrushes.Gray;
                                        break;
                                    case "47":
                                        insert.Background = (dark) ? _consoleBrushes.White : _consoleBrushes.Gray;
                                        break;


                                    #endregion ANSI COLOR SEQUENCES 30-37 and 40-47

                                    #region ConsoleColor Enumeration values
                                    case "TRANSPARENT":
                                        {
                                            if (back)
                                            {
                                                insert.Background = _consoleBrushes.Transparent;
                                            }
                                            else
                                            {
                                                insert.Foreground = _consoleBrushes.Transparent;
                                            }
                                            back = true;
                                        } break;
                                    #endregion ConsoleColor Enumeration values

                                    default:
                                        if (code[0] == '#')
                                        {
                                            #region parse hex color codes
                                            try
                                            {
                                                byte a, r, g, b;
                                                // if there's an alpha value...
                                                if (code.Length == 9)
                                                {
                                                    a = Byte.Parse(code.Substring(1, 2), NumberStyles.HexNumber);
                                                    r = Byte.Parse(code.Substring(3, 2), NumberStyles.HexNumber);
                                                    g = Byte.Parse(code.Substring(5, 2), NumberStyles.HexNumber);
                                                    b = Byte.Parse(code.Substring(7, 2), NumberStyles.HexNumber);
                                                }
                                                else if (code.Length == 7)
                                                {
                                                    r = Byte.Parse(code.Substring(1, 2), NumberStyles.HexNumber);
                                                    g = Byte.Parse(code.Substring(3, 2), NumberStyles.HexNumber);
                                                    b = Byte.Parse(code.Substring(5, 2), NumberStyles.HexNumber);
                                                    a = Byte.MaxValue;
                                                }
                                                else break;

                                                if (back)
                                                {
                                                    insert.Background = new SolidColorBrush(Color.FromArgb(a, r, g, b));
                                                }
                                                else
                                                {
                                                    insert.Foreground = new SolidColorBrush(Color.FromArgb(a, r, g, b));
                                                }

                                            }
                                            catch { } // just ignore the sequence?
                                            finally { back = true; }
                                            #endregion parse hex color codes
                                        }
                                        else
                                        {
                                            #region parse ConsoleColor enum values
                                            try
                                            {
                                                if (back)
                                                {
                                                    insert.Background = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)Enum.Parse(typeof(ConsoleColor), code));
                                                }
                                                else
                                                {
                                                    insert.Foreground = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)Enum.Parse(typeof(ConsoleColor), code));
                                                }
                                            }
                                            catch { } // just ignore the sequence?
                                            finally { back = true; }
                                            #endregion parse ConsoleColor enum values
                                        } break;
                                }
                            }
                            bg = insert.Background;
                            fg = insert.Foreground;
                        }
                    }
                    first = false;
                }
            }
            #endregion Escape Sequences

            ScrollToEnd();
            EndChange();
        }                                   


        /// <summary>
        /// Implements the CLS command the way most command-lines do:
        /// Scroll the window until the prompt is at the top ...
        /// (as opposed to clearing the screen and leaving the prompt at the bottom)
        /// </summary>        
        public void ClearScreen()
        {
            new TextRange(Document.ContentStart, Document.ContentEnd).Text = String.Empty;
            //SelectAll();
            //Selection.Text = String.Empty;
        }


        private bool _autoCopy = false;
        /// <summary>
        /// Copies the current selection of the text editing control to the <see cref="T:System.Windows.Clipboard"></see>.
        /// <remarks>
        /// Overrides the base behavior of the RichTextbox, providing a feature which allows automatic 
        /// copying of full "paragraphs" (that is: a prompt, command and result).  The idea is to allow
        /// a simple Ctrl+C to copy the previous command (and additional Ctrl-Cs to copy more commands).
        /// </remarks>
        /// </summary>
        new public void Copy()
        {
            if (_autoCopy || Selection.IsEmpty)
            {
                if (!_autoCopy)
                {
                    _autoCopy = true;
                    // select a whole paragraph, from the bottom
                    CaretPosition = CaretPosition.Paragraph.ContentEnd;
                    EditingCommands.SelectUpByParagraph.Execute(null, this);

                    CaretPosition = Selection.Start;
                }
                EditingCommands.SelectUpByParagraph.Execute(null, this);
            }

            // TODO: an option to leave the prompt out of the copied text.

            base.Copy();
        }

        #region ConsoleSizeCalculations
        //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        //{
        //    base.OnRenderSizeChanged(sizeInfo);
        //}

        /// <summary>
        /// Gets or sets the name of the specified font.
        /// </summary>
        /// <value></value>
        /// <returns>A font family. The default value is the system dialog font.</returns>
        public new FontFamily FontFamily
        {
            get
            {
                return base.FontFamily;
            }
            set
            {
                base.FontFamily = value;
                UpdateCharacterWidth();
                Document.LineHeight = FontSize * FontFamily.LineSpacing;
            }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        /// <value></value>
        /// <returns>A font size. The default value is the system dialog font size. The font size must be a positive number and in the range of the <see cref="P:System.Windows.SystemFonts.MessageFontSize"></see>.</returns>
        public new double FontSize
        {
            get
            {
                return base.FontSize;
            }
            set
            {
                base.FontSize = value;
                Document.LineHeight = FontSize * FontFamily.LineSpacing;
            }
        }

        private double _characterWidth = 0.5498046875; // Default to the size for Consolas

        public double CharacterWidthRatio
        {
            get { return _characterWidth; }
        }

        /// <summary>
        /// Gets or sets the size of the buffer.
        /// </summary>
        /// <value>The size of the buffer.</value>
        public System.Management.Automation.Host.Size BufferSize
        {
            get
            {
                return new System.Management.Automation.Host.Size(
                    (int)((((ExtentWidth > 0) ? ExtentWidth : RenderSize.Width) - (Padding.Left + Padding.Right))
                        / (FontSize * _characterWidth)) - 1,
                    (int)((((ExtentHeight > 0) ? ExtentHeight : RenderSize.Height) - (Padding.Top + Padding.Bottom))
                        / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)));
            }
            set
            {
                this.Width = value.Width * FontSize * _characterWidth;
                // our buffer is infinite-ish
                //this.Height = value.Y * Document.LineHeight;
            }
        }

        /// <summary>
        /// Gets or sets the size of the window.
        /// </summary>
        /// <value>The size of the window.</value>
        public System.Management.Automation.Host.Size WindowSize
        {
            get
            {
                return new System.Management.Automation.Host.Size(
                  (int)((((ViewportWidth > 0) ? ViewportWidth : RenderSize.Width) - (Padding.Left + Padding.Right))
                        / (FontSize * _characterWidth)) - 1,
                  (int)(((ViewportHeight > 0) ? ViewportHeight : RenderSize.Height)
                        / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)));

            }
            set
            {
                this.Width = value.Width * FontSize * _characterWidth;
                this.Height = value.Height * Document.LineHeight;
            }
        }

        /// <summary>
        /// Gets or sets the size of the max window.
        /// </summary>
        /// <value>The size of the max window.</value>
        public System.Management.Automation.Host.Size MaxWindowSize
        {
            get
            {
                return new System.Management.Automation.Host.Size(
                    (int)(System.Windows.SystemParameters.PrimaryScreenWidth - (Padding.Left + Padding.Right)
                            / (FontSize * _characterWidth)) - 1,
                    (int)(System.Windows.SystemParameters.PrimaryScreenHeight - (Padding.Top + Padding.Bottom)
                            / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)));

            }
            //set { myMaxWindowSize = value; }
        }

        /// <summary>
        /// Gets or sets the size of the max physical window.
        /// </summary>
        /// <value>The size of the max physical window.</value>
        public System.Management.Automation.Host.Size MaxPhysicalWindowSize
        {
            get { return MaxWindowSize; }
            //set { myMaxPhysicalWindowSize = value; }
        }

        private System.Management.Automation.Host.Coordinates _cursorPosition;

        /// <summary>
        /// Gets or sets the cursor position.
        /// </summary>
        /// <value>The cursor position.</value>
        public System.Management.Automation.Host.Coordinates CursorPosition
        {
            get
            {
                Rect caret = CaretPosition.GetInsertionPosition(LogicalDirection.Forward).GetCharacterRect(LogicalDirection.Backward);
                _cursorPosition.X = (int)caret.Left;
                _cursorPosition.Y = (int)caret.Top;
                return _cursorPosition;
            }
            set
            {
                TextPointer p = GetPositionFromPoint(new Point(value.X * FontSize * _characterWidth, value.Y * Document.LineHeight), true);
                CaretPosition = p ?? Document.ContentEnd;
            }
        }


        /// <summary>
        /// Updates the value of the CharacterWidthRatio
        /// <remarks>
        /// Called each time the font-family changes
        /// </remarks>
        /// </summary>
        private void UpdateCharacterWidth()
        {
            // Calculate the font width (as a percentage of it's height)            
            foreach (Typeface tf in FontFamily.GetTypefaces())
            {
                if (tf.Weight == FontWeights.Normal && tf.Style == FontStyles.Normal)
                {
                    GlyphTypeface glyph;// = new GlyphTypeface();
                    if (tf.TryGetGlyphTypeface(out glyph))
                    {
                        // if this is really a fixed width font, then the widths should be equal:
                        // glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'M']]
                        // glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'i']]
                        _characterWidth = glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'M']];
                        break;
                    }
                }
            }
        }
        #endregion ConsoleSizeCalculations

        //public static readonly RoutedEvent TitleChangedEvent = EventManager.RegisterRoutedEvent("TitleChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ConsoleRichTextBox));

        //public event RoutedEventHandler TitleChanged
        //{
        //    add
        //    {
        //        AddHandler(TitleChangedEvent, value);
        //    }
        //    remove
        //    {
        //        RemoveHandler(TitleChangedEvent, value);
        //    }
        //}

        public static DependencyProperty WindowTitleProperty = DependencyProperty.RegisterAttached("Title", typeof(string), typeof(ConsoleRichTextBox));

        public string WindowTitle
        {
            get { return (string)GetValue(WindowTitleProperty); }
            set { SetValue(WindowTitleProperty, value); }
        }


        /// <summary>
        /// Gets the viewport position (in lines/chars)
        /// </summary>
        /// <returns></returns>
        public Coordinates WindowPosition
        {
            get
            {
                int x, y = 0, lines = -1;
                TextPointer origin = GetPositionFromPoint(new Point(0, 0), true).GetInsertionPosition(LogicalDirection.Forward);
                //TextPointer c = origin.GetLineStartPosition(0).GetNextInsertionPosition(LogicalDirection.Forward);
                TextPointer c = origin.GetLineStartPosition(0).GetInsertionPosition(LogicalDirection.Forward);
                x = c.GetOffsetToPosition(origin);
                origin = origin.GetLineStartPosition(1);

                while (lines < 0)
                {
                    c = c.GetLineStartPosition(-10, out lines); y -= lines;
                }
                return new Coordinates(x, y);
            }
            set
            {
                //// (value.X * FontSize * characterWidth, value.Y * Document.LineHeight)
                //TextPointer lineStart = CaretPosition.DocumentStart.GetInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(value.Y, out lines).GetInsertionPosition(LogicalDirection.Forward);
                //while (lines < value.Y)
                //{
                //    for (; lines < value.Y; lines++)
                //    {
                //        CaretPosition.DocumentEnd.InsertLineBreak();
                //    }
                //    lineStart = CaretPosition.DocumentStart.GetInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(value.Y, out lines).GetInsertionPosition(LogicalDirection.Forward);
                //}

                //TextPointer nextLine = lineStart.GetLineStartPosition(1);
                //TextPointer site = lineStart.GetPositionAtOffset(value.X);
                //if (site.GetOffsetToPosition(nextLine) <= 0)
                //{
                //    site = lineStart;
                //}

                ScrollToVerticalOffset(value.Y * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight));
                ScrollToHorizontalOffset(value.X * FontSize * _characterWidth);
            }
        }

        public BufferCell GetCell(TextPointer position, LogicalDirection direction)
        {
            TextRange range = null;
            try
            {
                range = new TextRange(position, position.GetPositionAtOffset(1, direction));

                return new BufferCell(
                        (range.IsEmpty || range.Text[0] == '\n') ? ' ' : range.Text[0],
                        _consoleBrushes.ConsoleColorFromBrush((Brush)range.GetPropertyValue(TextElement.ForegroundProperty)),
                        _consoleBrushes.ConsoleColorFromBrush((Brush)range.GetPropertyValue(TextElement.BackgroundProperty)),
                        BufferCellType.Complete);
            }
            catch
            {
                return new BufferCell();
            }
        }

        public void SetCell(BufferCell cell, TextPointer position, LogicalDirection direction)
        {
            TextRange range = null;

            TextPointer positionPlus = position.GetNextInsertionPosition(LogicalDirection.Forward);
            if (positionPlus != null)
            {
                range = new TextRange(position, positionPlus);
            }

            if (null == range || range.IsEmpty)
            {
                position = position.GetInsertionPosition(LogicalDirection.Forward);
                if (position != null)
                {
                    Run r = position.GetAdjacentElement(LogicalDirection.Forward) as Run;

                    if (null != r)
                    {
                        if (r.Text.Length > 0)
                        {
                            char[] chr = r.Text.ToCharArray();
                            chr[0] = cell.Character;
                            r.Text = chr.ToString();
                        }
                        else
                        {
                            r.Text = cell.Character.ToString();
                        }
                    }
                    else
                    {
                        r = position.GetAdjacentElement(LogicalDirection.Backward) as Run;
                        if (null != r
                            && r.Background == _consoleBrushes.BrushFromConsoleColor(cell.BackgroundColor)
                            && r.Foreground == _consoleBrushes.BrushFromConsoleColor(cell.ForegroundColor)
                        )
                        {
                            if (r.Text.Length > 0)
                            {
                                r.Text = r.Text + cell.Character;
                            }
                            else
                            {
                                r.Text = cell.Character.ToString();
                            }
                        }
                        else
                        {
                            r = new Run(cell.Character.ToString(), position);
                        }
                    }
                    r.Background = _consoleBrushes.BrushFromConsoleColor(cell.BackgroundColor);
                    r.Foreground = _consoleBrushes.BrushFromConsoleColor(cell.ForegroundColor);
                    //position = r.ElementStart;
                }

            }
            else
            {

                range.Text = cell.Character.ToString();
                range.ApplyPropertyValue(TextElement.BackgroundProperty, _consoleBrushes.BrushFromConsoleColor(cell.BackgroundColor));
                range.ApplyPropertyValue(TextElement.ForegroundProperty, _consoleBrushes.BrushFromConsoleColor(cell.ForegroundColor));
            }
        }

        public BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            TextPointer origin, lineStart, cell;
            int lines = 0, x = 0, y = 0, width = rectangle.Right - rectangle.Left, height = rectangle.Bottom - rectangle.Top;
            BufferCell[,] buffer = new BufferCell[rectangle.Bottom - rectangle.Top, rectangle.Right - rectangle.Left];
            BufferCell blank = new BufferCell(' ', _consoleBrushes.ConsoleColorFromBrush(Foreground), _consoleBrushes.ConsoleColorFromBrush(Background), BufferCellType.Complete);

            lineStart = CaretPosition.DocumentStart.GetInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(rectangle.Top, out lines).GetInsertionPosition(LogicalDirection.Forward);
            if (lines < rectangle.Top)
            {
                //throw new ArgumentOutOfRangeException("rectangle", rectangle, "The .Top of the boundary rectangle is larger than the available buffer");
                for (; y < height; y++)
                {
                    for (; x < width; x++)
                    {
                        buffer[y, x] = blank;
                    }
                }
                return buffer;
            }

            cell = origin = lineStart.GetPositionAtOffset(rectangle.Left);
            lineStart = origin.GetLineStartPosition(1, out lines).GetInsertionPosition(LogicalDirection.Forward);

            while (y < height)
            {
                while (x < width && cell.GetOffsetToPosition(lineStart) > 0)
                {
                    buffer[y, x] = GetCell(cell, LogicalDirection.Forward);
                    cell = cell.GetPositionAtOffset(1, LogicalDirection.Forward);
                    x++;
                }
                // just in case we ran out of line before we ran out of text...
                while (x < width)
                {
                    buffer[y, x] = blank;
                    x++;
                }
                // try advancing one line ...
                cell = lineStart.GetPositionAtOffset(rectangle.Left - 1);
                lineStart = lineStart.GetLineStartPosition(1, out lines).GetInsertionPosition(LogicalDirection.Forward);
                y++;
                x = 0;

                // if we skipped lines, fill them in...
                if (lines > 1)
                {
                    for (int i = 1; i < lines; i++)
                    {
                        while (x < width)
                        {
                            buffer[y, x] = blank;
                            x++;
                        }
                        y++;
                        x = 0;
                    }
                }
                #region fill with blanks
                else if (lines == 0)
                {
                    // we've reached the end, just fill the rest with blanks.
                    while (y < height)
                    {
                        while (x < width)
                        {
                            buffer[y, x] = blank;
                            x++;
                        }
                        y++;
                        x = 0;
                    }
                }
                #endregion
            }
            return buffer;
        }

        public void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            if (rectangle.Left == -1 && rectangle.Right == -1)
            {
                ClearScreen();
            }
            else
            {

                TextPointer lineStart, lineEnd, cell, rowEnd, originCell;
                TextRange row;
                int lines = 0, y = 0, diff;
                int height = rectangle.Bottom - rectangle.Top;
                int width = rectangle.Right - rectangle.Left;

                string fillString = new string(fill.Character, width);

                lineStart = CaretPosition.DocumentStart.GetInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(rectangle.Top, out lines).GetInsertionPosition(LogicalDirection.Forward);
                while (lines < rectangle.Top)
                {
                    for (; lines < rectangle.Top; lines++)
                    {
                        CaretPosition.DocumentEnd.InsertLineBreak();
                    }
                    lineStart = CaretPosition.DocumentStart.GetInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(rectangle.Top, out lines).GetInsertionPosition(LogicalDirection.Forward);
                }

                cell = originCell = lineStart.GetPositionAtOffset(rectangle.Left);
                lineStart = originCell.GetLineStartPosition(1, out lines).GetInsertionPosition(LogicalDirection.Forward);
                lineEnd = originCell.GetLineStartPosition(1, out lines).GetNextInsertionPosition(LogicalDirection.Backward);

                if (lines != 1)
                {
                    lineStart.Paragraph.ElementEnd.InsertLineBreak();
                }
                BeginChange();
                while (y < height)
                {
                    diff = 0;
                    rowEnd = cell.GetPositionAtOffset(width);
                    #region clear existing text
                    if (null != rowEnd && null != lineEnd)
                    {
                        diff = rowEnd.GetOffsetToPosition(lineEnd);
                    }
                    if (diff > 0 || lineEnd == null)
                    {
                        row = new TextRange(cell, rowEnd);
                    }
                    else
                    {
                        row = new TextRange(cell, lineEnd);
                    }
                    row.Text = String.Empty;
                    #endregion clear existing text

                    // insert new text
                    Run r = new Run(fillString, cell);
                    r.Background = _consoleBrushes.BrushFromConsoleColor(fill.BackgroundColor);
                    r.Foreground = _consoleBrushes.BrushFromConsoleColor(fill.ForegroundColor);

                    // try advancing one line ...
                    cell = lineStart.GetPositionAtOffset(rectangle.Left - 1);
                    lineEnd = lineStart.GetLineStartPosition(1, out lines).GetNextInsertionPosition(LogicalDirection.Backward);
                    lineStart = lineStart.GetLineStartPosition(1, out lines).GetInsertionPosition(LogicalDirection.Forward);

                    if (lines != 1)
                    {
                        lineStart.Paragraph.ElementEnd.InsertLineBreak();
                    }
                    y++;
                }
                EndChange();
            }
        }

        public void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            TextPointer lineStart, lineEnd, cell, rowEnd, originCell;
            TextRange row;
            Run r;
            Brush back, fore;
            BufferCell bc;

            int lines = 0, x = 0, y = 0, diff;
            int height = contents.GetLength(0);
            int width = contents.GetLength(1);

            lineStart = CaretPosition.DocumentStart.GetInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(origin.Y, out lines);
            while (lines < origin.Y)
            {
                for (; lines < origin.Y; lines++)
                {
                    CaretPosition.DocumentEnd.InsertLineBreak();
                }
                lineStart = CaretPosition.DocumentStart.GetInsertionPosition(LogicalDirection.Forward).GetLineStartPosition(origin.Y, out lines);
            }

            if (origin.X > 0)
            {
                cell = originCell = lineStart.GetPositionAtOffset(origin.X).GetInsertionPosition(LogicalDirection.Forward);
            }
            else
            {
                cell = originCell = lineStart.GetInsertionPosition(LogicalDirection.Forward);
            }
            lineStart = originCell.GetLineStartPosition(1, out lines);
            lineEnd = lineStart.GetNextInsertionPosition(LogicalDirection.Backward);

            if (lines != 1)
            {
                lineStart.Paragraph.ElementEnd.InsertLineBreak();
            }
            BeginChange();
            while (y < height)
            {
                diff = 0;
                rowEnd = cell.GetPositionAtOffset(width);
                #region clear existing text
                if (null != rowEnd && null != lineEnd)
                {
                    diff = rowEnd.GetOffsetToPosition(lineEnd);
                }

                if (diff > 0 || lineEnd == null)
                {
                    row = new TextRange(cell, rowEnd);
                }
                else
                {
                    row = new TextRange(cell, lineStart);
                }
                row.Text = String.Empty;
                #endregion clear existing text

                // insert new text
                r = new Run(String.Empty, row.Start);

                r.Background = Background;
                r.Foreground = Foreground;
                for (x = 0; x < width; ++x)
                {
                    bc = contents[y, x];
                    back = _consoleBrushes.BrushFromConsoleColor(bc.BackgroundColor);
                    fore = _consoleBrushes.BrushFromConsoleColor(bc.ForegroundColor);

                    if (r.Background != back || r.Foreground != fore)
                    {
                        r = new Run(bc.Character.ToString(), r.ElementEnd);
                        r.Background = back;
                        r.Foreground = fore;
                    }
                    else
                    {
                        r.Text += bc.Character;
                    }
                }
                if (diff <= 0 && lineEnd != null)
                {
                    row.End.InsertLineBreak();
                    lineStart = row.Start.GetLineStartPosition(1);
                }

                // try advancing one line ...
                if (origin.X > 0)
                {
                    cell = lineStart.GetPositionAtOffset(origin.X).GetInsertionPosition(LogicalDirection.Forward);
                }
                else
                {
                    cell = lineStart.GetInsertionPosition(LogicalDirection.Forward);
                }
                lineStart = lineStart.GetLineStartPosition(1, out lines);
                if (lines != 1)
                {
                    lineStart.InsertLineBreak();
                    lineStart = lineStart.GetLineStartPosition(1, out lines);
                }
                lineEnd = lineStart.GetNextInsertionPosition(LogicalDirection.Backward);

                y++;
                x = 0;
            }
            EndChange();
        }


        public void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new Exception("The method or operation is not implemented.");
        }


        #region IPSRawConsole Members


        public int CursorSize
        {
            get
            {
                // TODO: we should look at implementing a nice cursor...
                return 25; 
            }
            set
            {
                /* It's meaningless to set our cursor */
            }
        }

        public void FlushInputBuffer()
        {
            ; // ToDo: as far as I can tell, we don't really have an input buffer
        }

        #endregion


        public System.Security.SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException("SecureStrings are not yet implemented by PoshConsole");
        }

        public PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException("Credentials are not yet implemented by PoshConsole");
        }

        public PSCredential PromptForCredential(
                string caption, string message, string userName,
                string targetName, PSCredentialTypes allowedCredentialTypes,
                PSCredentialUIOptions options)
        {
            throw new NotImplementedException("Credentials are not yet implemented by PoshConsole");
        }
    }
}
