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
    /// Here we EXPLICITLY implement the IPSConsole interface.
    /// Importantly, this implementation just calls the existing methods on the our ConsoleRichTextBox class
    /// Each call is wrapped in Dispatcher methods so that the interface is thread-safe!
    /// </summary>
    public partial class ConsoleControl : IPSConsole  //, IPSConsole, IConsoleControlBuffered
    {
        // Possibly an alternative panel that pops up and can be closed?
        #region IPSConsole Members


       // the PopupMenu uses these two things...
       private TabExpansion _expansion;
       private CommandHistory _cmdHistory;
       private PopupMenu _popup;
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


        IPSRawConsole IPSConsole.RawUI
        {
           get { return this; }
        }

        #region ReadLine 

        private readonly AutoResetEvent _gotInput = new AutoResetEvent(false);
        private string _lastInputString = null;
        public bool _waitingForInput = false;

        /// <summary>
        /// Handles the CommandEntered event of the Console buffer
        /// </summary>
        /// <param name="command">The command.</param>
        private void OnCommand(string command)
        {
            if (_waitingForInput)
            {
                //if (command.EndsWith("\n"))
                //{
                _lastInputString = command;
                //}
                //else
                //{
                //    lastInputString = command + "\n";
                //}
                _gotInput.Set();
            }
            else if(Command != null)
            {
               Command(this, new CommandEventArgs{Command = command, OutputBlock = _current } );
               _cmdHistory.Add(command); 
            }
        }

	
        /// <summary>
        /// Provides a way for scripts to request user input ...
        /// </summary>
        /// <returns></returns>
        string IPSConsole.ReadLine()
        {
            _waitingForInput = true;
            _gotInput.WaitOne();
            _waitingForInput = false;

            return _lastInputString;
        }
        #endregion ReadLine

        void IPSConsole.Write(string message) {
           Write(null, null, message, _current);
        }

        void IPSConsole.Write(string message, Block target)
        {
           // Write is Dispatcher checked
           Write(null, null, message, target);

        }

        void IPSConsole.Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message)
        {
           // Write is Dispatcher checked
           Write(foregroundColor, backgroundColor, message, _current);
        }
        void IPSConsole.Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message, Block target)
        {
           // Write is Dispatcher checked
           Write(foregroundColor, backgroundColor, message, target);
        }

        void IPSConsole.WriteLine(string message)
        {
           ((IPSConsole)this).WriteLine(message, _current);
        }
        void IPSConsole.WriteLine(string message, Block target)
        {
           // Write is Dispatcher checked
           Write(null, null, message + "\n", target);
        }

        void IPSConsole.WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message)
        {
           // Write is Dispatcher checked
           Write(foregroundColor, backgroundColor, message + "\n", _current);
        }
        void IPSConsole.WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string message, Block target)
        {
           // Write is Dispatcher checked
           Write(foregroundColor, backgroundColor, message + "\n", target);
        }

        void IPSConsole.WriteDebugLine(string message)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.DebugForeground, _consoleBrushes.DebugBackground, String.Format("DEBUG: {0}\n", message), _current);
        }

       void IPSConsole.WriteDebugLine(string message, Block target)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.DebugForeground, _consoleBrushes.DebugBackground, String.Format("DEBUG: {0}\n", message), target);
        }


        void IPSConsole.WriteErrorRecord(ErrorRecord errorRecord)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.ErrorForeground, _consoleBrushes.ErrorBackground, errorRecord + "\n", _current);
            if (errorRecord.InvocationInfo != null)
            {
               // Write is Dispatcher checked
               Write(_consoleBrushes.ErrorForeground, _consoleBrushes.ErrorBackground, errorRecord.InvocationInfo.PositionMessage + "\n", _current);
            }
        }


        void IPSConsole.WriteErrorLine(string message)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.ErrorForeground, _consoleBrushes.ErrorBackground, message + "\n", _current);
        }

        void IPSConsole.WriteVerboseLine(string message)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.VerboseForeground, _consoleBrushes.VerboseBackground, String.Format("VERBOSE: {0}\n", message), _current);
        }

        void IPSConsole.WriteWarningLine(string message)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.WarningForeground, _consoleBrushes.WarningBackground, String.Format("WARNING: {0}\n", message), _current);
        }

        void IPSConsole.WriteNativeLine(string message)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.NativeOutputForeground, _consoleBrushes.NativeOutputBackground, message + "\n", _current);
           Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() => SetPrompt()));
           // TODO: REIMPLEMENT NATIVE prompt using Begin/End and Prompt()
        }

        void IPSConsole.WriteNativeErrorLine(string message)
        {
           // Write is Dispatcher checked
           Write(_consoleBrushes.NativeErrorForeground, _consoleBrushes.NativeErrorBackground, message + "\n", _current);
           Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() => SetPrompt()));
        }

        #endregion
    }
}