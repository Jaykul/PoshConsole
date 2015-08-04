using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;

namespace PoshCode.Controls
{

    public class TabExpansion
    {

        #region [rgn] Fields (3)

        private CommandCompletion _choices;
        private string _command;
        private int _index;

        #endregion [rgn]

        #region [rgn] Constructors (1)

        public TabExpansion()
        {
            TabComplete = null;
        }

        #endregion [rgn]

        #region [rgn] Delegates and Events (1)

        // [rgn] Events (1)

        public Func<string, int, CommandCompletion> TabComplete { get; set; }

        #endregion [rgn]

        #region [rgn] Methods (5)

        // [rgn] Public Methods (4)

        //private IPoshConsoleService _service;
        //public void SetService(IPoshConsoleService service)
        //{
        //    _service = service;
        //}
        public CommandCompletion GetChoices(string currentCommand, int cursorIndex)
        {
            ConsoleControl.TabExpansionTrace.TraceEvent(TraceEventType.Information, 1, "GetChoices for '{0}'", currentCommand);
            if (_command != currentCommand || (_choices == null || _choices.CompletionMatches.Count == 0) && (_command == null || _command != currentCommand))
            {
                if (TabComplete == null)
                    return null;

                _command = currentCommand;
                _choices = TabComplete(currentCommand, cursorIndex);
            }

            if (ConsoleControl.TabExpansionTrace.Switch.Level >= SourceLevels.Information)
            {
                ConsoleControl.TabExpansionTrace.TraceEvent(TraceEventType.Information, 2, "Choice List:");
                foreach (var ch in _choices.CompletionMatches.Select(m => m.ListItemText))
                {
                    ConsoleControl.TabExpansionTrace.TraceEvent(TraceEventType.Information, 2, ch);
                }
            }
            return _choices;
        }

        public string Next(string currentCommand)
        {
            return Move(currentCommand, 1);
        }

        public string Previous(string currentCommand)
        {
            return Move(currentCommand, -1);
        }

        public void Reset()
        {
            _index = 0;
            _command = null;
            _choices = null;
        }

        // [rgn] Private Methods (1)

        private string Move(string currentCommand, int direction)
        {
            var count = _choices.CompletionMatches.Count;
            if (count == 0)
            {
                GetChoices(currentCommand, 0);
            }
            else
            {
                _index += direction;
            }

            if (_index < 0)
            {
                // wrap around
                _index = count - 1;
            }

            if (_index >= count)
            {
                // wrap around
                _index = 0;
            }

            if (count > 0)
            {
                return _choices.CompletionMatches[_index].CompletionText;
            }
            return _command;
        }

        #endregion [rgn]

    }
}
