using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Huddled.Wpf.Controls
{
   /// <summary>
   /// Here we EXPLICITLY implement the IPSConsole interface.
   /// Importantly, this implementation just calls the existing methods on the our ConsoleRichTextBox class
   /// Each call is wrapped in Dispatcher methods so that the interface is thread-safe!
   /// </summary>
   public partial class ConsoleControl //: IPSConsole
   {
       //[DllImport("credui", EntryPoint="CredUIPromptForCredentialsW", CharSet=CharSet.Unicode)]
       //private static extern CredUIReturnCodes CredUIPromptForCredentials(ref CREDUI_INFO pUiInfo, string pszTargetName, IntPtr Reserved, int dwAuthError, StringBuilder pszUserName, int ulUserNameMaxChars, StringBuilder pszPassword, int ulPasswordMaxChars, ref int pfSave, CREDUI_FLAGS dwFlags);

      private static PSInvalidCastException TryConvertTo(Type type, string input, out object output)
      {
         // default to string, that seems to be what PowerShell does
            output = input;
            try {
               output = LanguagePrimitives.ConvertTo(input, type, CultureInfo.InvariantCulture);
               return null;
            } 
            catch(PSInvalidCastException ice)
            {
               // Write(_brushes.ErrorForeground, _brushes.ErrorBackground, ice.Message );
               return ice;
            }
      }

      // Possibly an alternative panel that pops up and can be closed?
      #region IPSConsole Members


      Dictionary<string, PSObject> IPSConsole.Prompt(string caption, string message, Collection<FieldDescription> descriptions)
      {
         if (!string.IsNullOrEmpty(caption))
            ((IPSConsole)this).WriteLine(caption);
         if (!string.IsNullOrEmpty(message))
            ((IPSConsole)this).WriteLine(message);

         var results = new Dictionary<string, PSObject>();
         foreach (var fd in descriptions)
         {
            Type type = Type.GetType(fd.ParameterAssemblyFullName);
            
            string prompt = string.IsNullOrEmpty(fd.Label) ? fd.Name : fd.Label;

            if (type != null && type.IsArray)
            {
               type = type.GetElementType();
               var output = new List<PSObject>();
               int count = 0;
               do
               {
                  PSObject single = GetSingle(  caption, message, string.Format("{0}[{1}]", prompt, count++), 
                                                fd.HelpMessage, fd.DefaultValue, type);
                  if(single == null) break;

                  if(!(single.BaseObject is string) || ((string)single.BaseObject).Length > 0)
                  {
                     output.Add(single);
                  } else break;
               } while (true);

               results[fd.Name] = PSObject.AsPSObject(output.ToArray());
            } else
            {
               results[fd.Name] = GetSingle(caption, message, prompt, fd.HelpMessage, fd.DefaultValue, type);
            }

         }
         return results;
      }

      private PSObject GetSingle(string caption, string message, string prompt, string help, PSObject psDefault, Type type)
      {
         if (null != type && type.Equals(typeof(PSCredential)))
         {
            return PSObject.AsPSObject(((IPSConsole)this).PromptForCredential(caption, message, String.Empty, prompt));
         }

         while(true)
         {
            // TODO: Only show the help message if they type '?' as their entry something, in which case show help and re-prompt.
            if (!String.IsNullOrEmpty(help))
               ((IPSConsole) this).WriteLine(_brushes.ConsoleColorFromBrush(_brushes.VerboseForeground), _brushes.ConsoleColorFromBrush(_brushes.VerboseBackground), help);

            ((IPSConsole) this).Write(String.Format("{0}: ", prompt));

            if (null != type && typeof(SecureString).Equals(type))
            {
               var userData = ((IPSConsole) this).ReadLineAsSecureString() ?? new SecureString();
               return PSObject.AsPSObject(userData);
            } // Note: This doesn't look the way it does in PowerShell, but it should work :)
            else
            {
               if (psDefault != null && psDefault.ToString().Length > 0)
               {
                  if (Dispatcher.CheckAccess())
                  {
                     CurrentCommand = psDefault.ToString();
                     _commandBox.SelectAll();
                  }
                  else
                  {
                     Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action<string>) (def =>
                                                                                           {
                                                                                              CurrentCommand = def;
                                                                                              _commandBox.SelectAll();
                                                                                           }), psDefault.ToString());
                  }
               }

               var userData = ((IPSConsole) this).ReadLine();

               if (type != null && userData.Length > 0)
               {
                  object output;
                  var ice = TryConvertTo(type, userData, out output);
                  // Special exceptions that happen when casting to numbers and such ...
                  if (ice == null)
                  {
                     return PSObject.AsPSObject(output);
                  }
                  if ((ice.InnerException is FormatException) || (ice.InnerException is OverflowException))
                  {
                     ((IPSConsole)this).WriteErrorLine(
                        String.Format( @"Cannot recognize ""{0}"" as a {1} due to a format error.", userData, type.FullName )
                        );
                  }
                  else
                  {
                     return PSObject.AsPSObject(String.Empty);
                  }
               } 
               else if (userData.Length == 0)
               {
                      return PSObject.AsPSObject(String.Empty);
               } else return PSObject.AsPSObject(userData);
            }
         } 
      }

      int IPSConsole.PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
      {
         // Write the caption and message strings in Blue.
         ((IPSConsole)this).WriteLine(ConsoleColor.Blue, ConsoleColor.Black, caption + "\n" + message + "\n");

         // Convert the choice collection into something that's a little easier to work with
         // See the BuildHotkeysAndPlainLabels method for details.
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
                  Write(_brushes.VerboseForeground, _brushes.VerboseBackground, String.Format("[{0}] {1}  ", promptData[0, element], promptData[1, element]));
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
         // ReSharper disable SuggestUseVarKeywordEvident
         string[] result = new[] { String.Empty, String.Empty };
         string[] fragments = input.Split('&');
         // ReSharper restore SuggestUseVarKeywordEvident
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
      /// <param name="addHelp">Add the 'Help' prompt </param>
      /// <returns>
      /// A two dimensional array containing the accelerator characters
      /// and the cleaned-up labels</returns>
      private static string[,] BuildHotkeysAndPlainLabels(Collection<ChoiceDescription> choices, bool addHelp)
      {
         // Allocate the result array
         var count = addHelp ? choices.Count+1 : choices.Count;
         var hotkeysAndPlainLabels = new string[2, count];

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
      private readonly PopupMenu _popup;
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
      private string _lastInputString;
      private SecureString _lastPassword;

      public bool WaitingForInput;

      /// <summary>Handles the CommandEntered event of the Console buffer</summary>
      /// <param name="command">The command.</param>
      private void OnCommand(string command)
      {
         if (WaitingForInput)
         {
            _lastInputString = command.TrimEnd();
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
         Dispatcher.Invoke((Action)(() =>
                                       {
                                          lock (_commandContainer)
                                          {
                                             UpdateLayout();
                                             _next.Inlines.Remove(_commandContainer);
                                             ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - _current.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
                                             _current.Inlines.Add(_commandContainer);
                                             UpdateLayout();
                                             _commandContainer.Child.Focus();
                                          }
                                       }), DispatcherPriority.Render);
         Thread.Sleep(0);
         WaitingForInput = true;
         _gotInputLine.Reset();
         _gotInputLine.WaitOne();
         WaitingForInput = false;

         Dispatcher.Invoke((Action)(() =>
                                       {
                                          lock (_commandContainer)
                                          {
                                             _current.Inlines.Remove(_commandContainer);
                                             ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - _next.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
                                             _next.Inlines.Add(_commandContainer);
                                             _commandContainer.Child.Focus();
                                             UpdateLayout();
                                          }
                                       }), DispatcherPriority.Render);
         return _lastInputString ?? String.Empty;
      }

      SecureString IPSConsole.ReadLineAsSecureString()
      {
         Dispatcher.Invoke((Action)(() =>
         {
            lock (_commandContainer)
            {
               UpdateLayout();
               _commandContainer.Child = _passwordBox;
               _next.Inlines.Remove(_commandContainer);
               ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - _current.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
               _current.Inlines.Add(_commandContainer);
               UpdateLayout();
               _commandContainer.Child.Focus();
            }
         }), DispatcherPriority.Render);

         Thread.Sleep(0);
         WaitingForInput = true;
         _gotInputLine.Reset();
         _gotInputLine.WaitOne();
         WaitingForInput = false;

         Dispatcher.Invoke((Action)(() =>
         {
            lock (_commandContainer)
            {
               _commandContainer.Child = _commandBox;
               _current.Inlines.Remove(_commandContainer);
               ((Control)_commandContainer.Child).MaxWidth = Math.Max(_characterWidth * 10, ScrollViewer.ViewportWidth - _next.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
               _next.Inlines.Add(_commandContainer);
               _commandContainer.Child.Focus();
               UpdateLayout();
            }
         }), DispatcherPriority.Render);

         return _lastPassword;
      }
      #endregion ReadLine

 

 


      PSCredential IPSConsole.PromptForCredential(string caption, string message, string userName, string targetName, 
         PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
      {

         Collection<FieldDescription> fields;
         Dictionary<string, PSObject> login, password;

         // NOTE: I'm not sure this is the right action for the PromptForCredential targetName
         if (!String.IsNullOrEmpty(targetName))
         {
            caption = string.Format("Credential for {0}\n\n{1}", targetName, caption);
         }

         if ((options & PSCredentialUIOptions.ReadOnlyUserName) == PSCredentialUIOptions.Default )
         {
            var user = new FieldDescription("User");
            user.SetParameterType(typeof(string));
            user.Label = "Username";
            user.DefaultValue = PSObject.AsPSObject(userName);
            user.IsMandatory = true;

            do
            {
               fields = new Collection<FieldDescription>(new[] {user});
               login = ((IPSConsole) this).Prompt(caption, message, fields);
               userName = login["User"].BaseObject as string;
            } while ( userName != null && userName.Length == 0);
         }

         // I think this is all I can do with the allowedCredentialTypes
         // domain required
         if (allowedCredentialTypes > PSCredentialTypes.Generic)
         {
            // and no domain
            if (userName != null && userName.IndexOfAny(new[] { '\\', '@' }) < 0)
            {
               userName = string.Format("{0}\\{1}", targetName, userName);
            }
         }

         var pass = new FieldDescription("Password");
         pass.SetParameterType(typeof(SecureString));
         pass.Label = "Password for " + userName;
         pass.IsMandatory = true;

         fields = new Collection<FieldDescription>(new[] { pass });
         password = ((IPSConsole)this).Prompt(String.Empty, String.Empty, fields);

         // TODO: I'm not sure what to do with the PSCredentialUIOptions options, because PowerShell.exe ignores them
         return new PSCredential(userName, (SecureString)password["Password"].BaseObject);
      }

      PSCredential IPSConsole.PromptForCredential(string caption, string message, string userName, string targetName)
      {
         var user = new FieldDescription("User");
         user.SetParameterType(typeof(string));
         user.DefaultValue = PSObject.AsPSObject(userName);
         user.IsMandatory = true;


         var pass = new FieldDescription("Password");
         pass.SetParameterType(typeof(SecureString));
         pass.IsMandatory = true;

         var cred = new Collection<FieldDescription>(new[] { user, pass });

         var login = ((IPSConsole)this).Prompt(caption, message, cred);

         return new PSCredential(
            (string)login["User"].BaseObject,
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
         Write(_brushes.DebugForeground, _brushes.DebugBackground, String.Format("DEBUG: {0}\n", message), _current);
      }

      void IPSConsole.WriteDebugLine(string message, Block target)
      {
         // Write is Dispatcher checked
         Write(_brushes.DebugForeground, _brushes.DebugBackground, String.Format("DEBUG: {0}\n", message), target);
      }


      void IPSConsole.WriteErrorRecord(ErrorRecord errorRecord)
      {
         // Write is Dispatcher checked
         Write(_brushes.ErrorForeground, _brushes.ErrorBackground, errorRecord + "\n", _current);
         if (errorRecord.InvocationInfo != null)
         {
            // Write is Dispatcher checked
            Write(_brushes.ErrorForeground, _brushes.ErrorBackground, errorRecord.InvocationInfo.PositionMessage + "\n", _current);
         }
      }


      void IPSConsole.WriteErrorLine(string message)
      {
         // Write is Dispatcher checked
         Write(_brushes.ErrorForeground, _brushes.ErrorBackground, message + "\n", _current);
      }

      void IPSConsole.WriteVerboseLine(string message)
      {
         // Write is Dispatcher checked
         Write(_brushes.VerboseForeground, _brushes.VerboseBackground, String.Format("VERBOSE: {0}\n", message), _current);
      }

      void IPSConsole.WriteWarningLine(string message)
      {
         // Write is Dispatcher checked
         Write(_brushes.WarningForeground, _brushes.WarningBackground, String.Format("WARNING: {0}\n", message), _current);
      }

      void IPSConsole.WriteNativeOutput(string message)
      {
         // Write is Dispatcher checked
         Write(_brushes.NativeOutputForeground, _brushes.NativeOutputBackground, message, _current);
         Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(SetPrompt));
         // TODO: REIMPLEMENT NATIVE prompt using Begin/End and Prompt()
      }

      void IPSConsole.WriteNativeError(string message)
      {
         // Write is Dispatcher checked
         Write(_brushes.NativeErrorForeground, _brushes.NativeErrorBackground, message, _current);
         Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)(SetPrompt));
      }

      #endregion
   }
}
