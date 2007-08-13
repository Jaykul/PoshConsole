using System;
using System.Collections.Generic;
using System.Text;

namespace Huddled.PoshConsole
{
    public delegate List<string> TabExpansionLister(string commandLine);

    public class TabExpansion
    {
        public event TabExpansionLister TabComplete;

        private int _index;
        private string _command;
        private List<string> _choices;

        public TabExpansion()
        {
            _choices = new List<string>();
            TabComplete += new TabExpansionLister(delegate(string cmd) { return new List<string>(); });
        }

        //private IPoshConsoleService _service;
        //public void SetService(IPoshConsoleService service)
        //{
        //    _service = service;
        //}

        public List<string> GetChoices(string currentCommand)
        {
            if( (_choices == null || _choices.Count == 0) 
             && (_command == null || _command != currentCommand) ) 
            {
                _command = currentCommand;
                _choices = TabComplete(currentCommand);
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

        private string Move(string currentCommand, int direction)
        {
            if (_choices.Count == 0)
            {
                GetChoices(currentCommand);
            }
            else
            {
                _index += direction;
            }

            if (_index < 0)
            {
                // wrap around
                _index = _choices.Count - 1;
            }

            if (_index >= _choices.Count)
            {
                // wrap around
                _index = 0;
            }

            if (_choices.Count > 0)
            {
                return _choices[_index];
            }
            else
            {
                return _command;
            }
        }
    }
}
