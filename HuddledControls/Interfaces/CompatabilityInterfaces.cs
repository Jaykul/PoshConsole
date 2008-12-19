using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		bool RegisterHotkey(System.Windows.Input.KeyGesture key, ScriptBlock script);
	}

	/// <summary>
	/// <para>Provides an interface which extends the existing PowerShell interfaces with a
	/// <see cref="IPSXamlConsole" />, with access to the WPF Window and Dispatcher</para>
	/// </summary>
	public interface IPSWpfHost
	{
		IPSWpfConsole GetWpfConsole();
	}
}
