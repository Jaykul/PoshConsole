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

   public class CommandEventArgs
   {
      public string Command;
   }

   public delegate void CommmandDelegate(Object source, CommandEventArgs command);


   /// <summary>
   /// The ConsoleControl is a <see cref="RichTextBox"/> where all input goes to a sub-textbox after the "prompt"
   /// </summary>
   public partial class ConsoleControl : FlowDocumentReader
   {
      static ConsoleControl()
      {
         // DefaultStyleKeyProperty.OverrideMetadata(typeof(ConsoleControl), new FrameworkPropertyMetadata(typeof(ConsoleControl)));
      }

      private TextBox _commandBox;
      private InlineUIContainer _commandContainer;
      private Paragraph _current;
      private Paragraph _next;
      private Inline _prompt;
      private Popup _popup;

      public event CommmandDelegate Command;

      public ConsoleControl()
      {
         _popup = new Popup();
         // Add the popup to the logical branch of the console so keystrokes can be
         // processed from the popup by the console for the tab-complete scenario.
         // E.G.: $Host.Pri[tab].  => "$Host.PrivateData." instead of swallowing the period.
         AddLogicalChild(_popup);
         this.Document = new FlowDocument();
         this.ViewingMode = FlowDocumentReaderViewingMode.Scroll;
         this.Margin = new Thickness(0.0);
         this.Padding = new Thickness(2.0);


         _next = new Paragraph();
         _current = new Paragraph(new Run(@"
There once was a man from nantucket,
Who ran from the room with a bucket ..."));
         Document.Blocks.Add(_current);

         _commandBox = new TextBox() { 
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
         _next.Inlines.Add(_commandContainer);
         Prompt("[" + (i++) + "]: ");
      }

      private int i = 0;
      private void SetPrompt()
      {
         _commandContainer.Focus();
         _commandBox.Focus();
      }

      public void Prompt(string prompt)
      {
         //Dispatcher.ExitAllFrames();
         Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
         {
            if (_next != null)
            {
               Run insert = new Run(prompt, _next.ContentStart);
               insert.Background = Brushes.Transparent;
               insert.Foreground = Brushes.Gold;
               SetPrompt();
               // TODO: LimitBuffer();
            }
         });

      }



      private void Write(Brush foreground, Brush background, string text)
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)delegate
         {
            //BeginChange();
            // handle null values - so that other Write* methods don't have to deal with Dispatchers just to look up a color
            if (foreground == null) foreground = Foreground;
            if (background == null) background = Background;//Brushes.Transparent;

            if (_current == null)
            {
               _current = new Paragraph();
               Document.Blocks.Add(_current);
            }

            Run insert = new Run(text, _current.ContentEnd);
            insert.Background = background;
            insert.Foreground = foreground;
         });
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
         Trace.TraceInformation("Entering OnPreviewKeyDown:");
         Trace.Indent();

         Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action) delegate {
            switch (e.Key)
            {
               case Key.Enter:
                  OnEnterPressed(e);
                  break;

            }
         });
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

         if (Command != null)
         {
            Command(this, new CommandEventArgs { Command = cmd });
         }
         // TESTING
         BackgroundWorker worker = new BackgroundWorker();
         worker.DoWork +=new DoWorkEventHandler(worker_DoWork);
         worker.RunWorkerAsync(i);

         e.Handled = true;
      }

      void worker_DoWork(object sender, DoWorkEventArgs e)
      {
         Random r = new Random();
         int max = r.Next(2, 6);
         for (int j = 0; j < max; j++)
         {
            Write(Brushes.Navy, Brushes.Transparent, "This is a test ("+e.Argument+") of the emergency broadcast system\r\n");
            System.Threading.Thread.Sleep(r.Next(200,800));
         }
         Prompt("[" + (i++) + "]: ");
      }
      //protected override void OnKeyDown(KeyEventArgs e)
      //{
      //   base.OnKeyDown(e);
      //}
   }
}
