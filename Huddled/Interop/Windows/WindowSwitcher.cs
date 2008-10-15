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
using System.Windows;
using System.Windows.Interop;

namespace Huddled.Interop.Windows
{
   /// <summary>
   /// An extension class with methods to simulate Alt+Tab ...
   /// </summary>
   public static class WindowSwitcher
   {

      public static void ActivateNextWindow()
      {
         ActivateNextWindow(Application.Current.MainWindow);
      }

      public static void ActivateNextWindow(this Window current)
      {
         IntPtr next = GetNextWindow(current);

         NativeMethods.ShowWindow(next, NativeMethods.ShowWindowCommand.Show);
         NativeMethods.SetForegroundWindow(next);
      }

      private static IntPtr GetNextWindow(this Window relativeTo)
      {
         IntPtr current = GetWindowHandle(relativeTo);
         IntPtr next = NativeMethods.GetWindow(current, NativeMethods.GetWindowCommand.Next);

         while (!NativeMethods.IsWindowVisible(next) && (next != current))
         {
            next = NativeMethods.GetWindow(next, NativeMethods.GetWindowCommand.Next);
         }

         return next;
      }


      public static void ActivatePreviousWindow()
      {
         ActivatePreviousWindow(Application.Current.MainWindow);
      }

      public static void ActivatePreviousWindow(this Window current)
      {
         IntPtr previous = GetPreviousWindow(current);

         NativeMethods.ShowWindow(previous, NativeMethods.ShowWindowCommand.Show);
         NativeMethods.SetForegroundWindow(previous);
      }

      private static IntPtr GetPreviousWindow(this Window relativeTo)
      {
         IntPtr current = GetWindowHandle(relativeTo);
         IntPtr previous = NativeMethods.GetWindow(current, NativeMethods.GetWindowCommand.Previous);

         while (!NativeMethods.IsWindowVisible(previous) && (previous != current))
         {
            previous = NativeMethods.GetWindow(previous, NativeMethods.GetWindowCommand.Previous);
         }

         return previous;
      }

      private static IntPtr GetWindowHandle(this Window window)
      {
         return new WindowInteropHelper(window).Handle;
      }

   }

}
