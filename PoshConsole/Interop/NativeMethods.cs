using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Runtime.ConstrainedExecution;

namespace Huddled.PoshConsole
{
    public static partial class NativeMethods
	{

        /// <summary>
        /// Provides the enumeration values for calls to <see cref="NativeMethods.ShowWindow"/> or <see cref="NativeMethods.ShowWindowAsync"/>
        /// </summary>
        public enum ShowWindowCommand : uint
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window 
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>       
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value 
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except 
            /// the window is not actived.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position. 
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level 
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is 
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position. 
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the 
            /// STARTUPINFO structure passed to the CreateProcess function by the 
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread 
            /// that owns the window is not responding. This flag should only be 
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

        /// <summary>
        /// Provides the enumeration values for calls to <see cref="NativeMethods.GetWindow"/>
        /// </summary>
        public enum GetWindowCommand : uint
        {
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is highest in the Z order. If the specified window is a topmost window, the handle identifies the topmost window that is highest in the Z order. If the specified window is a top-level window, the handle identifies the top-level window that is highest in the Z order. If the specified window is a child window, the handle identifies the sibling window that is highest in the Z order.
            /// </summary>
            First = 0,
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is lowest in the Z order. If the specified window is a topmost window, the handle identifies the topmost window that is lowest in the Z order. If the specified window is a top-level window, the handle identifies the top-level window that is lowest in the Z order. If the specified window is a child window, the handle identifies the sibling window that is lowest in the Z order.
            /// </summary>
            Last = 1,
            /// <summary>
            /// The retrieved handle identifies the window below the specified window in the Z order. If the specified window is a topmost window, the handle identifies the topmost window below the specified window. If the specified window is a top-level window, the handle identifies the top-level window below the specified window. If the specified window is a child window, the handle identifies the sibling window below the specified window. 
            /// </summary>
            Next = 2,
            /// <summary>
            /// The retrieved handle identifies the window above the specified window in the Z order. If the specified window is a topmost window, the handle identifies the topmost window above the specified window. If the specified window is a top-level window, the handle identifies the top-level window above the specified window. If the specified window is a child window, the handle identifies the sibling window above the specified window.
            /// </summary>
            Previous = 3,
            /// <summary>
            /// The retrieved handle identifies the specified window's owner window, if any.
            /// </summary>
            Owner = 4,
            /// <summary>
            /// The retrieved handle identifies the child window at the top of the Z order, if the specified window is a parent window; otherwise, the retrieved handle is NULL. The function examines only child windows of the specified window. It does not examine descendant windows.
            /// </summary>
            Child = 5,
            /// <summary>
            /// Windows 2000/XP: The retrieved handle identifies the enabled popup window owned by the specified window (the search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is that of the specified window.
            /// </summary>
            Popup = 6
        }



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

        #region user32!GetWindow
        /// <summary> The GetWindow function retrieves a handle to a window 
        /// that has the specified relationship (Z-Order or owner) to the specified window.
		/// </summary>
		/// <param name="windowHandle">
        /// Handle to a window. The window handle retrieved is relative to this window, 
        /// based on the value of the command parameter.
        /// </param>
		/// <param name="command">
        /// Specifies the relationship between the specified window and the window 
        /// whose handle is to be retrieved.
        /// </param>
		/// <returns>
        /// If the function succeeds, the return value is a window handle. 
        /// If no window exists with the specified relationship to the specified window, 
        /// the return value is IntPtr.Zero. 
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern IntPtr GetWindow(IntPtr windowHandle, GetWindowCommand command);
        #endregion

        #region user32!ShowWindow
        /// <summary>
        /// The ShowWindow function sets the specified window's show state.
        /// </summary>
        /// <param name="hWnd">
        /// Handle to the window.
        /// </param>
        /// <param name="nCmdShow">
        /// Specifies how the window is to be shown. This parameter is ignored 
        /// the first time an application calls <see cref="ShowWindow"/>, if the program that
        /// launched the application provides a <see cref="STARTUPINFO"/> structure. 
        /// Otherwise, the first time ShowWindow is called, the value should be the value 
        /// obtained by the WinMain function in its nCmdShow parameter.</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static extern bool ShowWindow(IntPtr windowHandle, ShowWindowCommand command);
        #endregion

        /// <summary>
        /// Gets the window handle for the specified <see cref="System.Windows.Window"/>
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns></returns>
        public static IntPtr GetWindowHandle(System.Windows.Window window)
        {
            return new WindowInteropHelper(window).Handle;
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
