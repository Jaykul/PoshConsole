using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Runtime.ConstrainedExecution;

namespace PoshConsole.Interop
{
    public static partial class NativeMethods
	{
		// It looks like the function is in shell32.dll - just not exported pre XP SP1. 
        // We could hypothetically reference it by ordinal number -- should work from Win2K SP4 on.
        // [DllImport("shell32.dll",EntryPoint="#680",CharSet=CharSet.Unicode)]
        [DllImport("shell32.dll", EntryPoint="IsUserAnAdmin", CharSet=CharSet.Unicode)]
        public static extern bool IsUserAnAdmin();
		
	}
}
