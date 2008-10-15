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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Huddled.Interop;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;

namespace Huddled.Wpf
{
   public class NativeBehaviors : ObservableCollection<NativeBehavior>
   {
      /// <summary>The HWND handle to our window</summary>
      protected IntPtr WindowHandle { get; private set; }
      /// <summary>Gets the collection of active handlers.</summary>
      /// <value>A List of the mappings from <see cref="NativeMethods.WindowMessage"/>s
      /// to <see cref="NativeMethods.MessageHandler"/> delegates.</value>
      protected List<MessageMapping> Handlers { get; set; }
      /// <summary>
      /// The reference to the Target or "owner" window 
      /// should be accessed through the <see cref="NativeBehaviors.Window"/> property.
      /// </summary>
      private WeakReference _owner;
      /// <summary>Gets or Sets the target/owner window</summary>
      /// <value>The <see cref="Window"/> these Native Behavrios affect.</value>
      public Window Target
      {
         get
         {
            if (_owner != null)
            {
               return _owner.Target as Window;
            }
            else return null;
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

      ///// <summary>Initializes a new instance of the <see cref="NativeBehaviors"/> class
      ///// with no behaviors and no owner window
      ///// </summary>
      //public NativeBehaviors() { Handlers = new List<MessageMapping>(); }

      /// <summary>Initializes a new instance of the <see cref="NativeBehaviors"/> class
      /// with the specified target <see cref="Window"/> 
      /// and <em>no</em> <see cref="NativeBehavior"/>s.
      /// </summary>
      /// <param name="target">The Window to be affected by this collection of behaviors</param>
      public NativeBehaviors(Window target) { 
         Handlers = new List<MessageMapping>();
         Target = target;
         target.SetValue(NativeBehaviorsProperty, this);
      }

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


      #region The Attached DependencyProperty
      /// <summary>
      /// The Behaviors DependencyProperty is the collection of WindowMessage-based behaviors
      /// </summary>
      /// <remarks>
      /// Making the DependencyProperty Private or Internal means that the XAML parser can't see it.
      /// However, the XAML parser *can* see the Public "GetBehaviors" and/or "SetBehaviors" methods
      /// So when you use <code><![CDATA[<wpf:Native.Behaviors />]]></code> in XAML, it will use the 
      /// GetBehaviors method, which gives us the opportunity to initialize the collection -- this 
      /// will not work if you used the name of a public DependencyProperty.
      /// </remarks>
      /// <example><![CDATA[
      /// <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      ///     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      ///     xmlns:huddled="http://schemas.huddledmasses.org/wpf"
      ///     >
      ///     <huddled:Behaviors.Behaviors>
      ///         <huddled:SnappingWindow SnapDistance="40" />
      ///     </huddled:Behaviors.Behaviors>
      ///     <Grid><Label Content="Drag this window near the screen edges"/></Grid>
      /// </Window>
      /// ]]></example>
      private static readonly DependencyProperty NativeBehaviorsProperty = DependencyProperty.RegisterAttached(
          "NativeBehaviors", typeof(NativeBehaviors), typeof(NativeBehaviors),
          new FrameworkPropertyMetadata(null, OnNativeBehaviorsChanged, CoerceNativeBehaviors));

      #region The PUBLIC accessors which wrap the hidden dependency property
      /// <summary>Sets the behaviors.</summary>
      /// <param name="window">The window.</param>
      /// <param name="behaviors">The collection of <see cref="NativeBehavior"/>s.</param>
      public static void SetBehaviors(Window window, NativeBehaviors behaviors)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }
         window.SetValue(NativeBehaviorsProperty, behaviors);
      }

      /// <summary>Gets the behaviors.</summary>
      /// <param name="window">The window.</param>
      /// <returns>The collection of <see cref="NativeBehavior"/>s.</returns>
      public static NativeBehaviors GetBehaviors(Window window)
      {
         return GetNativeBehaviors(window);
      }

      public static IEnumerable<TBehavior> SelectBehaviors<TBehavior>(Window window) where TBehavior : NativeBehavior
      {
         foreach (var behavior in NativeBehaviors.GetBehaviors(window))
         {
            if (behavior is TBehavior)
            {
               yield return (TBehavior)behavior;
            }
         }
      }
      #endregion
      /// <summary>Gets the behaviors.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <returns></returns>
      private static NativeBehaviors GetNativeBehaviors(Window window)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }

