// Copyright (c) 2008 Joel Bennett

// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// *****************************************************************************
// NOTE: YOU MAY *ALSO* DISTRIBUTE THIS FILE UNDER ANY OF THE FOLLOWING...
// PERMISSIVE LICENSES:
// BSD:	 http://www.opensource.org/licenses/bsd-license.php
// MIT:   http://www.opensource.org/licenses/mit-license.html
// Ms-PL: http://www.opensource.org/licenses/ms-pl.html
// RECIPROCAL LICENSES:
// Ms-RL: http://www.opensource.org/licenses/ms-rl.html
// GPL 2: http://www.gnu.org/copyleft/gpl.html
// *****************************************************************************
// LASTLY: THIS IS NOT LICENSED UNDER GPL v3 (although the above are compatible)
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Runtime.ConstrainedExecution;

namespace Huddled.Interop.Windows
{
   public static partial class NativeMethods
   {
      /// <summary>
      /// Provides the enumeration values for calls to <see cref="NativeMethods.ShowWindow"/> or <see cref="NativeMethods.ShowWindowAsync"/>
      /// </summary>
      public enum ShowWindowCommand : int
      {
         /// <summary>
         /// Hides the Window and activates another Window.
         /// </summary>
         Hide = 0,
         /// <summary>
         /// Activates and displays a Window. If the Window is minimized or 
         /// maximized, the system restores it to its original size and position.
         /// An application should specify this flag when displaying the Window 
         /// for the first time.
         /// </summary>
         Normal = 1,
         /// <summary>
         /// Activates the Window and displays it as a minimized Window.
         /// </summary>
         ShowMinimized = 2,
         /// <summary>
         /// Maximizes the specified Window.
         /// </summary>
         Maximize = 3, // is this the right value?
         /// <summary>
         /// Activates the Window and displays it as a maximized Window.
         /// </summary>       
         ShowMaximized = 3,
         /// <summary>
         /// Displays a Window in its most recent size and position. This value 
         /// is similar to <see cref="ShowWindowCommand.Normal"/>, except 
         /// the Window is not actived.
         /// </summary>
         ShowNoActivate = 4,
         /// <summary>
         /// Activates the Window and displays it in its current size and position. 
         /// </summary>
         Show = 5,
         /// <summary>
         /// Minimizes the specified Window and activates the next top-level 
         /// Window in the Z order.
         /// </summary>
         Minimize = 6,
         /// <summary>
         /// Displays the Window as a minimized Window. This value is similar to
         /// <see cref="ShowWindowCommand.ShowMinimized"/>, except the 
         /// Window is not activated.
         /// </summary>
         ShowMinNoActive = 7,
         /// <summary>
         /// Displays the Window in its current size and position. This value is 
         /// similar to <see cref="ShowWindowCommand.Show"/>, except the 
         /// Window is not activated.
         /// </summary>
         ShowNA = 8,
         /// <summary>
         /// Activates and displays the Window. If the Window is minimized or 
         /// maximized, the system restores it to its original size and position. 
         /// An application should specify this flag when restoring a minimized Window.
         /// </summary>
         Restore = 9,
         /// <summary>
         /// Sets the show state based on the SW_* value specified in the 
         /// STARTUPINFO structure passed to the CreateProcess function by the 
         /// program that started the application.
         /// </summary>
         ShowDefault = 10,
         /// <summary>
         ///  <b>Windows 2000/XP:</b> Minimizes a Window, even if the thread 
         /// that owns the Window is not responding. This flag should only be 
         /// used when minimizing windows from a different thread.
         /// </summary>
         ForceMinimize = 11
      }

