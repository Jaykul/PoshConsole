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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;

namespace Huddled.Wpf
{
   public static class Behaviors
   {
      //[AttachedPropertyBrowsableForType(typeof(Window))]
      // NOTE: The INTERNAL DependencyProperty hides it from the XAML parser, 
      //       which forces it to use the public Set accessor so we can do initialization
      private static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached(
          "InternalBehaviors", typeof(WindowBehaviors), typeof(Behaviors),
          new FrameworkPropertyMetadata(null, OnWindowBehaviorsChanged, CoerceWindowBehaviors));

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

      private static void OnBehaviorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea)
      {
         var behaviors = (WindowBehaviors)sender;
         // get the owner of the collection (the object to which it is attached)  
         var owner = behaviors.Window;
         if (owner != null)
         {
            if (nccea.Action == NotifyCollectionChangedAction.Add || nccea.Action == NotifyCollectionChangedAction.Replace)
            {
               foreach (Behavior behavior in nccea.NewItems)
               {
                  behavior.AddTo(owner);
                  behaviors.Handlers.AddRange(behavior.GetHandlers());
               }
            }
            if (nccea.Action == NotifyCollectionChangedAction.Remove || nccea.Action == NotifyCollectionChangedAction.Replace)
            {
               foreach (Behavior behavior in nccea.OldItems)
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

      /// <summary>Gets the behaviors.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <returns></returns>
      
      private static WindowBehaviors GetInternalBehaviors(Window window)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }

         var behaviors = (WindowBehaviors)window.GetValue(BehaviorsProperty);

         if(behaviors == null)
         {
            behaviors = new WindowBehaviors { Window = window };
            window.SetValue(BehaviorsProperty, behaviors);
         }
         else if(behaviors.Window != null)
         {
            behaviors.Window = window;
            window.SetValue(BehaviorsProperty, behaviors);
         }

         return behaviors;
      }

      public static WindowBehaviors GetBehaviors(Window window)
      {
         return GetInternalBehaviors(window);
      }

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
         window.SetValue(BehaviorsProperty, chrome);
      }
   }
}