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
         var source = PresentationSource.FromVisual(window);
         return GetLocalWorkAreaRect(window.RestoreBounds).DPITransformFromWindow(source);
      }

      public static Rect GetLocalWorkArea(IntPtr handle)
      {
         HwndSource source = HwndSource.FromHwnd(handle);
         return GetLocalWorkAreaRect(handle).DPITransformFromWindow(source);
      }


      [CLSCompliant(false)]
      public static Huddled.Interop.NativeMethods.ApiRect GetLocalWorkAreaRect(this Window window)
      {
         // Get handle for nearest monitor to this window
         var wih = new WindowInteropHelper(window);
         return GetLocalWorkAreaRect(wih.Handle);
      }

      [CLSCompliant(false)]
      public static Huddled.Interop.NativeMethods.ApiRect GetLocalWorkAreaRect(IntPtr handle)
      {
         // Get handle for nearest monitor to this window
         IntPtr hMonitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MonitorDefault.ToNearest);
         NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMonitor);
         return mi.MonitorWorkingSpaceRect;
      }

      [CLSCompliant(false)]
      public static Huddled.Interop.NativeMethods.ApiRect GetLocalWorkAreaRect(this Point position)
      {
         // Get handle for nearest monitor to this window
         IntPtr hMonitor = NativeMethods.MonitorFromPoint(position, NativeMethods.MonitorDefault.ToNearest);
         NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMonitor);
         return mi.MonitorWorkingSpaceRect;
      }

      [CLSCompliant(false)]
      public static Huddled.Interop.NativeMethods.ApiRect GetLocalWorkAreaRect(Rect source)
      {
         // Get handle for nearest monitor to this window
         NativeMethods.ApiRect rect = source;
         IntPtr hMonitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MonitorDefault.ToNearest);
         NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMonitor);
         return mi.MonitorWorkingSpaceRect;
      }


      [CLSCompliant(false)]
      public static Huddled.Interop.NativeMethods.ApiRect GetLocalWorkAreaRect(this NativeMethods.WindowPosition source)
      {
         // Get handle for nearest monitor to this window
         NativeMethods.ApiRect rect = source.ToApiRect();
         IntPtr hMonitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MonitorDefault.ToNearest);
         NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMonitor);
         return mi.MonitorWorkingSpaceRect;
      }
   }
}
