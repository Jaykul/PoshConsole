using System.Windows.Interop;
using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;

namespace PoshConsole.Interop
{
    public partial class NativeMethods
    {
        #region user32!RegisterHotKey
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int uVirtKey);
        #endregion
        #region user32!UnregisterHotKey
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
    }
}