using System.Collections.Generic;
using System.Windows.Input;

namespace System.Management.Automation.Host
{
	/// <summary>
	/// <para>Provides an interface which extends the existing PowerShell interfaces with a 
	/// <see cref="PoshCode.Interop.Hotkeys.HotkeyManager"/> able to execute scriptblocks
	/// </para>
	/// </summary>
	public interface IPSBackgroundHost
	{
      bool AddHotkey(KeyGesture key, ScriptBlock script);
      bool RemoveHotkey(KeyGesture key);
      IEnumerable<KeyValuePair<KeyGesture, ScriptBlock>> Hotkeys();
   }
}
