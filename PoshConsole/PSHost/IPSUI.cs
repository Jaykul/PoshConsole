using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Security;
using Huddled.WPF.Controls.Interfaces;

namespace PoshConsole.PSHost
{
   /// <summary>
   /// <para>This interface wraps up the methods of PSHost which are not particularly console-oriented.
   /// They may be implemented on the console, or they may be implemented elswhere.</para>
   /// <para>In PoshConsole, they are implemented as part of the main window class.</para>
   /// </summary>
   public interface IPSUI
   {
      void WriteProgress(long sourceId, ProgressRecord record);
      PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options);
      PSCredential PromptForCredential(string caption, string message, string userName, string targetName);
      SecureString ReadLineAsSecureString();

      void SetShouldExit(int exitCode);
      IPoshConsoleControl Console { get; }
   }

   /// <summary>
   /// <para>Provides an interface which extends the existing PowerShell interfaces with a 
   /// <see cref="Huddled.Interop.Hotkeys.HotkeyManager"/> able to execute scriptblocks
   /// </para>
   /// </summary>
   public interface IPSBackgroundHost
   {
      bool RegisterHotkey(System.Windows.Input.KeyGesture key, ScriptBlock script);
   }

   /// <summary>
   /// Wraps up <see cref="IPSUI" /> and <see cref="IPSUI"/> in one place.
   /// </summary>
   public interface IPSPoshBackgroundUI : IPSUI, IPSBackgroundHost
   {

   }
}
