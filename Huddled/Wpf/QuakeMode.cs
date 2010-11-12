// Copyright (c) 2008 Joel Bennett http://HuddledMasses.org/ http://HuddledMasses.org/

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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interactivity;
using Huddled.Interop;
using Huddled.Interop.Windows;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;
using System.Windows.Media.Animation;
using System.Windows.Data;
using System.Windows.Interop;
using EventTrigger = System.Windows.EventTrigger;


namespace Huddled.Wpf
{
   public class QuakeMode : Behavior<Window>
   {

      private readonly List<EventTrigger> _triggers = new List<EventTrigger>();
      protected override void OnAttached()
      {
         base.OnAttached();

         var duration = new Duration(TimeSpan.FromSeconds(Duration));
         var sizeProperty = new PropertyPath((Dimension == Direction.Height) ? FrameworkElement.HeightProperty : FrameworkElement.WidthProperty);

         var lostFocus = new Storyboard();
         var gotFocus = new Storyboard();


         if (Dimension == Direction.Height)
         {
            AssociatedObject.Deactivated += (sender, ea) =>
                                               { 
                                                  Size = ((Window)sender).Height;
                                                  if (Enabled)
                                                  {
                                                     lostFocus.Begin();
                                                  }
                                               };
         }
         else
         {
            AssociatedObject.Deactivated += (sender, ea) =>
                                               { 
                                                  Size = ((Window)sender).Width;
                                                  if (Enabled)
                                                  {
                                                     lostFocus.Begin();
                                                  }
                                               };
         }
         AssociatedObject.Activated += (sender, ea) =>
                                          {
                                             if (Enabled)
                                             {
                                                gotFocus.Begin();
                                             } 
                                          };


         // The HIDING animations
         // Animate the opacity

		 var fade = new DoubleAnimation
		            	{
		            		To = 0.0,
		            		Duration = new Duration(TimeSpan.FromSeconds(Duration + .5)),
		            		EasingFunction = new SineEase()
		            	};
      	Storyboard.SetTarget(fade, AssociatedObject);
         Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));

         // The HIDING animations
         // Animate the height
         var shrink = new DoubleAnimation
                      	{
							From = 400,
                      		To = 10.0,
							Duration = new Duration(TimeSpan.FromSeconds(5)), 
							EasingFunction = new QuarticEase()
                      	};
		 //var fromBinding = new Binding("SizeProperty") { Source = this, Mode = BindingMode.OneWayToSource };
		 //BindingOperations.SetBinding(shrink, DoubleAnimation.FromProperty, fromBinding);
      	 Storyboard.SetTarget(shrink, AssociatedObject);
         Storyboard.SetTargetProperty(shrink, sizeProperty);

