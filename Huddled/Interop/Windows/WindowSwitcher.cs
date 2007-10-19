using System;
using System.Windows;
using System.Windows.Interop;

namespace Huddled.Interop.Windows
{
    public static class WindowSwitcher
    {
        
		#region [rgn] Methods (4)

		// [rgn] Public Methods (2)

		public static void ActivateNextWindow()
        {
            ActivateNextWindow(Application.Current.MainWindow);
        }
		
		public static void ActivateNextWindow(Window current)
        {
            IntPtr next = GetNextWindow(current);

            NativeMethods.ShowWindow(next, NativeMethods.ShowWindowCommand.Show);
            NativeMethods.SetForegroundWindow(next);
        }
		
		// [rgn] Private Methods (2)

		private static IntPtr GetNextWindow(Window relativeTo)
        {
            IntPtr current = GetWindowHandle(relativeTo);
            IntPtr next = NativeMethods.GetWindow(current, NativeMethods.GetWindowCommand.Next);

            while (!NativeMethods.IsWindowVisible(next))
            {
                next = NativeMethods.GetWindow(next, NativeMethods.GetWindowCommand.Next);
            }

            return next;
        }
		
		private static IntPtr GetWindowHandle(Window window)
        {
            return new WindowInteropHelper(window).Handle;
        }
		
		#endregion [rgn]

    }

}