      /// <summary>
      /// Provides the enumeration values for calls to <see cref="NativeMethods.GetWindow"/>
      /// </summary>
      public enum GetWindowCommand : int
      {
         /// <summary>
         /// The retrieved handle identifies the Window of the same type that is highest in the Z order. If the specified Window is a topmost Window, the handle identifies the topmost Window that is highest in the Z order. If the specified Window is a top-level Window, the handle identifies the top-level Window that is highest in the Z order. If the specified Window is a child Window, the handle identifies the sibling Window that is highest in the Z order.
         /// </summary>
         First = 0,
         /// <summary>
         /// The retrieved handle identifies the Window of the same type that is lowest in the Z order. If the specified Window is a topmost Window, the handle identifies the topmost Window that is lowest in the Z order. If the specified Window is a top-level Window, the handle identifies the top-level Window that is lowest in the Z order. If the specified Window is a child Window, the handle identifies the sibling Window that is lowest in the Z order.
         /// </summary>
         Last = 1,
         /// <summary>
         /// The retrieved handle identifies the Window below the specified Window in the Z order. If the specified Window is a topmost Window, the handle identifies the topmost Window below the specified Window. If the specified Window is a top-level Window, the handle identifies the top-level Window below the specified Window. If the specified Window is a child Window, the handle identifies the sibling Window below the specified Window. 
         /// </summary>
         Next = 2,
         /// <summary>
         /// The retrieved handle identifies the Window above the specified Window in the Z order. If the specified Window is a topmost Window, the handle identifies the topmost Window above the specified Window. If the specified Window is a top-level Window, the handle identifies the top-level Window above the specified Window. If the specified Window is a child Window, the handle identifies the sibling Window above the specified Window.
         /// </summary>
         Previous = 3,
         /// <summary>
         /// The retrieved handle identifies the specified Window's owner Window, if any.
         /// </summary>
         Owner = 4,
         /// <summary>
         /// The retrieved handle identifies the child Window at the top of the Z order, if the specified Window is a parent Window; otherwise, the retrieved handle is NULL. The function examines only child windows of the specified Window. It does not examine descendant windows.
         /// </summary>
         Child = 5,
         /// <summary>
         /// Windows 2000/XP: The retrieved handle identifies the enabled popup Window owned by the specified Window (the search uses the first such Window found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is that of the specified Window.
         /// </summary>
         Popup = 6
      }

