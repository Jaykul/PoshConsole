using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Win32
{
	public class Application
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

        //[DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);
         

        // It looks like the function is in shell32.dll - just not exported pre XP SP1. 
        // We could hypothetically reference it by ordinal number -- should work from Win2K SP4 on.
        // [DllImport("shell32.dll",EntryPoint="#680",CharSet=CharSet.Unicode)]
        [DllImport("shell32.dll", EntryPoint="IsUserAnAdmin", CharSet=CharSet.Unicode)]
        public static extern bool IsUserAnAdmin();

		/// <summary>
		/// The GetWindow function retrieves a handle to a window that has the specified relationship (Z-Order or owner) to the specified window.
		/// </summary>
		/// <param name="windowHandle">Handle to a window. The window handle retrieved is relative to this window, based on the value of the command parameter.</param>
		/// <param name="command">Specifies the relationship between the specified window and the window whose handle is to be retrieved.</param>
		/// <returns>If the function succeeds, the return value is a window handle. If no window exists with the specified relationship to the specified window, the return value is IntPtr.Zero. To get extended error information, call GetLastError.</returns>
		[DllImport("user32.dll")]
		public static extern IntPtr GetWindow(IntPtr windowHandle, GetWindowCommand command);
        
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);

		public enum GetWindowCommand : uint
		{
			/// <summary>
			/// The retrieved handle identifies the window of the same type that is highest in the Z order. If the specified window is a topmost window, the handle identifies the topmost window that is highest in the Z order. If the specified window is a top-level window, the handle identifies the top-level window that is highest in the Z order. If the specified window is a child window, the handle identifies the sibling window that is highest in the Z order.
			/// </summary>
			First    = 0,
			/// <summary>
			/// The retrieved handle identifies the window of the same type that is lowest in the Z order. If the specified window is a topmost window, the handle identifies the topmost window that is lowest in the Z order. If the specified window is a top-level window, the handle identifies the top-level window that is lowest in the Z order. If the specified window is a child window, the handle identifies the sibling window that is lowest in the Z order.
			/// </summary>
			Last     = 1,
			/// <summary>
			/// The retrieved handle identifies the window below the specified window in the Z order. If the specified window is a topmost window, the handle identifies the topmost window below the specified window. If the specified window is a top-level window, the handle identifies the top-level window below the specified window. If the specified window is a child window, the handle identifies the sibling window below the specified window. 
			/// </summary>
			Next     = 2,
			/// <summary>
			/// The retrieved handle identifies the window above the specified window in the Z order. If the specified window is a topmost window, the handle identifies the topmost window above the specified window. If the specified window is a top-level window, the handle identifies the top-level window above the specified window. If the specified window is a child window, the handle identifies the sibling window above the specified window.
			/// </summary>
			Previous = 3,
			/// <summary>
			/// The retrieved handle identifies the specified window's owner window, if any.
			/// </summary>
			Owner    = 4,
			/// <summary>
			/// The retrieved handle identifies the child window at the top of the Z order, if the specified window is a parent window; otherwise, the retrieved handle is NULL. The function examines only child windows of the specified window. It does not examine descendant windows.
			/// </summary>
			Child    = 5,
			/// <summary>
			/// Windows 2000/XP: The retrieved handle identifies the enabled popup window owned by the specified window (the search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is that of the specified window.
			/// </summary>
			Popup    = 6
		}

        public enum ShowWindowCommand : int
        {
            Hide = 0,
            Normal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            ShowNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActive = 7,
            ShowNA = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimize = 11,
            Max = 11
        }

        public static IntPtr GetWindowHandle(System.Windows.Window window)
        {
            return new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
        }

        //public static IntPtr GetOwnerHandle(System.Windows.Window window)
        //{
        //    return new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Owner;
        //}

		/// <summary>
		/// Gets the next window.
		/// </summary>
		/// <returns></returns>
		public static IntPtr GetNextWindow( IntPtr windowHandle )
		{
            //IntPtr handle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
            IntPtr next = GetWindow(windowHandle, GetWindowCommand.Next);

            // and then make sure we have a visible window
            while (!IsWindowVisible(next))
            {
                next = GetWindow(next, GetWindowCommand.Next);
            }
            return next;
		}

		/// <summary>
		/// Activates the next window.
		/// </summary>
		/// <returns></returns>
        public static bool ActivateNextWindow(IntPtr windowHandle)
		{

            IntPtr next = GetNextWindow(windowHandle);
            ShowWindow(next,ShowWindowCommand.Show);
            return SetForegroundWindow(next);
            
		}
	}
}
