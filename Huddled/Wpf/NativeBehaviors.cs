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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using Huddled.Interop;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;

namespace Huddled.Wpf
{
   public class NativeBehaviors :  ObservableCollection<NativeBehavior>//, INativeBehavior
   {
      /// <summary>The HWND handle to our window</summary>
      protected IntPtr WindowHandle { get; private set; }
      /// <summary>Gets the collection of active handlers.</summary>
      /// <value>A List of the mappings from <see cref="NativeMethods.WindowMessage"/>s
      /// to <see cref="NativeMethods.MessageHandler"/> delegates.</value>
	  [CLSCompliant(false)]
	  protected List<MessageMapping> Handlers { get; set; }
      /// <summary>
      /// The reference to the Target or "owner" window 
      /// should be accessed through the <see cref="Window"/> property.
      /// </summary>
      private WeakReference _owner;
      /// <summary>Gets or Sets the target/owner window</summary>
      /// <value>The <see cref="Window"/> these Native Behavrios affect.</value>



      ///// <summary>Initializes a new instance of the <see cref="NativeBehaviors"/> class
      ///// with the specified target <see cref="Window"/> 
      ///// and <see cref="NativeBehavior"/>s.
      ///// </summary>
      ///// <param name="target">The Window to be affected by this collection of behaviors</param>
      ///// <param name="behaviors">The NativeBehaviors</param>
      //public NativeBehaviors(Window target, NativeBehaviors behaviors)
      //{
      //   Handlers = new List<MessageMapping>();
      //   Window = target;
      //   target.SetValue(NativeBehaviorsProperty, behaviors);
      //}

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
         Debug.Assert(hwnd == WindowHandle); // Only expecting messages for our cached HWND.

         // cast and cache the message
         var message = (NativeMethods.WindowMessage)msg;
         // NOTE: we may process a message multiple times
         // and we have no good way to handle that...
         var result = IntPtr.Zero;
         foreach (var handlePair in Handlers)
         {  if (handlePair.Key == message)
            {
               var r = handlePair.Value(wParam, lParam, ref handled);
               // So, we'll return the last non-zero result (if any)
               if (r != IntPtr.Zero) { result = r; }
         }  }
         return result;
      }


      private Window _target;

      /// <summary>
      /// Attaches to the specified object.
      /// </summary>
      /// <param name="window">The object to attach to.</param>
      public void Attach(Window window)
      {
         _target = window;

         Handlers = new List<MessageMapping>();
         // Target.SetValue(NativeBehaviorsProperty, this);

         // design mode bailout (in Design mode there's no window, and no wndproc)
         if (DesignerProperties.GetIsInDesignMode(_target)) { return; }

         if (_owner != null && WindowHandle != IntPtr.Zero)
         {
            HwndSource.FromHwnd(WindowHandle).RemoveHook(WndProc);
         }

         _owner = new WeakReference(_target);

         // Use whether we can get an HWND to determine if the Window has been loaded.
         WindowHandle = new WindowInteropHelper(_target).Handle;


         if (IntPtr.Zero == WindowHandle)
         {
            _target.SourceInitialized += (sender, e) =>
            {
               WindowHandle = new WindowInteropHelper((Window)sender).Handle;
               HwndSource.FromHwnd(WindowHandle).AddHook(WndProc);
            };
         }
         else
         {
            HwndSource.FromHwnd(WindowHandle).AddHook(WndProc);
         }


         foreach (var behavior in this)
         {
            behavior.Attach(window);
            Handlers.AddRange(behavior.GetHandlers());

         }
      }

      /// <summary>
      /// Detaches this instance from its associated object.
      /// </summary>
      public void Detach()
      {
         foreach (var behavior in this)
         {
            behavior.Detach();
            foreach (var h in behavior.GetHandlers())
            {
               Handlers.Remove(h);
            }
         }
      }

      /// <summary>
      /// Gets the associated object.
      /// </summary>
      /// <value>
      /// The associated object.
      /// </value>
      /// <remarks>
      /// Represents the object the instance is attached to.
      /// </remarks>
      public Window AssociatedObject
      {
         get { return _target; }
      }
   }
}