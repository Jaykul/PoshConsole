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
   /// The ConsoleControl is a <see cref="RichTextBox"/> where all input goes to a sub-textbox after the "prompt"
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
      }
      public override void EndInit()
      {
         base.EndInit();
         Document.Background = Background;
         Document.Foreground = Foreground;
         Document.PagePadding = Padding;
         Padding = new Thickness(0.0);

         _commandBox.Foreground = Foreground;
         _commandBox.Background = Background;

         BindingOperations.SetBinding(Document, FlowDocument.FontFamilyProperty, new Binding("FontFamily") { Source = this });
         BindingOperations.SetBinding(Document, FlowDocument.FontSizeProperty, new Binding("FontSize") { Source = this });
         BindingOperations.SetBinding(Document, FlowDocument.FontFamilyProperty, new Binding("FontFamily") { Source = this });


         ProcessStartup();
      }

      protected virtual void ProcessStartup()
      {
         // TODO: replace this standin with an actual startup script
         _current = new Paragraph(new Run("There once was a man from nantucket,\nWho ran from the room with a bucket ..."));
         Document.Blocks.Add(_current);

         // create the prompt, and stick the command block in it
         _next = new Paragraph();
         Document.Blocks.Add(_next);
         _next.Inlines.Add(_commandContainer);
         Prompt("[0]: ");

      }


      private void SetPrompt()
      {
         _commandContainer.Focus();
         _commandBox.Focus();
         UpdateLayout();
         _next.BringIntoView();
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