      // It looks like the function is in shell32.dll - just not exported pre XP SP1. 
      // We could hypothetically reference it by ordinal number -- should work from Win2K SP4 on.
      // [DllImport("shell32.dll",EntryPoint="#680",CharSet=CharSet.Unicode)]
      [DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin", CharSet = CharSet.Unicode)]
      public static extern bool IsUserAnAdmin();

      //[DllImport("user32.dll")]
      //[return: MarshalAs(UnmanagedType.Bool)]
      //public static extern bool BringWindowToTop(IntPtr hWnd);

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool IsWindowVisible(IntPtr hWnd);

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool SetForegroundWindow(IntPtr hWnd);


      #region user32!GetWindow
      /// <summary> The GetWindow function retrieves a handle to a Window 
      /// that has the specified relationship (Z-Order or owner) to the specified Window.
      /// </summary>
      /// <param name="windowHandle">
      /// Handle to a Window. The Window handle retrieved is relative to this Window, 
      /// based on the value of the command parameter.
      /// </param>
      /// <param name="command">
      /// Specifies the relationship between the specified Window and the Window 
      /// whose handle is to be retrieved.
      /// </param>
      /// <returns>
      /// If the function succeeds, the return value is a Window handle. 
      /// If no Window exists with the specified relationship to the specified Window, 
      /// the return value is IntPtr.Zero. 
      /// To get extended error information, call GetLastError.
      /// </returns>
      [DllImport("user32", SetLastError = true)]
      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
      public static extern IntPtr GetWindow(IntPtr windowHandle, GetWindowCommand command);
      #endregion
      #region user32!ShowWindow
      /// <summary>
      /// The ShowWindow function sets the specified Window's show state.
      /// </summary>
      /// <param name="windowHandle">
      /// Handle to the Window.
      /// </param>
      /// <param name="command">
      /// Specifies how the Window is to be shown. This parameter is ignored 
      /// the first time an application calls <see cref="ShowWindow"/>, if the program that
      /// launched the application provides a <see cref="StartupInfo"/> structure. 
      /// Otherwise, the first time ShowWindow is called, the value should be the value 
      /// obtained by the WinMain function in its nCmdShow parameter.</param>
      /// <returns></returns>
      [return: MarshalAs(UnmanagedType.Bool)]
      [DllImport("user32", SetLastError = true)]
      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
      public static extern bool ShowWindow(IntPtr windowHandle, ShowWindowCommand command);
      #endregion

      public delegate IntPtr MessageHandler(IntPtr wParam, IntPtr lParam, ref bool handled);

      /// <summary>Window message values, WM_*
      /// </summary>
      public enum WindowMessage
      {
         NULL = 0x0000,
         Create = 0x0001,
         Destroy = 0x0002,
         Move = 0x0003,
         Moving = 0x0216,
         Size = 0x0005,
         Activate = 0x0006,
         SetFocus = 0x0007,
         KillFocus = 0x0008,
         Enable = 0x000a,
         SetRedraw = 0x000b,
         SetText = 0x000c,
         GetText = 0x000d,
         GetTextLength = 0x000e,
         Paint = 0x000f,
         Close = 0x0010,
         QueryEndSession = 0x0011,
         Quit = 0x0012,
         QueryOpen = 0x0013,
         EraseBackground = 0x0014,
         SystemColorChange = 0x0015,

         WindowPositionChanging = 0x0046,
         WindowPositionChanged = 0x0047,

         SetIcon = 0x0080,
         NcCreate = 0x0081,
         NcDestroy = 0x0082,
         NcCalcSize = 0x0083,
         NcHitTest = 0x0084,
         NcPaint = 0x0085,
         NcActivate = 0x0086,
         GetDialogCode = 0x0087,
         SyncPaint = 0x0088,
         NcMouseMove = 0x00a0,
         NcLButtonDown = 0x00a1,
         NcLButtonUp = 0x00a2,
         NcLButtonDoubleClick = 0x00a3,
         NcRButtonDown = 0x00a4,
         NcRButtonUp = 0x00a5,
         NcRButtonDoubleClick = 0x00a6,
         NcMButtonDown = 0x00a7,
         NcMButtonUp = 0x00a8,
         NcMButtonDoubleClick = 0x00a9,

         SysKeyDown = 0x0104,
         SysKeyUp = 0x0105,
         SysChar = 0x0106,
         SysDeadChar = 0x0107,
         SysCommand = 0x0112,

         Hotkey = 0x312,

         DwmCompositionChanged = 0x031e,
         User = 0x0400,
         App = 0x8000,
      }

      /// <summary>SetWindowPos options
      /// </summary>
      [Flags]
      public enum WindowPositionFlags
      {
         AsyncWindowPosition = 0x4000,
         DeferErase = 0x2000,
         DrawFrame = 0x0020,
         FrameChanged = 0x0020,
         HideWindow = 0x0080,
         NoActivate = 0x0010,
         NoCopyBits = 0x0100,
         NoMove = 0x0002,
         NoOwnerZorder = 0x0200,
         NoRedraw = 0x0008,
         NoReposition = 0x0200,
         NoSendChanging = 0x0400,
         NoSize = 0x0001,
         NoZorder = 0x0004,
         ShowWindow = 0x0040,
      }

      /// <summary>lParam for WindowPositionChanging
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct WindowPosition
      {
         public IntPtr Handle;
         public IntPtr HandleInsertAfter;
         public int Left;
         public int Top;
         public int Width;
         public int Height;
         public WindowPositionFlags Flags;

         public int Right { get { return Left + Width; } }
         public int Bottom { get { return Top + Height; } }
      }   
   }
}
