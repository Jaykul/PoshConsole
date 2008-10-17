using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Windows.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Reflection;

namespace Huddled.WPF.Controls
{

   /// <summary>
   /// The ConsoleControl is a <see cref="FlowDocumentScrollViewer"/> where all input goes to a sub-textbox after the "prompt"
   /// </summary>
   public partial class ConsoleControl : FlowDocumentScrollViewer
   {
      //static readonly ConsoleBrushes ConsoleBrushes = new ConsoleBrushes();

      static ConsoleControl()
      {
         InitializeCommands();

         DefaultStyleKeyProperty.OverrideMetadata(typeof(ConsoleControl), new FrameworkPropertyMetadata(typeof(ConsoleControl)));
      }

      private static void InitializeCommands()
      {
         CommandManager.RegisterClassCommandBinding(typeof(ConsoleControl),
                                                    new CommandBinding(ApplicationCommands.Cut, OnExecuteCut, OnCanExecuteCut));
         CommandManager.RegisterClassCommandBinding(typeof(ConsoleControl),
                                                    new CommandBinding(ApplicationCommands.Paste, OnExecutePaste, OnCanExecutePaste));
         CommandManager.RegisterClassCommandBinding(typeof(ConsoleControl),
                                                    new CommandBinding(ApplicationCommands.Stop, OnApplicationStop));

         //CommandManager.RegisterClassCommandBinding(typeof(ConsoleRichTextBox),
         //    new CommandBinding(ApplicationCommands.Copy,
         //    new ExecutedRoutedEventHandler(OnCopy),
         //    new CanExecuteRoutedEventHandler(OnCanExecuteCopy)));

      }

      private readonly ConsoleBrushes _brushes;
      private readonly TextBox _commandBox;
      private readonly PasswordBox _passwordBox;
      private readonly InlineUIContainer _commandContainer;
      protected Paragraph _current;
      protected Paragraph _next;



      public ConsoleControl()
      {
         _brushes = new ConsoleBrushes();
         _popup = new PopupMenu(this);
         // Add the popup to the logical branch of the console so keystrokes can be
         // processed from the popup by the console for the tab-complete scenario.
         // E.G.: $Host.Pri[tab].  => "$Host.PrivateData." instead of swallowing the period.
         AddLogicalChild(_popup);

         _expansion = new TabExpansion();
         _cmdHistory = new CommandHistory();

         _commandBox = new TextBox()
                          {
                             IsEnabled = true,
                             Focusable = true,
                             AcceptsTab = true,
                          };
         _passwordBox = new PasswordBox()
                           {
                              IsEnabled = true,
                              Focusable = true
                           };
         _commandBox.PreviewKeyDown += new KeyEventHandler(_commandBox_PreviewKeyDown);
         _passwordBox.PreviewKeyDown += new KeyEventHandler(_passwordBox_PreviewKeyDown);

         _commandContainer = new InlineUIContainer(_commandBox) { BaselineAlignment = BaselineAlignment.Center };
      }



      public override void EndInit()
      {
         base.EndInit();

         //// Initialize the document ...

         _current = new Paragraph();
         _current.ClearFloaters = WrapDirection.Both;

         Document.Blocks.Add(_current);
         // We need to crush the PagePadding so that the "Width" values work...
         Document.PagePadding = new Thickness(0.0);
         //   IsOptimalParagraphEnabled = true,
         //};

         // create the prompt, and stick the command block in it
         _next = new Paragraph();
         Document.Blocks.Add(_next);
         _next.Inlines.Add(_commandContainer);

         Properties.Colors.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ColorsPropertyChanged);
         Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SettingsPropertyChanged);

         // we have to (manually) bind the document and _commandBox values to the "real" ones...
         BindingOperations.SetBinding(Document, FlowDocument.FontFamilyProperty, new Binding("FontFamily") { Source = this });
         BindingOperations.SetBinding(Document, FlowDocument.FontSizeProperty, new Binding("FontSize") { Source = this });

         BindingOperations.SetBinding(Document, FlowDocument.BackgroundProperty, new Binding("Background") { Source = this });
         BindingOperations.SetBinding(Document, FlowDocument.ForegroundProperty, new Binding("Foreground") { Source = this });

         // BindingOperations.SetBinding(_commandBox, FlowDocument.BackgroundProperty, new Binding("Background") { Source = this });
         // BindingOperations.SetBinding(_commandBox, FlowDocument.ForegroundProperty, new Binding("Foreground") { Source = this });

