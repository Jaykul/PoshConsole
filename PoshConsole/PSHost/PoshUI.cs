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
using Huddled.WPF.Controls.Interfaces;

namespace PoshConsole.Host
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
    internal class PoshUI : PSHostUserInterface
    {
        
		#region [rgn] Fields (3)

		private IPSConsole myConsole = null;
		private IPSUI myPsHelper = null;
		private PoshRawUI myRawUi = null;

		#endregion [rgn]

		#region [rgn] Constructors (1)

		public PoshUI(PoshRawUI rawUI, IPSUI uiHelper)
        {
            myRawUi = rawUI;
            myConsole = uiHelper.Console;
            myPsHelper = uiHelper;
        }
		
		#endregion [rgn]

		#region [rgn] Properties (1)

		public override PSHostRawUserInterface RawUI
        {
            get
            {
                return myRawUi;
            }
        }
		
		#endregion [rgn]

		#region [rgn] Methods (6)

		// [rgn] Public Methods (6)

		public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
           return myConsole.Prompt(caption, message, descriptions);
        }
		
		public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
           return myConsole.PromptForChoice(caption, message, choices, defaultChoice);
        }
		
		public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
		   return myConsole.PromptForCredential(caption, message, userName, targetName, allowedCredentialTypes, options);
      }
		
		public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
           return myConsole.PromptForCredential(caption, message, userName, targetName);
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
        public override string ReadLine()
        {
            return myConsole.ReadLine();
        }
		
		public override System.Security.SecureString ReadLineAsSecureString()
        {
		   return myConsole.ReadLineAsSecureString();
        }
		
		#endregion [rgn]

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
            myPsHelper.WriteProgress(sourceId, record);
        }
        #endregion
    }
}