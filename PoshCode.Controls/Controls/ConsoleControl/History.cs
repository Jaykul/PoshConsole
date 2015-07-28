using System;
using System.Collections.Generic;

namespace PoshCode.Controls
{
    public class CommandHistory
    {
        
		#region [rgn] Fields (3)

		private String _currentCommand;
		//private readonly IPSConsoleControl _control;
        private readonly List<String> _history;
		private Int32 _index;

		#endregion [rgn]

		#region [rgn] Constructors (1)

		public CommandHistory(/*ConsoleRichTextBox control*/)
        {
            _history = new List<String>();
            //_control = control;
            _index = -1;
        }
		
		#endregion [rgn]

		#region [rgn] Methods (8)

		// [rgn] Public Methods (7)

		public void Add(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                _history.Add(command);
            }
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
		    return currentCommand;
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
		
		public string Next(string currentCommand)
		{
		    if (_index != -1)
            {
                long length = _history.Count;

                if (_index < length - 1)
                {
                    return _history[++_index];
                }
                if (_index == length - 1)
                {
                    return Last(currentCommand);
                }
                return currentCommand;
            }
		    return currentCommand;
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
                if (_index > 0)
                {
                    return _history[--_index];
                }
                return currentCommand;
            }
            return currentCommand;
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
		
		// [rgn] Internal Methods (1)

		internal List<string> GetChoices(string CurrentCommand)
        {
            return _history;
        }
		
		#endregion [rgn]

    }
}