         // find the ScrollViewer
         _scrollViewer = Template.FindName("PART_ContentHost", this) as ScrollViewer;
      }

      ScrollViewer _scrollViewer;
      private ScrollViewer ScrollViewer
      {
         get
         {
            if (_scrollViewer == null)
            {
               _scrollViewer = Template.FindName("PART_ContentHost", this) as ScrollViewer;
            }
            return _scrollViewer;
         }
      }

      //private void NewParagraph()
      //{

      //}
      public void Prompt(string prompt)
      {
         //Dispatcher.ExitAllFrames();
         Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
         {
            if (_next != null)
            {
               Run insert = new Run(prompt);
               insert.Background = Background;
               insert.Foreground = Foreground;
               _next.Inlines.Add(insert);

               SetPrompt();
            }
         });

      }

      private void SetPrompt()
      {
         Dispatcher.VerifyAccess();
         // the problem is, the prompt might have used Write-Host
         // so we need to move the _commandContainer to the end.
         lock (_commandContainer)
         {
            _next.Inlines.Remove(_commandContainer);
            _next.Inlines.Add(_commandContainer);
         }

         UpdateLayout();
         //ScrollViewer.ScrollToBottom();
         _commandContainer.Child.Focus(); // Notice this is "whichever" is active ;)
      }


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
          new PropertyChangedCallback(BackgroundColorPropertyChanged)));

      private static void BackgroundColorPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
      {
         // put code here to handle the property changed for BackgroundColor
         ConsoleControl ConsoleControlObj = depObj as ConsoleControl;
         if (ConsoleControlObj != null)
         {
            ConsoleControlObj.Background = ConsoleControlObj._brushes.BrushFromConsoleColor((ConsoleColor)e.NewValue);
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
          new PropertyChangedCallback(ForegroundColorPropertyChanged)));

      private static void ForegroundColorPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
      {
         // put code here to handle the property changed for ForegroundColor
         ConsoleControl ConsoleControlObj = depObj as ConsoleControl;
         if (ConsoleControlObj != null)
         {
            ConsoleControlObj.Foreground = ConsoleControlObj._brushes.BrushFromConsoleColor((ConsoleColor)e.NewValue);
         }
      }

      #endregion Color Dependency properties

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
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
               {
                  // BUGBUG: Fonts that are not embedded cannot be resolved from this base Uri
                  FontFamily = new FontFamily(new Uri("pack://application:,,,/PoshConsole;component/poshconsole.xaml"), Properties.Settings.Default.FontFamily.Source + ",/FontLibrary;Component/#Bitstream Vera Sans Mono,Global Monospace");
               });
               break;
            case "FontSize":
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
               {
                  FontSize = Properties.Settings.Default.FontSize;
               });
               break;
         }
      }



      private void Write(Brush foreground, Brush background, string text)
      {
         Write(foreground, background, text, _current);
      }

      private void Write(ConsoleColor foreground, ConsoleColor background, string text)
      {
         Write(foreground, background, text, _current);
      }


      private void Write(Brush foreground, Brush background, string text, Block target)
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
         {
            // handle null values so that other methods don't have to 
            // use Dispatchers just to look up a color
            if (foreground == null) foreground = Foreground;
            if (background == null) background = Background;//Brushes.Transparent;

            if (target == null) target = _current;

            new Run(text, target.ContentEnd)
                {
                   Background = background,
                   Foreground = foreground
                };
            ScrollViewer.ScrollToBottom();
            // _commandContainer.BringIntoView();
         });
      }

      private void Write(ConsoleColor foreground, ConsoleColor background, string text, Block target)
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
         {
            if (target == null) target = _current;

            new Run(text, target.ContentEnd)
            {
               Background = _brushes.BrushFromConsoleColor(background),
               Foreground = _brushes.BrushFromConsoleColor(foreground)
            };
            ScrollViewer.ScrollToBottom();
         });
      }



      public string CurrentCommand
      {
         get { return _commandBox.Text; }
         set
         {
            _commandBox.Text = value;
            _commandBox.CaretIndex = value.Length;
         }
      }

      public string Title
      {
         get { return (string)GetValue(TitleProperty); }
         set { SetValue(TitleProperty, value); }
      }


      // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
      // TODO: Check if other properties ought to be backed by dependency properties so they can be bound      
      public static readonly DependencyProperty TitleProperty =
          DependencyProperty.Register("Title", typeof(string), typeof(ConsoleControl), new UIPropertyMetadata("WPF Rich Console"));


      public TextRange FindText(String input)
      {
         TextPointer position = Document.ContentStart;
         return FindNext(ref position, input);
      }

      /// <summary>
      /// Find the input string within the document, starting at the specified position.
      /// </summary>
      /// <param name="position">the current text position</param>
      /// <param name="input">input text</param>
      /// <returns>An <see cref="TextRange"/> represeneting the (next) matching string within the text container.</returns>
      public TextRange FindNext(ref TextPointer position, String input)
      {
         TextRange result = null;
         if (position.CompareTo(Document.ContentEnd) == 0)
         {
            position = Document.ContentStart;
         }

         result = DocumentHelper.FindText( position, Document.ContentEnd, input, DocumentHelper.FindFlags.None, CultureInfo.CurrentCulture);
         if (result != null)
         {
            position = result.End;
            Selection.Select(result.Start, result.End);
         }
         else
         {
            position = Document.ContentEnd;
            Selection.Select(position, position);
         }

         double top = this.PointToScreen(position.GetLineStartPosition(0).GetCharacterRect(LogicalDirection.Forward).TopLeft).Y;
         Trace.WriteLine(string.Format(" Top: {0}, CharOffset: {1}", top, position));
         ScrollViewer.ScrollToVerticalOffset(top);
         //this.BringIntoView( );

         return result;
      }



      /// <summary>
      /// An ugly internal search helper (which uses reflection, and "just works")
      /// </summary>
      private static class DocumentHelper
      {
         private static MethodInfo findMethod = null;
         [Flags]
         public enum FindFlags
         {
            None = 0,
            MatchCase = 1,
            FindInReverse = 2,
            FindWholeWordsOnly = 4,
            MatchDiacritics = 8,
            MatchKashida = 16,
            MatchAlefHamza = 32,
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
                     new Object[] { findContainerStartPosition,
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
      } 
   }
}
