using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Windows.Documents;

namespace System.Management.Automation.Host
{
   /// <summary>
   /// An interface for <see cref="System.Management.Automation.Host.PSHostUserInterface"/> to allow 
   /// implementation along with <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> in 
   /// the same class ... since there's no multiple inheritance in C#.
   /// </summary>
   /// <remarks>The need for this interface appears to be an oversight in the PowerShell hosting API.
   /// Since most implementations of <see cref="System.Management.Automation.Host.PSHostUserInterface"/>
   /// and <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> will be within the same 
   /// control, it seems problematic at best to have them as separate base classes.
   /// </remarks>
   /// <seealso cref="IPSRawConsole"/>
   /// <seealso cref="IPSXamlConsole"/>
   /// <seealso cref="IPSUI"/>
   /// <seealso cref="IPoshConsoleControl"/>
   public interface IPSConsole
   {
      // If I put this in the interface, it will break the interface: 
      // the interface would need to return an IPSRawConsole
      // But obviously that wouldn't be helpful here, so we'll just skip it.
      IPSRawConsole RawUI { get; }
      
      // TODO: There are additional methods, but I've un-objectively decided that these six don't belong in the console

      Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions);
      int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice);
      // void WriteProgress(long sourceId, ProgressRecord record);
      PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options);
      PSCredential PromptForCredential(string caption, string message, string userName, string targetName);
      SecureString ReadLineAsSecureString();
      string ReadLine();
      void Write(string value);
      void Write(string value, Block target);

      void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
      void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value, Block target);

      void WriteLine(string value);
      void WriteLine(string value, Block target);

      void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
      void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value, Block target);

      void WriteDebugLine(string message);
      void WriteDebugLine(string message, Block target);
      void WriteErrorRecord(ErrorRecord errorRecord);
      void WriteErrorLine(string value);
      void WriteVerboseLine(string message);
      void WriteWarningLine(string message);
      // I added these so I could offer to color 'native' interactions differently
      void WriteNativeOutput(string message);
      void WriteNativeError(string message);
   }
}