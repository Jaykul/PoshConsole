using System;
using System.Collections.Generic;
using System.Text;

namespace Huddled.PoshConsole
{
    public delegate void InputEventHandler(object source, string commandLine);
    public interface IInput
    {
        event InputEventHandler GotUserInput;

        /// <summary>
        /// Writes the input.
        /// </summary>
        /// <param name="command">The command.</param>
        void Write(string command);

        /// <summary>
        /// Reads the input.
        /// </summary>
        /// <returns></returns>
        string Read();
 
    }
}
