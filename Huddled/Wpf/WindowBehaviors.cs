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
   public abstract class Behavior : DependencyObject
   {
      /// <summary>
      /// Called when this behavior is initially hooked up to a <see cref="System.Windows.Window"/>
      /// <see cref="Behavior"/> implementations may override this to perfom actions
      /// on the actual window (the Chrome behavior uses this to change the template)
      /// </summary>
      /// <param name="window"></param>
      virtual public void AddTo(Window window) { }

      /// <summary>
      /// Called when this behavior is unhooked from a <see cref="System.Windows.Window"/>
      /// <see cref="Behavior"/> implementations may override this to perfom actions
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

   /// <summary>A collection of <see cref="Behavior"/>s</summary>
   public class WindowBehaviors : ObservableCollection<Behavior>
   {
      private WeakReference _owner;
      protected IntPtr WindowHandle;

      /// <summary>
      /// Initializes a new instance of the <see cref="WindowBehaviors"/> class.
      /// </summary>
      public WindowBehaviors()
      {
         Handlers = new List<MessageMapping>();
      }

      /// <summary>
      /// Gets the owner window
      /// </summary>
      /// <value>The owner.</value>
      public Window Window
      {
         get
         {
            if (_owner != null)
            {
               return _owner.Target as Window;
            } else return null;
         }
         set
         {
            if (_owner != null && WindowHandle != IntPtr.Zero)
            {
               HwndSource.FromHwnd(WindowHandle).RemoveHook(WndProc);
            }

            Debug.Assert(null != value);
            _owner = new WeakReference(value);

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

      protected override void InsertItem(int index, Behavior item)
      {
         base.InsertItem(index, item);
      }
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
         // Only expecting messages for our cached HWND.
         Debug.Assert(hwnd == WindowHandle);
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