         // The HIDING animations
         // Toggle the visibility AFTER it should be hidden
		 var hide = new ObjectAnimationUsingKeyFrames();
		 hide.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(Duration + .5))));
		 Storyboard.SetTarget(hide, AssociatedObject);
		 Storyboard.SetTargetProperty(hide, new PropertyPath(UIElement.VisibilityProperty));
         
         //var normalize = new ObjectAnimationUsingKeyFrames();
         //normalize.KeyFrames.Add(new DiscreteObjectKeyFrame(WindowState.Normal, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
         //Storyboard.SetTarget(normalize, window);
         //Storyboard.SetTargetProperty(normalize, new PropertyPath(Window.WindowStateProperty));

         //lostFocus.Children.Add(normalize);
         lostFocus.Children.Add(shrink);
         //lostFocus.Children.Add(fade);
		 //lostFocus.Children.Add(hide);


         // The SHOWING animations
         // Animate the opacity 
         var unfade = new DoubleAnimation { To = 1.0, Duration = duration };
		 var toOpacity = new Binding("OpacityProperty") { Source = this, Mode = BindingMode.OneWayToSource };
         BindingOperations.SetBinding(unfade, DoubleAnimation.ToProperty, toOpacity);
         Storyboard.SetTarget(unfade, AssociatedObject);
         Storyboard.SetTargetProperty(unfade, new PropertyPath(UIElement.OpacityProperty));

         // The SHOWING animations
         // Animate the height
         var grow = new DoubleAnimation { To = 250.0, Duration = duration };
         var toBinding = new Binding("SizeProperty") { Source = this, Mode = BindingMode.OneWayToSource};
         BindingOperations.SetBinding(grow, DoubleAnimation.ToProperty, toBinding);
         Storyboard.SetTarget(grow, AssociatedObject);
         Storyboard.SetTargetProperty(grow, sizeProperty);

         // The SHOWING animations
         // Toggle the visibility BEFORE we try to animate it
         var show = new ObjectAnimationUsingKeyFrames();
         show.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
         Storyboard.SetTarget(show, AssociatedObject);
         Storyboard.SetTargetProperty(show, new PropertyPath(UIElement.VisibilityProperty));

         //// The SHOWING animations
         //// Toggle the WindowState
         //var maximize = new ObjectAnimationUsingKeyFrames();
         //maximize.KeyFrames.Add(new DiscreteObjectKeyFrame(WindowState.Maximized, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(Duration))));
         //Storyboard.SetTarget(maximize, window);
         //Storyboard.SetTargetProperty(maximize, new PropertyPath(System.Windows.Window.WindowStateProperty));

         //gotFocus.Children.Add(show);
         gotFocus.Children.Add(grow);
         //gotFocus.Children.Add(unfade);
         //gotFocus.Children.Add(maximize);
      }


      protected override void OnDetaching()
      {
         foreach (var trigger in _triggers)
         {
            AssociatedObject.Triggers.Remove(trigger);
         }
         base.OnDetaching();
      }



	  //protected override IEnumerable<MessageMapping> Handlers
	  //{
	  //   get
	  //   {
	  //      yield return new MessageMapping(NativeMethods.WindowMessage.GetMinMaxInfo, OnGetMinMaxInfo);
	  //   }
	  //}

      /// <summary>Handles the GetMinMaxInfo Window Message.
      /// </summary>
      /// <param name="wParam">The wParam.</param>
      /// <param name="lParam">The lParam.</param>
      /// <param name="handled">Whether or not this message has been handled ... (we don't change it)</param>
      /// <returns>IntPtr.Zero</returns>      
      private IntPtr OnGetMinMaxInfo(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         if (Enabled)
         {
            var minMaxInfo = (NativeMethods.MinMaxInfo)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MinMaxInfo));
            var handle = new WindowInteropHelper(AssociatedObject).Handle;
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MonitorDefault.ToNearest);
            NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMonitor);
            minMaxInfo.MaxPosition.x = mi.MonitorWorkingSpaceRect.Left;
            minMaxInfo.MaxPosition.y = mi.MonitorWorkingSpaceRect.Top;
            minMaxInfo.MaxSize.x = mi.MonitorWorkingSpaceRect.Width;
            minMaxInfo.MaxSize.y = (int)Math.Floor(Size);

            Marshal.StructureToPtr(minMaxInfo, lParam, true);
         }
         return IntPtr.Zero;
      }


      public static readonly DependencyProperty EnabledProperty =
         DependencyProperty.Register("Enabled", typeof(bool), typeof(QuakeMode), new UIPropertyMetadata(true));

      public bool Enabled
      {
         get { return (bool)GetValue(EnabledProperty); }
         set
         {
            SetValue(EnabledProperty, value);
            //AssociatedObject.WindowStyle = value ? WindowStyle.None : (AssociatedObject.AllowsTransparency ? WindowStyle.None : WindowStyle.ThreeDBorderWindow);
         }
      }

      public static readonly DependencyProperty DurationProperty =
         DependencyProperty.Register("Duration", typeof(int), typeof(QuakeMode), new UIPropertyMetadata(1));

      public int Duration
      {
         get { return (int)GetValue(DurationProperty); }
         set { SetValue(DurationProperty, value); }
      }


      public static readonly DependencyProperty SizeProperty =
          DependencyProperty.Register("Size", typeof(Double), typeof(QuakeMode), new UIPropertyMetadata(300.0));

      public Double Size
      {
         get { return (Double)GetValue(SizeProperty); }
         set { SetValue(SizeProperty, value); }
      }


      public static readonly DependencyProperty OpacityProperty =
         DependencyProperty.Register("Opacity", typeof(Double), typeof(QuakeMode), new UIPropertyMetadata(1.0));

      public Double Opacity
      {
         get { return (Double)GetValue(OpacityProperty); }
         set { SetValue(OpacityProperty, value); }
      }

      public enum Direction
      {
         Height, Width
      }

      public static readonly DependencyProperty DimensionProperty =
          DependencyProperty.Register("Dimension", typeof(Direction), typeof(QuakeMode), new UIPropertyMetadata(Direction.Height));

      public Direction Dimension
      {
         get { return (Direction)GetValue(DimensionProperty); }
         set { SetValue(DimensionProperty, value); }
      }

   }
}
