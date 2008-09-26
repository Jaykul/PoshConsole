using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
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
using Huddled.WPF.Controls.Properties;
using Colors=Huddled.WPF.Controls.Properties.Colors;

namespace Huddled.WPF.Controls
{
   /// <summary>
   /// Here we EXPLICITLY implement the IPSConsole interface.
   /// Importantly, this implementation just calls the existing methods on the our ConsoleRichTextBox class
   /// Each call is wrapped in Dispatcher methods so that the interface is thread-safe!
   /// </summary>
   public partial class ConsoleControl : IPSConsole  //, IPSConsole, IConsoleControlBuffered
   {

       //[DllImport("credui", EntryPoint="CredUIPromptForCredentialsW", CharSet=CharSet.Unicode)]
       //private static extern CredUIReturnCodes CredUIPromptForCredentials(ref CREDUI_INFO pUiInfo, string pszTargetName, IntPtr Reserved, int dwAuthError, StringBuilder pszUserName, int ulUserNameMaxChars, StringBuilder pszPassword, int ulPasswordMaxChars, ref int pfSave, CREDUI_FLAGS dwFlags);

      // Possibly an alternative panel that pops up and can be closed?
      #region IPSConsole Members


      Dictionary<string, PSObject> IPSConsole.Prompt(string caption, string message, Collection<FieldDescription> descriptions)
      {
         ((IPSConsole)this).WriteLine(ConsoleColor.Blue, ConsoleColor.Black, caption + "\n" + message + " ");

         var results = new Dictionary<string, PSObject>();
         foreach (var fd in descriptions)
         {
            var label = GetHotkeyAndLabel(fd.Label);

            // TODO: Only show the help message if they ... something.
            if (!String.IsNullOrEmpty(fd.HelpMessage)) ((IPSConsole)this).WriteLine(fd.HelpMessage);
            if (!String.IsNullOrEmpty(fd.Name)) ((IPSConsole)this).Write(String.Format("\n{0}: ", fd.Name));

            //((IPSConsole)this).WriteLine(ConsoleColor.Blue, ConsoleColor.Black, );

            if (!fd.ParameterTypeFullName.Equals("System.Security.SecureString"))
            {
               if (fd.DefaultValue != null)
               {
                  if (Dispatcher.CheckAccess())
                  {
                     _commandBox.Text = fd.DefaultValue.ToString();
                  }
                  else
                  {
                     Dispatcher.BeginInvoke(DispatcherPriority.Input,
                                            (Action<string>)((def) =>
                                                                 {
                                                                    _commandBox.Text = def;
                                                                    _commandBox.SelectAll();
                                                                 }), fd.DefaultValue.ToString());
                  }
               }
               var userData = ((IPSConsole)this).ReadLine();
               if (userData == null) userData = ""; // return null;
               results[fd.Name] = PSObject.AsPSObject(userData);
            }
            else
            {

               var userData = ((IPSConsole)this).ReadLineAsSecureString();
               if (userData == null) userData = new SecureString();
               results[fd.Name] = PSObject.AsPSObject(userData);
            }
         }
         return results;
      }

