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
         Document = new FlowDocument();
         //ViewingMode = FlowDocumentReaderViewingMode.Scroll;
         Margin = new Thickness(0.0);
         Padding = new Thickness(2.0);

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

         Properties.Colors.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ColorsPropertyChanged);
         Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SettingsPropertyChanged);

      }
      public override void EndInit()
      {
         base.EndInit();
         Document.PagePadding = Padding;
         Padding = new Thickness(0.0);

         // we have to (manually) bind the document and _commandBox values to the "real" ones...
         BindingOperations.SetBinding(Document, FlowDocument.FontFamilyProperty, new Binding("FontFamily") { Source = this });
         BindingOperations.SetBinding(Document, FlowDocument.FontSizeProperty, new Binding("FontSize") { Source = this });

         BindingOperations.SetBinding(Document, FlowDocument.BackgroundProperty, new Binding("Background") { Source = this });
         BindingOperations.SetBinding(Document, FlowDocument.ForegroundProperty, new Binding("Foreground") { Source = this });

         // BindingOperations.SetBinding(_commandBox, FlowDocument.BackgroundProperty, new Binding("Background") { Source = this });
         BindingOperations.SetBinding(_commandBox, FlowDocument.ForegroundProperty, new Binding("Foreground") { Source = this });

         ProcessStartup();
      }

      protected virtual void ProcessStartup()
      {
         // TODO: replace this standin with an actual startup script
         _current = new Paragraph();
         Document.Blocks.Add(_current);

         // create the prompt, and stick the command block in it
         _next = new Paragraph();
         Document.Blocks.Add(_next);
         _next.Inlines.Add(_commandContainer);
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
               _next.Inlines.Remove(_commandContainer);
               //_next.Inlines.Clear();
               _next.Inlines.AddRange(new Inline[] { insert, _commandContainer });
               SetPrompt();
               // TODO: LimitBuffer();
            }
         });

      }

      private void SetPrompt()
      {
         UpdateLayout();
         _commandBox.Focus();
         _next.BringIntoView();
      }



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

      private void Write(Brush foreground, Brush background, string text, Block target)
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
         {
            //BeginChange();
            // handle null values - so that other Write* methods don't have to deal with Dispatchers just to look up a color
            if (foreground == null) foreground = Foreground;
            if (background == null) background = Background;//Brushes.Transparent;

            if (target == null) target = _current;
            //if (_current == null)
            //{
            //   _current = new Paragraph();
            //   Document.Blocks.Add(_current);
            //}

            Run insert = new Run(text, target.ContentEnd);
            insert.Background = background;
            insert.Foreground = foreground;
         });
         _next.BringIntoView();
         //ScrollToEnd();
         //EndChange();
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
         string cmd = _commandBox.Text;
         _current = _next;
         _next = new Paragraph();
         Document.Blocks.Add(_next);

         _commandBox.Text = "";
         _current.Inlines.Add(new Run(cmd));
         _current.Inlines.Add(new LineBreak());
         // move the input to the NEXT prompt line
         _current.Inlines.Remove(_commandContainer);
         _next.Inlines.Add(_commandContainer);
         UpdateLayout();

         // _cmdHistory.Add(_commandBox.Text);

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
