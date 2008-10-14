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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Huddled.Interop.Windows;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.Windows.NativeMethods.WindowMessage, Huddled.Interop.Windows.NativeMethods.MessageHandler>;

namespace Huddled.Wpf
{

   /// <summary>A behavior based on hooking a window message</summary>
   public abstract class NativeBehavior : DependencyObject
   {
      /// <summary>
      /// Called when this behavior is initially hooked up to an initialized <see cref="System.Windows.Window"/>
      /// <see cref="NativeBehavior"/> implementations may override this to perfom actions
      /// on the actual window (the Chrome behavior uses this to change the template)
      /// </summary>
      /// <remarks>Implementations should NOT depend on this being exectued before 
      /// the Window is SourceInitialized, and should use a WeakReference if they need 
      /// to keep track of the window object...
      /// </remarks>
      /// <param name="window"></param>
      virtual public void AddTo(Window window) { }

      /// <summary>
      /// Called when this behavior is unhooked from a <see cref="System.Windows.Window"/>
      /// <see cref="NativeBehavior"/> implementations may override this to perfom actions
      /// on the actual window.
      /// </summary>
      /// <param name="window"></param>
      virtual public void RemoveFrom(Window window) { }

      /// <summary>
      /// Gets the <see cref="MessageMapping"/>s for this behavior 
      /// (one for each Window Message you need to handle)
      /// </summary>
      /// <value>A collection of <see cref="MessageMapping"/> objects.</value>
      public abstract IEnumerable<MessageMapping> GetHandlers();
   }

   /// <summary>A collection of <see cref="NativeBehavior"/>s</summary>
   public class WindowBehaviors : ObservableCollection<NativeBehavior>
   {
      private WeakReference _target;
      protected IntPtr WindowHandle;

      /// <summary>
      /// Initializes a new instance of the <see cref="WindowBehaviors"/> class.
      /// </summary>
      public WindowBehaviors()
      {
         Handlers = new List<MessageMapping>();
      }


      /// <summary>
      /// Gets or sets the target <see cref="Window"/>
      /// </summary>
      /// <value>The Window being altered by this behavior.</value>
      public Window Target
      {
         get
         {
            if (_target != null)
            {
               return _target.Target as Window;
            } else return null;
         }
         set
         {
            if (_target != null && WindowHandle != IntPtr.Zero)
            {
               HwndSource.FromHwnd(WindowHandle).RemoveHook(WndProc);
            }

            Debug.Assert(null != value);
            _target = new WeakReference(value);

            // Use whether we can get an HWND to determine if the Window has been loaded.
            WindowHandle = new WindowInteropHelper(value).Handle;


            if (IntPtr.Zero == WindowHandle)
            {
               value.SourceInitialized += (sender, e) =>
               {
                  WindowHandle = new WindowInteropHelper((Window)sender).Handle;
                  HwndSource.FromHwnd(WindowHandle).AddHook(WndProc);
               };
            }
            else
            {
               HwndSource.FromHwnd(WindowHandle).AddHook(WndProc);
            }
         }
      }

      /// <summary>
      /// Gets the collection of active handlers.
      /// </summary>
      /// <value>The handlers.</value>
      public List<MessageMapping> Handlers { get; private set; }

      /// <summary>
      /// A WndProc handler which processes all the registered message mappings
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
         var message = (NativeMethods.WindowMessage)msg;

         foreach (var handlePair in Handlers)
         {
            if (handlePair.Key == message)
            {
               return handlePair.Value(wParam, lParam, ref handled);
            }
         }
         return IntPtr.Zero;
      }
   }

}
