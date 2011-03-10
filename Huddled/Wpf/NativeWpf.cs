using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace Huddled.Wpf
{
   public abstract class NativeWpf
   {
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
// ReSharper disable InconsistentNaming
      private static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached(
          "Behaviors", typeof(NativeBehaviors), typeof(NativeBehaviors),
          new PropertyMetadata(new NativeBehaviors()));
// ReSharper restore InconsistentNaming

      #region The PUBLIC accessors which wrap the hidden dependency property
      ///// <summary>Sets the behaviors.</summary>
      ///// <param name="window">The window.</param>
      ///// <param name="behaviors">The collection of <see cref="NativeBehavior"/>s.</param>
      //public static void SetBehaviors(Window window, NativeBehaviors behaviors)
      //{
      //   if (window == null)
      //   {
      //      throw new ArgumentNullException("window");
      //   }
      //   window.SetValue(NativeBehaviorsProperty, behaviors);
      //}

      /// <summary>Gets the behaviors.</summary>
      /// <param name="obj">The window.</param>
      /// <returns>The collection of <see cref="NativeBehavior"/>s.</returns>
      public static NativeBehaviors GetBehaviors(DependencyObject obj)
      {
         Debug.Assert(obj is Window,"NativeBehaviors.Behaviors can only apply to top-level Window objects.");
         var window = (Window)obj;
         var behaviors = (NativeBehaviors) window.GetValue(BehaviorsProperty);
         if (behaviors == null)
         {
            behaviors = new NativeBehaviors();
            window.SetValue(BehaviorsProperty, behaviors);
         }
         return behaviors;
      }

      public static IEnumerable<TBehavior> SelectBehaviors<TBehavior>(Window window) where TBehavior : NativeBehavior
      {
         return GetBehaviors(window).OfType<TBehavior>();
      }
 

      #endregion
      ///// <summary>Gets the behaviors.
      ///// </summary>
      ///// <param name="window">The window.</param>
      ///// <returns></returns>
      //private static NativeBehaviors GetNativeBehaviors(Window window)
      //{
      //   if (window == null) { throw new ArgumentNullException("window"); }
      //   // This is the plain old normal thing:
      //   var behaviors = (NativeBehaviors)window.GetValue(NativeBehaviorsProperty);
      //   // Our raison d'être: create a new collection if there isn't one yet
      //   if (behaviors == null) { behaviors = new NativeBehaviors(window); }

      //   return behaviors;
      //}


      #endregion

   }
}
