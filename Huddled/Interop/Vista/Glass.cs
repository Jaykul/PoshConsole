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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Huddled.Interop.Vista
{
   /// <summary>
   /// An addendum to the Win32.NativeMethods class with all the methods which are needed by the Vista.Glass class
   /// Placed here for portability between projects, and to assign blame ;-)
   /// </summary>
   public static class Glass
   {
      public static bool TryExtendFrameIntoClientArea(this Window window)
      {
         return TryExtendFrameIntoClientArea(window, window.BorderThickness);
      }

      public static bool TryExtendFrameIntoClientArea(this Window window, Thickness margin)
      {
         try
         {
            if (IsCompositionEnabled)
            {
               IntPtr hwnd = new WindowInteropHelper(window).Handle;
               if (!IntPtr.Zero.Equals(hwnd))
               {
                  ExtendFrameIntoClientArea(hwnd, margin);
                  // Set the background to transparent to get the full Glass effect
                  window.Background = Brushes.Transparent;
                  return true;
               }
            }
         }
         finally { }
         return false;
      }

      /// <summary>Extends the frame into client area.
      /// </summary>
      /// <param name="hwnd">The Window.</param>
      /// <param name="margin">The margin.</param>
      /// <returns><c>True</c> if the function succeeded, <c>False</c> otherwise.</returns>
      public static void ExtendFrameIntoClientArea(IntPtr hwnd, Thickness margin)
      {
         if (!IsCompositionEnabled)
            throw new InvalidOperationException("Composition is not enabled. Glass cannot be extended.");

         if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("The Window must be shown before extending glass. As a suggestion, in WPF you should call this during the SourceInitialized event.");

         // Set the background to transparent to get the full Glass effect
         HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;

         MARGINS margins = new MARGINS(margin);
         DwmExtendFrameIntoClientArea(hwnd, ref margins);
      }

      /// <summary>Gets a value indicating whether Window Composition is enabled.
      /// </summary>
      /// <value>
      /// 	<c>true</c> if composition is enabled; otherwise, <c>false</c>.
      /// </value>
      public static bool IsCompositionEnabled
      {
         get
         {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                   && Environment.OSVersion.Version.Major >= 6
                   && DwmIsCompositionEnabled();
         }
      }

      public static void EnableBlurBehind(this Window window, bool enable, IntPtr region, bool transition)
      {
         var blurBehind = new BLURBEHIND
                             {
                                Flags = (BlurBehindOptions.Enable | BlurBehindOptions.TransitionOnMaximized),
                                Enable = enable,
                                TransitionOnMaximized = transition
                             };

         if (enable && IntPtr.Zero != region)
         {
            blurBehind.Flags |= BlurBehindOptions.BlurRegion;
            blurBehind.RegionBlur = region;
         }

         DwmEnableBlurBehindWindow(new WindowInteropHelper(window).Handle, ref blurBehind);
      }
      public static void EnableBlurBehind(this Window window, bool enable, IntPtr region)
      {
         EnableBlurBehind(window, enable, region, false);
      }
      public static void EnableBlurBehind(this Window window, bool enable)
      {
         EnableBlurBehind(window, enable, IntPtr.Zero, false);
      }
      public static void EnableBlurBehind(this Window window)
      {
         EnableBlurBehind(window, true, IntPtr.Zero, false);
      }


      [DllImport("dwmapi.dll", PreserveSig = false)]
      private static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

      [DllImport("dwmapi.dll", PreserveSig = false)]
      private static extern bool DwmIsCompositionEnabled();

      [DllImport("dwmapi.dll", PreserveSig = false)]
      private static extern void DwmEnableBlurBehindWindow(IntPtr hWnd, ref BLURBEHIND pBlurBehind);

      [DllImport("dwmapi.dll", PreserveSig = false)]
      private static extern void DwmEnableComposition(bool bEnable);

      [DllImport("dwmapi.dll", PreserveSig = false)]
      private static extern void DwmGetColorizationColor(out int pcrColorization, [MarshalAs(UnmanagedType.Bool)]out bool pfOpaqueBlend);

      [DllImport("dwmapi.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool DwmDefWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

      public static RECT GetWindowRect(this Window window)
      {
         RECT windowPosition;
         GetWindowRect(new WindowInteropHelper(window).Handle, out windowPosition);
         return windowPosition;
      }
   }

   /// <summary>A Win32 Margins structure for the DWM api calls.</summary>
   [Serializable, StructLayout(LayoutKind.Sequential)]
   struct MARGINS
   {
      public MARGINS(System.Windows.Thickness t)
      {
         Left = (int)t.Left;
         Right = (int)t.Right;
         Top = (int)t.Top;
         Bottom = (int)t.Bottom;
      }
      public int Left;
      public int Right;
      public int Top;
      public int Bottom;
   }

   [Serializable, StructLayout(LayoutKind.Sequential)]
   public struct RECT
   {
      public RECT(Rect r)
      {
         Left = (int)r.Left;
         Right = (int)r.Right;
         Top = (int)r.Top;
         Bottom = (int)r.Bottom;
      }

      public int Left;
      public int Top;
      public int Right;
      public int Bottom;

      public void Offset(int dx, int dy)
      {
         Left += dx;
         Top += dy;
         Right += dx;
         Bottom += dy;
      }

      public static implicit operator Rect(RECT source)
      {
         return new Rect(source.Left, source.Top, Math.Abs(source.Right - source.Left), Math.Abs(source.Bottom - source.Top));
      }
      public static implicit operator RECT(Rect source)
      {
         return new RECT(source);
      }
      //public static explicit operator Rect(RECT source)
      //{
      //   return new Rect(source.Left, source.Top, (source.Right - source.Left), (source.Bottom - source.Top));
      //}
      //public static explicit operator RECT(Rect source)
      //{
      //   return new RECT(source);
      //}
   }

   [StructLayout(LayoutKind.Sequential)]
   struct BLURBEHIND
   {
      public BlurBehindOptions Flags;
      [MarshalAs(UnmanagedType.Bool)]
      public bool Enable;
      public IntPtr RegionBlur;
      [MarshalAs(UnmanagedType.Bool)]
      public bool TransitionOnMaximized;
   }

   [Flags]
   enum BlurBehindOptions : uint
   {
      Enable = 0x00000001,
      BlurRegion = 0x00000002,
      TransitionOnMaximized = 0x00000004,
   }
}