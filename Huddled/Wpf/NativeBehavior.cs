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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Interactivity;
using Huddled.Interop;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;

namespace Huddled.Wpf
{

   /// <summary>A behavior based on hooking a window message</summary>
   public abstract class NativeBehavior : Behavior<Window>
   {
      /// <summary>Gets the collection of active handlers.</summary>
      /// <value>A List of the mappings from <see cref="NativeMethods.WindowMessage"/>s
      /// to <see cref="NativeMethods.MessageHandler"/> delegates.</value>
      protected abstract IEnumerable<MessageMapping> Handlers { get; }

      /// <summary>The HWND handle to our window</summary>
      protected IntPtr WindowHandle { get; private set; }

      /// <summary>
      /// Called after the window source is initialized, 
      /// after the WindowHandle property has been set, 
      /// and after the window has been hooked by the NativeBehavior WndProc
      /// </summary>
      protected virtual void OnWindowSourceInitialized() {}

      /// <summary>
      /// Called after the behavior is attached to an AssociatedObject.
      /// </summary>
      /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
      protected override void OnAttached()
      {
         // design mode bailout (in Design mode there's no window, and no wndproc)
         // this doesn't happen in System.Windows.Interactivity.Behaviors
         if (DesignerProperties.GetIsInDesignMode(AssociatedObject))
         {
            return;
         }

         // If we can get a handle, then the window has already been initialized.
         WindowHandle = new WindowInteropHelper(AssociatedObject).Handle;
         if (IntPtr.Zero == WindowHandle)
         {
            AssociatedObject.SourceInitialized += (sender, e) =>
            {
               WindowHandle = new WindowInteropHelper(AssociatedObject).Handle;

               var source = HwndSource.FromHwnd(WindowHandle);
               if (source != null)
               {
                  source.AddHook(WndProc);
               }

               OnWindowSourceInitialized();
            };
         }
         else
         {
            var source = HwndSource.FromHwnd(WindowHandle);
            if (source != null)
            {
               source.AddHook(WndProc);
            }
            OnWindowSourceInitialized();
         }

         base.OnAttached();
      }

      /// <summary>
      /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
      /// </summary>
      /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
      protected override void OnDetaching()
      {
         if (IntPtr.Zero != WindowHandle)
         {
            var source = HwndSource.FromHwnd(WindowHandle);
            if (source != null)
            {
               source.RemoveHook(WndProc);
            }
         }
         WindowHandle = IntPtr.Zero;
         base.OnDetaching();
      }


      /// <summary>
      /// A Window Process Message Handler delegate
      /// which processes all the registered message mappings
      /// </summary>
      /// <param name="hwnd">The window handle.</param>
      /// <param name="msg">The message.</param>
      /// <param name="wParam">The wParam.</param>
      /// <param name="lParam">The lParam.</param>
      /// <param name="handled">Set to true if the message has been handled</param>
      /// <returns>IntPtr.Zero</returns>
      [DebuggerNonUserCode]
      private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         Contract.Assert(hwnd == WindowHandle); // Only expecting messages for our cached HWND.

         // cast and cache the message
         var message = (NativeMethods.WindowMessage)msg;

         // NOTE: we may process a message multiple times
         // and we have no good way to handle that...
         var result = IntPtr.Zero;
         foreach (var handlePair in Handlers.Where(handlePair => handlePair.Key == message))
         {
            var r = handlePair.Value(wParam, lParam, ref handled);
            // So, we'll return the last non-zero result (if any)
            if (r != IntPtr.Zero) { result = r; }
         }
         return result;
      }

   }

}