      int IPSConsole.PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
      {
         // Write the caption and message strings in Blue.
         ((IPSConsole)this).WriteLine(ConsoleColor.Blue, ConsoleColor.Black, caption + "\n" + message + "\n");

         // Convert the choice collection into something that's a little easier to work with
         // See the BuildHotkeysAndPlainLabels method for details.
         var results = new Dictionary<string, PSObject>();
         var promptData = BuildHotkeysAndPlainLabels(choices, true);


         // Loop reading prompts until a match is made, the default is
         // chosen or the loop is interrupted with ctrl-C.
         while (true)
         {

            // Format the overall choice prompt string to display...
            for (int element = 0; element < promptData.GetLength(1); element++)
            {
               if (element == defaultChoice)
               {
                  Write(ConsoleBrushes.VerboseForeground, ConsoleBrushes.VerboseBackground, String.Format("[{0}] {1}  ", promptData[0, element], promptData[1, element]));
               }
               else
               {
                  Write(null, null, String.Format("[{0}] {1}  ", promptData[0, element], promptData[1, element]));
               }
            }
            Write(null, null, String.Format("(default is \"{0}\"):", promptData[0, defaultChoice]));

            string data = ((IPSConsole)this).ReadLine().Trim().ToUpper();

            // If the choice string was empty, use the default selection.
            if (data.Length == 0)
               return defaultChoice;

            // See if the selection matched and return the
            // corresponding index if it did...
            for (int i = 0; i < choices.Count; i++)
            {
               if (promptData[0, i][0] == data[0])
                  return i;
            }

            // If they picked the very last thing in the list, they want help
            if (promptData.GetLength(1) > choices.Count && promptData[0, choices.Count] == data)
            {
               // Show help
               foreach (var choice in choices)
               {
                  ((IPSConsole)this).WriteLine(string.Format("{0} - {1}", choice.Label.Replace("&", ""), choice.HelpMessage));
               }
            }
            else
            {
               ((IPSConsole) this).WriteErrorLine("Invalid choice: " + data);
            }
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
               result[0] = fragments[1][0].ToString().ToUpper();
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
      private static string[,] BuildHotkeysAndPlainLabels(Collection<ChoiceDescription> choices, bool addHelp)
      {
         // Allocate the result array
         int count = addHelp ? choices.Count+1 : choices.Count;
         string[,] hotkeysAndPlainLabels = new string[2, count];

         for (int i = 0; i < choices.Count; ++i)
         {
            string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);

            if (addHelp && hotkeyAndLabel[0] == "?") {
               addHelp = false;
            }
            hotkeysAndPlainLabels[0, i] = hotkeyAndLabel[0];
            hotkeysAndPlainLabels[1, i] = hotkeyAndLabel[1];
         }

         if(addHelp)
         {
            hotkeysAndPlainLabels[0, count-1] = "?";
            hotkeysAndPlainLabels[1, count-1] = "Help";  // TODO: Internationalization?
         }

         return hotkeysAndPlainLabels;
      }





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

      private readonly AutoResetEvent _gotInputKey = new AutoResetEvent(false);
      private readonly AutoResetEvent _gotInputLine = new AutoResetEvent(false);
      private string _lastInputString = null;
      private SecureString _lastPassword = null;

      public bool _waitingForInput = false;

      /// <summary>Handles the CommandEntered event of the Console buffer</summary>
      /// <param name="command">The command.</param>
      private void OnCommand(string command)
      {
         if (_waitingForInput)
         {
            _lastInputString = command;
            _gotInputLine.Set();
         }
         else if (Command != null)
         {
            Command(this, new CommandEventArgs { Command = command, OutputBlock = _current });
            _cmdHistory.Add(command);
            Trace.WriteLine("OnCommand, clearing KeyInfo queue.");
         }
      }


      /// <summary>
      /// Provides a way for scripts to request user input ...
      /// </summary>
      /// <returns></returns>
      string IPSConsole.ReadLine()
      {
         _waitingForInput = true;
         _gotInputLine.Reset();
         _gotInputLine.WaitOne();
         _waitingForInput = false;

         return _lastInputString;
      }

      SecureString IPSConsole.ReadLineAsSecureString()
      {
         Dispatcher.Invoke((Action)(() =>
                                       {
                                          _commandContainer.Child = _passwordBox;
                                          Focus();
                                          _passwordBox.Focus();
                                       }));
         _waitingForInput = true;
         _gotInputLine.Reset();
         _gotInputLine.WaitOne();
         _waitingForInput = false;
         Dispatcher.Invoke((Action)(() => _commandContainer.Child = _commandBox));

         return _lastPassword;
      }
      #endregion ReadLine

 

 


      PSCredential IPSConsole.PromptForCredential(string caption, string message, string userName, string targetName, 
         PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
      {
         var user = new FieldDescription("User Name");
         user.SetParameterType(typeof(string));
         user.DefaultValue = PSObject.AsPSObject(userName);
         user.IsMandatory = true;


         var pass = new FieldDescription("Password");
         pass.SetParameterType(typeof(SecureString));
         pass.IsMandatory = true;
         Collection<FieldDescription> fields;
         Dictionary<string, PSObject> login;

         // NOTE: I'm not sure this is the right action for the PromptForCredential targetName
         if (!String.IsNullOrEmpty(targetName))
         {
            caption = string.Format("Credential for {0}\n\n{1}", targetName, caption);
         }

         if ((options & PSCredentialUIOptions.ReadOnlyUserName) != PSCredentialUIOptions.Default )
         {
            fields = new Collection<FieldDescription>(new[] { pass });
            login = ((IPSConsole)this).Prompt(caption, message, fields);
            login["User Name"] = PSObject.AsPSObject(userName);
         }
         else
         {
            fields = new Collection<FieldDescription>(new[] {user, pass});
            login = ((IPSConsole)this).Prompt( caption, message, fields);
         }

         // TODO: I can't figure out what to do with the PromptForCredential allowedCredentialTypes
         // TODO: I can't figure out what to do with the PromptForCredential options


         return new PSCredential(
            (string)login["User Name"].BaseObject,
            (SecureString)login["Password"].BaseObject);
      }

      PSCredential IPSConsole.PromptForCredential(string caption, string message, string userName, string targetName)
      {
         var user = new FieldDescription("User Name");
         user.SetParameterType(typeof(string));
         user.DefaultValue = PSObject.AsPSObject(userName);
         user.IsMandatory = true;


         var pass = new FieldDescription("Password");
         pass.SetParameterType(typeof(SecureString));
         pass.IsMandatory = true;

         var cred = new Collection<FieldDescription>(new[] { user, pass });

         var login = ((IPSConsole)this).Prompt(caption, message, cred);

         return new PSCredential(
            (string)login["User Name"].BaseObject,
            (SecureString)login["Password"].BaseObject);
      }


      void IPSConsole.Write(string message)
      {
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
         Write(ConsoleBrushes.DebugForeground, ConsoleBrushes.DebugBackground, String.Format("DEBUG: {0}\n", message), _current);
      }

      void IPSConsole.WriteDebugLine(string message, Block target)
      {
         // Write is Dispatcher checked
         Write(ConsoleBrushes.DebugForeground, ConsoleBrushes.DebugBackground, String.Format("DEBUG: {0}\n", message), target);
      }


      void IPSConsole.WriteErrorRecord(ErrorRecord errorRecord)
      {
         // Write is Dispatcher checked
         Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, errorRecord + "\n", _current);
         if (errorRecord.InvocationInfo != null)
         {
            // Write is Dispatcher checked
            Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, errorRecord.InvocationInfo.PositionMessage + "\n", _current);
         }
      }


