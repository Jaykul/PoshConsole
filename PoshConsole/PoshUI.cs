 //
// Copyright (c) 2006 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
// PARTICULAR PURPOSE.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Huddled.PoshConsole
{
    /// <summary>
    /// Sample implementation of the PSHostUserInterface class.
    /// Note that not all members of this class are implemented. 
    /// Members that are not implemented usually throw a
    /// NotImplementedException exception although some just do 
    /// nothing and silently return. Members that map onto the 
    /// existing .NET console APIs are supported. The credential and 
    /// secure string methods are not supported.
    /// </summary>
    class PoshUI : PSHostUserInterface
    {
        private PoshRawUI myRawUi = null;
        private IConsoleControl myConsole = null;
        private IInput myInput = null;

        public PoshUI(PoshRawUI rawUI, IConsoleControl console, IInput handler)
        {
            myRawUi = rawUI;
            myConsole = console;
            myInput = handler;
        }

        public override PSHostRawUserInterface RawUI
        {
            get
            {
                return myRawUi;
            }
        }


        ///// <summary>
        ///// Indicate to the host application that exit has
        ///// been requested. Pass the exit code that the host
        ///// application should use when exiting the process.
        ///// </summary>
        ///// <param name="exitCode"></param>
        //public void SetShouldExit(int exitCode)
        //{
        //    Write("We should EXIT now with exit code " + exitCode);
        //    //program.ShouldExit = true;
        //    //program.ExitCode = exitCode;
        //}

        public override Dictionary<string, PSObject> Prompt( 
            string caption, string message, Collection<FieldDescription> descriptions)
        {

            WriteLine(ConsoleColor.Blue, ConsoleColor.Black, caption + "\n" + message + " ");

            Dictionary<string, PSObject> results = new Dictionary<string, PSObject>();
            foreach (FieldDescription fd in descriptions)
            {
                string[] label = GetHotkeyAndLabel(fd.Label);
                
                if( !String.IsNullOrEmpty( fd.HelpMessage ) ) Write( fd.HelpMessage);

                WriteLine(ConsoleColor.Blue, ConsoleColor.Black, String.Format("\n{0}: ", label[1]));

                string userData = ReadLine();
                if (userData == null)
                    return null;
                results[fd.Name] = PSObject.AsPSObject(userData);
            }
            return results;
        }

        public override int PromptForChoice(
                string caption, string message,
                Collection<ChoiceDescription> choices, int defaultChoice)
        {
            // Write the caption and message strings in Blue.
            WriteLine(ConsoleColor.Blue, ConsoleColor.Black, caption + "\n" + message + "\n");

            // Convert the choice collection into something that's a little easier to work with
            // See the BuildHotkeysAndPlainLabels method for details.
            Dictionary<string, PSObject> results = new Dictionary<string, PSObject>();
            string[,] promptData = BuildHotkeysAndPlainLabels(choices);

            // Format the overall choice prompt string to display...
            StringBuilder sb = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                sb.Append(String.Format("|{0}> {1} ", promptData[0, element], promptData[1, element]));
            }
            sb.Append(String.Format("[Default is ({0}]", promptData[0, defaultChoice]));

            // Loop reading prompts until a match is made, the default is
            // chosen or the loop is interrupted with ctrl-C.
            while (true)
            {
                WriteLine(ConsoleColor.Cyan, ConsoleColor.Black, sb.ToString());
                string data = ReadLine().Trim().ToUpper(CultureInfo.CurrentCulture);

                // If the choice string was empty, use the default selection.
                if (data.Length == 0)
                    return defaultChoice;

                // See if the selection matched and return the
                // corresponding index if it did...
                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == data)
                        return i;
                }
                WriteErrorLine("Invalid choice: " + data);
            }
        }

        /// <summary>
        /// Parse a string containing a hotkey character.
        /// 
        /// Take a string of the form: 
        /// "Yes to &amp;all"
        /// And return a two-dimensional array split out as
        ///    "A", "Yes to all".
        /// </summary>
        /// <param name="input">The string to process</param>
        /// <returns>
        /// A two dimensional array containing the parsed components.
        /// </returns>
        private static string[] GetHotkeyAndLabel(string input)
        {
            string[] result = new string[] { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0)
                    result[0] = fragments[1][0].ToString().ToUpper(CultureInfo.CurrentCulture);
                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else
            {
                result[1] = input;
            }
            return result;
        }

        /// <summary>
        /// This is a private worker function that splits out the
        /// accelerator keys from the menu and builds a two dimentional 
        /// array with the first access containing the
        /// accelerator and the second containing the label string
        /// with &amp; removed.
        /// </summary>
        /// <param name="choices">The choice collection to process</param>
        /// <returns>
        /// A two dimensional array containing the accelerator characters
        /// and the cleaned-up labels</returns>
        private static string[,] BuildHotkeysAndPlainLabels(
                Collection<ChoiceDescription> choices)
        {
            // Allocate the result array
            string[,] hotkeysAndPlainLabels = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);
                hotkeysAndPlainLabels[0, i] = hotkeyAndLabel[0];
                hotkeysAndPlainLabels[1, i] = hotkeyAndLabel[1];
            }
            return hotkeysAndPlainLabels;
        }


        public override string ReadLine()
        {
            return myInput.Read() + "\n";
        }

        public override System.Security.SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException(
                      "SecureStrings are not yet implemented by PoshConsole");
        }

        public override PSCredential PromptForCredential(
                string caption, string message, string userName, string targetName)
        {

            throw new NotImplementedException(
                      "Credentials are not yet implemented by PoshConsole");
        }

        public override PSCredential PromptForCredential(
                string caption, string message, string userName,
                string targetName, PSCredentialTypes allowedCredentialTypes,
                PSCredentialUIOptions options)
        {
            throw new NotImplementedException(
                      "Credentials are not yet implemented by PoshConsole");
        }



        #region OutputMethods
        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            myConsole.Write(foregroundColor, backgroundColor, value);
        }

        public override void Write(string value)
        {
            myConsole.Write(value);
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            myConsole.WriteLine(foregroundColor, backgroundColor, value);
        }

        public override void WriteLine()
        {
            myConsole.WriteLine(string.Empty);
        }

        public override void WriteLine(string value)
        {
            myConsole.WriteLine(value);
        }

        public override void WriteErrorLine(string value)
        {
            myConsole.WriteErrorLine(value);
        }

        public override void WriteDebugLine(string value)
        {
            myConsole.WriteDebugLine(value);
        }

        public override void WriteVerboseLine(string value)
        {
             myConsole.WriteVerboseLine(value);
        }

        public override void WriteWarningLine(string value)
        {
            myConsole.WriteWarningLine(value);
        }


        /// <summary>
        /// Progress is not implemented by this class. Since it's not
        /// required for the cmdlet to work, it is better to do nothing
        /// instead of throwing an exception.
        /// </summary>
        /// <param name="sourceId">See base class</param>
        /// <param name="record">See base class</param>
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            myConsole.SendProgressUpdate(sourceId, record);
        }

        #endregion

    }
}