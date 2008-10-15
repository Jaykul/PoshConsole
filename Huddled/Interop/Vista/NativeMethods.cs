// Copyright (c) 2008 Joel Bennett http://HuddledMasses.org/

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
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Huddled.Wpf;

namespace Huddled.Interop
{
   /// <summary>
   /// An addendum to the Win32.NativeMethods class with all the methods which are needed by the Vista.NativeMethods class
   /// Placed here for portability between projects, and to assign blame ;-)
   /// </summary>
   public static partial class NativeMethods
   {

      #region Native Values

      #endregion

      #region Native Types


      [StructLayout(LayoutKind.Sequential)]
      public class DWM_BLURBEHIND
      {
         public uint dwFlags;
         [MarshalAs(UnmanagedType.Bool)]
         public bool fEnable;
         public IntPtr hRegionBlur;
         [MarshalAs(UnmanagedType.Bool)]
         public bool fTransitionOnMaximized;

         public const uint DWM_BB_ENABLE = 0x00000001;
         public const uint DWM_BB_BLURREGION = 0x00000002;
         public const uint DWM_BB_TRANSITIONONMAXIMIZED = 0x00000004;
      }
      #endregion




      #region Custom Wrapper methods
      /// <summary>
      /// Attempt to extend the Glass frame into client area of a <see cref="Window"/>, 
      /// using it's <see cref="Control.BorderThickness"/> as the frame areas.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <returns>True if attempt succeeded, False otherwise</returns>
      public static bool TryExtendFrameIntoClientArea(this Window window)
      {
         return TryExtendFrameIntoClientArea(window, window.BorderThickness);
      }

      /// <summary>
      /// Attempt to extend the Glass frame into client area of a <see cref="Window"/>, 
      /// with the specified Thickness for the frame margin
      /// </summary>
      /// <param name="window">The window.</param>
      /// <param name="margin">The margin.</param>
      /// <returns>True if attempt succeeded, False otherwise</returns>
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

      /// <summary>Extends the frame into client area of a window by it's Handle,
      /// using the specified <see cref="Thickness"/> as the frame margin
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

         Margins margins = new Margins(margin);
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

      /// <summary>
      /// Toggle the Blurred glass on the specified window region of the client area of a Window.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <param name="enable">if set to <c>true</c> enable, otherwise disable.</param>
      /// <param name="region">A pointer to the region to to enable blur on, or IntPtr.Zero for the whole window</param>
      /// <param name="transition">If set to <c>true</c> transition the blur on Maximized.</param>
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
      /// <summary>
      /// Toggle the Blurred glass on the specified window region of the client area of a Window, without a transition
      /// </summary>
      /// <param name="window">The window.</param>
      /// <param name="enable">if set to <c>true</c> enable, otherwise disable.</param>
      /// <param name="region">A pointer to the region to to enable blur on, or IntPtr.Zero for the whole window</param>
      public static void EnableBlurBehind(this Window window, bool enable, IntPtr region)
      {
         EnableBlurBehind(window, enable, region, false);
      }
      /// <summary>
      /// Toggle the Blurred glass behind the entire client area of a Window, without a transition
      /// </summary>
      /// <param name="window">The window.</param>
      /// <param name="enable">if set to <c>true</c> enable, otherwise disable.</param>
      public static void EnableBlurBehind(this Window window, bool enable)
      {
         EnableBlurBehind(window, enable, IntPtr.Zero, false);
      }

      /// <summary>
      /// Enable the Blurred glass behind the entire client area of a Window without a transition.
      /// </summary>
      /// <param name="window">The window.</param>
      public static void EnableBlurBehind(this Window window)
      {
         EnableBlurBehind(window, true, IntPtr.Zero, false);
      }
      #endregion Custom Wrapper methods
      
      #region PInvoke method signatures

      [DllImport("dwmapi.dll", PreserveSig = false)]
      private static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins margins);

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
      private static extern bool GetWindowRect(IntPtr hWnd, out ApiRect lpApiRect);
      
      public static ApiRect GetWindowRect(this Window window)
      {
         ApiRect windowPosition;
         GetWindowRect(new WindowInteropHelper(window).Handle, out windowPosition);
         return windowPosition;
      }

      [DllImport("dwmapi.dll")]
      public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out System.Drawing.Size size);

      // ************************************************
      // ***** VISTA ONLY *******************************
      // ************************************************
      //[DllImport("dwmapi.dll")]
      //public static extern int DwmRegisterThumbnail(IntPtr hwndDestination, IntPtr hwndSource, IntPtr pReserved, out SafeThumbnailHandle phThumbnailId);
      [DllImport("dwmapi.dll")]
      public static extern int DwmRegisterThumbnail(IntPtr hwndDestination, IntPtr hwndSource, out IntPtr hThumbnailId);

      [DllImport("dwmapi.dll")]
      public static extern int DwmUnregisterThumbnail(IntPtr hThumbnailId);

      //[DllImport("dwmapi.dll")]
      //public static extern int DwmUpdateThumbnailProperties(SafeThumbnailHandle hThumbnailId, ref DwmThumbnailProperties ptnProperties);
      [DllImport("dwmapi.dll")]
      public static extern int DwmUpdateThumbnailProperties(IntPtr hThumbnailId, ref ThumbnailProperties thumbProps);

      [DllImport("dwmapi.dll")]
      public static extern int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool pfEnabled);

      #endregion PInvoke method signatures

      #region Structs for use with the Windows API



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

      [Serializable, StructLayout(LayoutKind.Sequential)]
      public struct ThumbnailProperties
      {
         public ThumbnailFlags Flags;
         public ApiRect Destination;
         public ApiRect Source;

         public byte Opacity;

         [MarshalAs(UnmanagedType.Bool)]
         public bool Visible;

         [MarshalAs(UnmanagedType.Bool)]
         public bool ClientAreaOnly;

         public ThumbnailProperties(ApiRect destination, ThumbnailFlags flags)
         {
            Source = new ApiRect();
            Destination = destination;
            Flags = flags;

            Opacity = 255;
            Visible = true;
            ClientAreaOnly = false;
         }
      }

      #endregion Structs for use with the Windows API

      #region Enums for use with the Windows API

      [Flags()]
      public enum ThumbnailFlags : uint
      {
         /// <summary>
         /// Indicates a value for fSourceClientAreaOnly has been specified.
         /// </summary>
         RectDestination = 0x01,
         /// <summary>
         /// Indicates a value for rcSource has been specified.
         /// </summary>
         RectSource = 0x02,
         /// <summary>
         /// Indicates a value for opacity has been specified.
         /// </summary>
         Opacity = 0x04,
         /// <summary>
         /// Indicates a value for fVisible has been specified.
         /// </summary>
         Visible = 0x08,
         /// <summary>
         /// Indicates a value for fSourceClientAreaOnly has been specified.
         /// </summary>
         SourceClientAreaOnly = 0x10
      }


      #endregion [rgn]

   }
}