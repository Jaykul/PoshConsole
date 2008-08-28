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
        #region IPSConsoleControl Members

        /// <summary>
        /// Right before a prompt we want to insert a new paragraph...
        /// But we want to trim any whitespace off the end of the output first 
        /// because the paragraph mark makes plenty of whitespace
        /// </summary>
        void IPoshConsoleControl.CommandFinished(System.Management.Automation.Runspaces.PipelineState results)
        {
            //// NOTE: we have to use the dispatcher, otherwise this might complete before the command output
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate {
                if (results != System.Management.Automation.Runspaces.PipelineState.Completed
                    && results != System.Management.Automation.Runspaces.PipelineState.NotStarted)
                {
                    ((IPSConsole)this).WriteVerboseLine("PowerShell Pipeline is: " + results);
                }
                TrimOutput();
            });
        }


        private void TrimOutput()
        {
            if (_current != null && _current.ContentEnd.IsInSameDocument(Document.ContentEnd))
            {
                //// I'm having issues with an extra paragraph getting inserted in the output...
                //if (!_current.Equals(Document.Blocks.LastBlock.ContentEnd.Paragraph))
                //{
                //    TextRange tr = new TextRange(_current.ContentEnd, Document.Blocks.LastBlock.ContentEnd);
                //    if (tr.Text.TrimEnd().Length == 0)
                //    {
                //        Document.Blocks.Remove(Document.Blocks.LastBlock);
                //    }
                //}

                // and extra lines too...
                // if the paragraph has content
                if (_current.Inlines.Count > 0)
                {
                    // trim from the end until we run out of inlines or hit some non-whitespace
                    Inline ln = _current.Inlines.LastInline;
                    while (ln != null)
                    {
                        Run run = ln as Run;
                        if (run != null)
                        {
                            run.Text = run.Text.TrimEnd();
                            // if there's text in this run, stop trimming!!!
                            if (run.Text.Length > 0) break;
                            ln = ln.PreviousInline;
                            _current.Inlines.Remove(run);
                        }
                        else if (ln is LineBreak)
                        {
                            Inline tmp = ln;
                            ln = ln.PreviousInline;
                            _current.Inlines.Remove(tmp);
                        }
                        else break;
                    }
                }
            }
            //// paragraph break before each prompt ensure the command and it's output are in a paragraph on their own
            //// This means that the paragraph select key (and triple-clicking) gets you a command and all it's output
            // _current.ContentEnd.InsertParagraphBreak();
            _current = _next;
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
        #endregion
    }
}