      void IPSConsole.WriteErrorLine(string message)
      {
         // Write is Dispatcher checked
         Write(ConsoleBrushes.ErrorForeground, ConsoleBrushes.ErrorBackground, message + "\n", _current);
      }

      void IPSConsole.WriteVerboseLine(string message)
      {
         // Write is Dispatcher checked
         Write(ConsoleBrushes.VerboseForeground, ConsoleBrushes.VerboseBackground, String.Format("VERBOSE: {0}\n", message), _current);
      }

      void IPSConsole.WriteWarningLine(string message)
      {
         // Write is Dispatcher checked
         Write(ConsoleBrushes.WarningForeground, ConsoleBrushes.WarningBackground, String.Format("WARNING: {0}\n", message), _current);
      }

      void IPSConsole.WriteNativeLine(string message)
      {
         // Write is Dispatcher checked
         Write(ConsoleBrushes.NativeOutputForeground, ConsoleBrushes.NativeOutputBackground, message + "\n", _current);
         Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() => SetPrompt()));
         // TODO: REIMPLEMENT NATIVE prompt using Begin/End and Prompt()
      }

      void IPSConsole.WriteNativeErrorLine(string message)
      {
         // Write is Dispatcher checked
         Write(ConsoleBrushes.NativeErrorForeground, ConsoleBrushes.NativeErrorBackground, message + "\n", _current);
         Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() => SetPrompt()));
      }

      #endregion
   }
}