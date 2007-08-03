using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Huddled.PoshConsole
{
    public class InputHandler : IInput
    {
        private AutoResetEvent GotInput = new AutoResetEvent(false);
        public event InputEventHandler GotUserInput;
        private string lastInputString = null;
        public bool waitingForInput = false;

        public InputHandler()
        {
            // add a do-nothing delegate so we don't have to test for it
            GotUserInput += new InputEventHandler(delegate(object o, string cmd) { });
        }

        /// <summary>
        /// Handles the CommandEntered event of the Console buffer
        /// </summary>
        /// <param name="command">The command.</param>
        public void Write(string command)
        {
            if (waitingForInput)
            {
                lastInputString = command;
                GotInput.Set();
            }
            else 
            {
                GotUserInput( this, command);
            }
        }

        /// <summary>
        /// Provides a way for scripts to request user input ...
        /// </summary>
        /// <returns></returns>
        public string Read()
        {
            string result = null;

            waitingForInput = true;
            GotInput.WaitOne();
            waitingForInput = false;

            result = lastInputString;
            return result;
        }
    }
}
