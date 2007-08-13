using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Huddled.PoshConsole
{
    public class CommandHistory
    {
        //private readonly IPSConsoleControl _control;
        private readonly List<String> _history;

        private Int32 _index;
        private String _currentCommand;

        public CommandHistory(/*ConsoleRichTextBox control*/)
        {
            _history = new List<String>();
            //_control = control;
            _index = -1;
        }

        public void AddEntry(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                _history.Add(command);
            }
        }

        public void Reset()
        {
            _index = -1;
            ResetCurrentCommand();
        }

        public void ResetCurrentCommand()
        {
            _currentCommand = string.Empty;
        }

        public string Previous(string currentCommand)
        {
            if (_history.Count > 0)
            {
                if (_index == -1)
                {
                    _currentCommand = currentCommand;

                    _index = _history.Count - 1;
                    return _history[_index];
                }
                else if (_index > 0)
                {
                    return _history[--_index];
                }
                else return currentCommand;

            }
            else return currentCommand;
        }

        public string Next(string currentCommand)
        {
            if (_index != -1)
            {
                long length = _history.Count;

                if (_index < length - 1)
                {
                    return _history[++_index];
                }
                else if (_index == length - 1)
                {
                    return Last(currentCommand);
                }
                else return currentCommand;
            }
            else return currentCommand;
        }

        public string First(string currentCommand)
        {
            if (_history.Count > 0)
            {
                if (_index == -1)
                {
                    _currentCommand = currentCommand;
                }

                _index = 0;
                return _history[0];
            }
            else return currentCommand;
        }

        public string Last(string currentCommand)
        {
            if (_index == -1)
            {
                return currentCommand;
            }

            _index = -1;
            return _currentCommand;
        }

        internal List<string> GetChoices(string CurrentCommand)
        {
            return _history;
        }
    }
}