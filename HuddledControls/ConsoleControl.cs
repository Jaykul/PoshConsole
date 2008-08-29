using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Huddled.WPF.Controls
{

   /// <summary>
   /// The ConsoleControl is a <see cref="FlowDocumentScrollViewer"/> where all input goes to a sub-textbox after the "prompt"
   /// </summary>
   public partial class ConsoleControl : FlowDocumentScrollViewer
   {
      static readonly ConsoleBrushes _consoleBrushes = new ConsoleBrushes();

      static ConsoleControl()
      {
         // DefaultStyleKeyProperty.OverrideMetadata(typeof(ConsoleControl), new FrameworkPropertyMetadata(typeof(ConsoleControl)));
      }


      private readonly TextBox _commandBox;
      private readonly InlineUIContainer _commandContainer;
      protected Paragraph _current;
      protected Paragraph _next;
     

      public ConsoleControl()
      {
         _popup = new Popup();
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
                             /*AcceptsReturn = true, */
                             Margin = new Thickness(0.0),
                             Padding = new Thickness(0.0),
                             BorderThickness = new Thickness(0.0),
                             Background = Brushes.Transparent
                          };
         _commandContainer = new InlineUIContainer(_commandBox) { BaselineAlignment = BaselineAlignment.Center };

         //Document = new FlowDocument
         //              {
         //                  PagePadding = new Thickness(2.0),
         //                  IsOptimalParagraphEnabled = true,                           
         //              };
         //ViewingMode = FlowDocumentReaderViewingMode.Scroll;
         Margin = new Thickness(0.0);
         Padding = new Thickness(0.0);

      }
      public override void EndInit()
      {
         base.EndInit();

         //// Initialize the document ...

         _current = new Paragraph();
         Document.Blocks.Add(_current);

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
         BindingOperations.SetBinding(_commandBox, FlowDocument.ForegroundProperty, new Binding("Foreground") { Source = this });

      }

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
               // the problem is, the prompt might have used Write-Host
               // so we need to move the _commandContainer to the end.
               _next.Inlines.Remove(_commandContainer);
               _next.Inlines.Add(insert);
               _next.Inlines.Add(_commandContainer);
               SetPrompt();
            }
         });

      }

      private void SetPrompt()
      {
         UpdateLayout();
         _commandBox.Focus();
         _commandContainer.BringIntoView();
      }

      //private void TrimOutput()
      //{
      //   if (_current != null)
      //   {
      //      // and extra lines too...
      //      // if the paragraph has content
      //      if (_current.Inlines.Count > 0)
      //      {
      //         // trim from the end until we run out of inlines or hit some non-whitespace
      //         Inline ln = _current.Inlines.LastInline;
      //         while (ln != null)
      //         {
      //            Run run = ln as Run;
      //            if (run != null)
      //            {
      //               run.Text = run.Text.TrimEnd();
      //               // if there's text in this run, stop trimming!!!
      //               if (run.Text.Length > 0) break;
      //               ln = ln.PreviousInline;
      //               _current.Inlines.Remove(run);
      //            }
      //            else if (ln is LineBreak)
      //            {
      //               Inline tmp = ln;
      //               ln = ln.PreviousInline;
      //               _current.Inlines.Remove(tmp);
      //            }
      //            else break;
      //         }
      //      }
      //   }
      //   //// paragraph break before each prompt ensure the command and it's output are in a paragraph on their own
      //   //// This means that the paragraph select key (and triple-clicking) gets you a command and all it's output
      //   // _current.ContentEnd.InsertParagraphBreak();
      //   _current = _next;
      //}

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
            ConsoleControlObj.Background = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)e.NewValue);
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
            ConsoleControlObj.Foreground = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)e.NewValue);
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
            _commandContainer.BringIntoView();
         });
      }

      private void Write(ConsoleColor foreground, ConsoleColor background, string text, Block target)
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
         {
            if (target == null) target = _current;

            new Run(text, target.ContentEnd)
            {
               Background = _consoleBrushes.BrushFromConsoleColor(background),
               Foreground = _consoleBrushes.BrushFromConsoleColor(foreground)
            };
            _commandContainer.BringIntoView();
         });
      }

      protected override void OnPreviewTextInput(TextCompositionEventArgs e)
      {
         _commandBox.Focus();
         //_commandBox.RaiseEvent(e);
         base.OnPreviewTextInput(e);
      }
      protected override void OnPreviewKeyDown(KeyEventArgs e)
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
         {
            switch (e.Key)
            {
               case Key.Enter:
                  OnEnterPressed(e);
                  break;

            }
         });
      }
      protected virtual void FlushInputBuffer()
      {
         _commandBox.Clear();
      }

      private void OnEnterPressed(KeyEventArgs e)
      {
         // get the command
         string cmd = _commandBox.Text;
         // clear the box
         _commandBox.Text = "";
         // put the text in instead
         _next.Inlines.InsertBefore(_commandContainer, new Run(cmd + "\n"));
         // move the box to the NEXT location
         _next.Inlines.Remove(_commandContainer);
         // and ... NOW, this is the destination for output
         _current = _next; 
         // and the new prompt will go down here
         _next = new Paragraph(_commandContainer);
         Document.Blocks.Add(_next);
         

         UpdateLayout();

         OnCommand(cmd);
         e.Handled = true;
      }

      public string CurrentCommand
      {
         get { return _commandBox.Text; }
         set { _commandBox.Text = value; }
      }

      private string _title;
      public string Title
      {
         get { return _title; }
         set { _title = value; }
      }


   }
}
