using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Huddled.Interop.Windows
{
   public static class Extensions
   {
         public static Rect GetLocalWorkArea(this Window window)
         {
            // Get handle for nearest monitor to this window
            var wih = new WindowInteropHelper(window);
            return GetLocalWorkArea(wih.Handle);
         }

         public static Rect GetLocalWorkArea(IntPtr handle)
         {
            HwndSource source = HwndSource.FromHwnd(handle);
            return GetLocalWorkAreaRect(handle).DPITransformFromWindow(source);
         }

         public static Huddled.Interop.NativeMethods.ApiRect GetLocalWorkAreaRect(this Window window)
         {
            // Get handle for nearest monitor to this window
            var wih = new WindowInteropHelper(window);
            return GetLocalWorkAreaRect(wih.Handle);
         }
         public static Huddled.Interop.NativeMethods.ApiRect GetLocalWorkAreaRect(IntPtr handle)
         {
            // Get handle for nearest monitor to this window
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MonitorDefault.ToNearest);
            NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMonitor);
            return mi.MonitorWorkingSpaceRect;
         }
   }
}
