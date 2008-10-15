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

namespace Huddled.Wpf
{
   public static partial class Native
   {
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
          "NativeBehaviors", typeof(WindowBehaviors), typeof(Native),
          new FrameworkPropertyMetadata(null, OnWindowBehaviorsChanged, CoerceWindowBehaviors));

      /// <summary>Sets the behaviors.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <param name="chrome">The chrome.</param>
      public static void SetBehaviors(Window window, WindowBehaviors chrome)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }
         window.SetValue(NativeBehaviorsProperty, chrome);
      }

      /// <summary>
      /// Gets the behaviors.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <returns>The <see cref="WindowBehaviors"/> collection.</returns>
      public static WindowBehaviors GetBehaviors(Window window)
      {
         return GetNativeBehaviors(window);
      }

      /// <summary>This is the internal/private <see cref="DependencyProperty"/> accessor.</summary>
      /// <param name="window">The window.</param>
      /// <returns>The <see cref="WindowBehaviors"/> collection.</returns>
      private static WindowBehaviors GetNativeBehaviors(Window window)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }

         var behaviors = (WindowBehaviors)window.GetValue(NativeBehaviorsProperty);

         if (behaviors == null)
         {
            behaviors = new WindowBehaviors { Target = window };
            window.SetValue(NativeBehaviorsProperty, behaviors);
         }

         Debug.Assert(behaviors.Target != null);
         //{
         //   behaviors.Window = window;
         //   window.SetValue(NativeBehaviorsProperty, behaviors);
         //}

         return behaviors;
      }


      /// <summary>
      /// Coerces values to window behaviors.
      /// </summary>
      /// <param name="dependency">The dependency.</param>
      /// <param name="value">The value.</param>
      /// <returns></returns>
      private static object CoerceWindowBehaviors(DependencyObject dependency, object value)
      {
         if (DesignerProperties.GetIsInDesignMode(dependency))
         {
            return value;
         }

         var window = (Window)dependency;
         var behaviors = (WindowBehaviors)value;

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

      /// <summary> Called when the <see cref="WindowBehaviors"/> collection is replaced.  
      /// This shouldn't actually happen in normal use (it <em>will not</em> happen purely through use in XAML).
      /// </summary>
      /// <param name="dependency">The dependency.</param>
      /// <param name="dpcEA">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> 
      /// instance containing the event data.</param>
      private static void OnWindowBehaviorsChanged(DependencyObject dependency, DependencyPropertyChangedEventArgs dpcEA)
      {
         if (DesignerProperties.GetIsInDesignMode(dependency))
         {
            return;
         }

         var window = (Window)dependency;
         var oldBehaviors = (WindowBehaviors)dpcEA.OldValue;
         var newBehaviors = (WindowBehaviors)dpcEA.NewValue;

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

      /// <summary>
      /// Called when [behaviors collection changed].
      /// </summary>
      /// <param name="sender">The sender.</param>
      /// <param name="nccea">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
      private static void OnBehaviorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea)
      {
         var behaviors = (WindowBehaviors)sender;
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


   }
}