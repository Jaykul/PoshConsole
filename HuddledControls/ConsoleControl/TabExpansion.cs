using System.Collections.Generic;
using Huddled.WPF.Controls.Utility;

namespace Huddled.WPF.Controls
{
    public delegate List<string> TabExpansionLister(string commandLine);

    public class TabExpansion
    {
        
      #region [rgn] Fields (3)

      private List<string> _choices;
      private string _command;
      private int _index;

      #endregion [rgn]

      #region [rgn] Constructors (1)

      public TabExpansion()
        {
            _choices = new List<string>();
            TabComplete += cmd => new List<string>();
        }
      
      #endregion [rgn]

      #region [rgn] Delegates and Events (1)

      // [rgn] Events (1)

      public event TabExpansionLister TabComplete;
      
      #endregion [rgn]

      #region [rgn] Methods (5)

      // [rgn] Public Methods (4)

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
      
      // [rgn] Private Methods (1)

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
                string last = _command.GetLastWord();
                return _command.Remove(_command.Length - last.Length) + _choices[_index];
            }
         return _command;
        }
      
      #endregion [rgn]

    }
}
