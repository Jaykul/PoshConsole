using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Huddled.WPF.Controls.Interfaces;

namespace System.Management.Automation.Host
{
	/// <summary>
	/// <para>Provides an interface which extends the existing PowerShell interfaces with a 
	/// <see cref="Huddled.Interop.Hotkeys.HotkeyManager"/> able to execute scriptblocks
	/// </para>
	/// </summary>
	public interface IPSBackgroundHost
	{
      bool AddHotkey(System.Windows.Input.KeyGesture key, ScriptBlock script);
      bool RemoveHotkey(System.Windows.Input.KeyGesture key);
      IEnumerable<KeyValuePair<KeyGesture, ScriptBlock>> Hotkeys();
   }
}
