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
using Huddled.WPF.Controls.Interfaces;
using System.Management.Automation.Runspaces;

namespace Huddled.WPF.Controls
{
    /// <summary>
    /// Here we EXPLICITLY implement the IPSConsoleControl interface.
    /// Importantly, this implementation just calls the existing methods on the our ConsoleRichTextBox class
    /// Each call is wrapped in Dispatcher methods so that the interface is thread-safe!
    /// </summary>
    public partial class ConsoleControl : IPoshConsoleControl  //, IPSConsole, IConsoleControlBuffered
    {
        //public event TabCompleteHandler TabComplete;
        //public event HistoryHandler GetHistory;
       public event CommmandDelegate Command;
       private int _id = 0;

        /// <summary>
        /// Right before a prompt we want to insert a new paragraph...
        /// But we want to trim any whitespace off the end of the output first 
        /// because the paragraph mark makes plenty of whitespace
        /// </summary>
        void IPoshConsoleControl.CommandFinished(System.Management.Automation.Runspaces.PipelineState results)
        {
           _id++;
            //// NOTE: we have to use the dispatcher, otherwise this might complete before the command output
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate {
               //_current.Name = "Output"+_id;
               //_current.Tag = results;
               _current.Tag = _id;

               if (results != System.Management.Automation.Runspaces.PipelineState.Completed
                    && results != System.Management.Automation.Runspaces.PipelineState.NotStarted)
                {

                    ((IPSConsole)this).WriteVerboseLine("PowerShell Pipeline is: " + results);
                }
            });
        }



        //ConsoleScrollBarVisibility IPoshConsoleControl.VerticalScrollBarVisibility
        //{
        //    get
        //    {
        //        return (ConsoleScrollBarVisibility)Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate { return base.VerticalScrollBarVisibility; });
        //    }
        //    set
        //    {
        //       Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { base.VerticalScrollBarVisibility = (ScrollBarVisibility)value; });
        //    }
        //}

        //ConsoleScrollBarVisibility IPoshConsoleControl.HorizontalScrollBarVisibility
        //{
        //    get
        //    {
        //       return (ConsoleScrollBarVisibility)Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate { return base.HorizontalScrollBarVisibility; });
        //    }
        //    set
        //    {
        //       Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { base.HorizontalScrollBarVisibility = (ScrollBarVisibility)value; });
        //    }
        //}

        TextBox IPoshConsoleControl.CommandBox
        {
           get
           {
              return _commandBox;
           }
        }

        // Using a DependencyProperty as the backing store for CaretColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CaretColorProperty =
            DependencyProperty.Register("CaretColor", typeof(Color), typeof(ConsoleControl), new UIPropertyMetadata(Colors.White));

        public Color CaretColor
        {
           get { return (Color)GetValue(CaretColorProperty); }
           set { SetValue(CaretColorProperty, value); }
        }
       
        //, CaretColorChanged
        //public static void CaretColorChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        //{
        //   var console = source as ConsoleControl;
        //   if (console != null)
        //   {
        //      Color c = (Color)args.NewValue;
        //      console._commandBox.Background = new SolidColorBrush();
        //   }
        //}

        //public Runspace Runspace
        //{
        //   get; set;
        //}
    }
 }