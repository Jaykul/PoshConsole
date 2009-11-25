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
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(wih.Handle, NativeMethods.MonitorDefault.ToNearest);
            NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMonitor);
            HwndSource source = HwndSource.FromHwnd(wih.Handle);
            return mi.MonitorWorkingSpaceRect.DPITransformFromWindow(source);
         }
   }
}