         var behaviors = (NativeBehaviors)window.GetValue(NativeBehaviorsProperty);

         if (behaviors == null)
         {
            behaviors = new NativeBehaviors(window);
         }

         Debug.Assert(behaviors.Target != null);
         //{
         //   behaviors.Window = window;
         //   window.SetValue(NativeBehaviorsProperty, behaviors);
         //}

         return behaviors;
      }

      #region The helper methods which handle misassignments
      /// <summary>
      /// Verify that assigned values are, in fact, <see cref="NativeBehaviors"/>
      /// </summary>
      /// <param name="dependency">The Window</param>
      /// <param name="value">The NativeBehaviors</param>
      /// <returns></returns>
      private static object CoerceNativeBehaviors(DependencyObject dependency, object value)
      {
         if (DesignerProperties.GetIsInDesignMode(dependency))
         {
            return value;
         }

         var window = (Window)dependency;
         var behaviors = (NativeBehaviors)value;

         if (window == null)
         {
            throw new ArgumentNullException("dependency");
         }

         if (behaviors == null)
         {
            throw new ArgumentNullException("value");
         }

         if (!window.CheckAccess())
         {
            throw new NotSupportedException();
         }

         return behaviors;
      }
      /// <summary>
      /// Hook up our CollectionChanged event when the new NativeBehaviors collection
      /// is set for a <see cref="Window"/>, and add any initial behaviors to the window.
      /// </summary>
      /// <remarks>Although this method handles removing or replacing a NativeBehavior collection
      /// from a Window, it isn't recommended that you try to do that, and this may be deprecated
      /// in a future release (that is: once you assign a NativeBehaviors collection to a Window,
      /// you shouldn't try to remove it or replace it.  Certainly if you do it in XAML, you need
      /// not worry about that ever happening...
      /// </remarks>
      /// <param name="dependency">The <see cref="Window"/></param>
      /// <param name="dpcEA">The <see cref="EventArgs"/></param>
      private static void OnNativeBehaviorsChanged(DependencyObject dependency, DependencyPropertyChangedEventArgs dpcEA)
      {
         if (DesignerProperties.GetIsInDesignMode(dependency))
         {
            return;
         }

         var window = (Window)dependency;
         var oldBehaviors = (NativeBehaviors)dpcEA.OldValue;
         var newBehaviors = (NativeBehaviors)dpcEA.NewValue;

         if (newBehaviors != null)
         {
            foreach (var behavior in newBehaviors)
            {
               behavior.AddTo(window);
               newBehaviors.Handlers.AddRange(behavior.GetHandlers());
            }
            newBehaviors.CollectionChanged += OnBehaviorsCollectionChanged;
         }

         if (oldBehaviors != null)
         {
            foreach (var behavior in oldBehaviors)
            {
               behavior.RemoveFrom(window);
               foreach (var h in behavior.GetHandlers())
               {
                  oldBehaviors.Handlers.Remove(h);
               }
            }
            oldBehaviors.CollectionChanged -= OnBehaviorsCollectionChanged;
         }

      }
      #endregion
      /// <summary>
      /// Handles changes to the NativeBehaviors collection, invoking the <see cref="NativeBehavior.AddTo"/>
      /// and <see cref="NativeBehavior.RemoveFrom"/> methods, and adding their handlers to the list.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="nccea"></param>
      private static void OnBehaviorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea)
      {
         var behaviors = (NativeBehaviors)sender;
         // get the owner of the collection (the object to which it is attached)  
         var owner = behaviors.Target;
         if (owner != null)
         {
            if (nccea.Action == NotifyCollectionChangedAction.Add || nccea.Action == NotifyCollectionChangedAction.Replace)
            {
               foreach (NativeBehavior behavior in nccea.NewItems)
               {
                  behavior.AddTo(owner);
                  behaviors.Handlers.AddRange(behavior.GetHandlers());
               }
            }
            if (nccea.Action == NotifyCollectionChangedAction.Remove || nccea.Action == NotifyCollectionChangedAction.Replace)
            {
               foreach (NativeBehavior behavior in nccea.OldItems)
               {
                  behavior.RemoveFrom(owner);
                  foreach (var h in behavior.GetHandlers())
                  {
                     behaviors.Handlers.Remove(h);
                  }
               }
            }
         }
      }
      #endregion
   }
}