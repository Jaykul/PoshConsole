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
using System.Runtime.InteropServices;
using System.Windows;
using Huddled.Interop;
using Huddled.Interop.Windows;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;
using System.Windows.Media.Animation;
using System.Windows.Data;


namespace Huddled.Wpf
{
   public class QuakeMode : NativeBehavior
   {
      public double _height;
      public List<EventTrigger> _triggers = new List<EventTrigger>();
      public override void AddTo(Window window)
      {
         //<Storyboard x:Key="OnLostFocus1">
         //   <DoubleAnimationUsingKeyFrames BeginTime="00:00:00" Storyboard.TargetName="{x:Null}" Storyboard.TargetProperty="(FrameworkElement.Height)">
         //      <SplineDoubleKeyFrame KeyTime="00:00:01" Value="0"/>
         //   </DoubleAnimationUsingKeyFrames>
         //   <DoubleAnimationUsingKeyFrames BeginTime="00:00:00" Storyboard.TargetName="{x:Null}" Storyboard.TargetProperty="(UIElement.Opacity)">
         //      <SplineDoubleKeyFrame KeyTime="00:00:01" Value="0"/>
         //   </DoubleAnimationUsingKeyFrames>
         //</Storyboard>
         if (Dimension == Direction.Height)
         {
            Size = window.Height;
         }
         else
         {
            Size = window.Width;
         }

         var LostFocus = new Storyboard();
         var GotFocus = new Storyboard();

         if (Dimension == Direction.Height)
         {
            window.Deactivated += new EventHandler((sender, ea) => { 
               Size = ((Window)sender).Height;
               if (Enabled)
               {
                  LostFocus.Begin();
               }
            });
         }
         else
         {
            window.Deactivated += new EventHandler((sender, ea) => { 
               Size = ((Window)sender).Width;
               if (Enabled)
               {
                  LostFocus.Begin();
               }
            });
         }
         window.Activated += new EventHandler((sender, ea) =>
         {
            if (Enabled)
            {
               GotFocus.Begin();
            } 
         });

         // Animate the opacity 
         var fade = new DoubleAnimation
         {
            To = 0.50,
            Duration = new Duration(TimeSpan.FromSeconds(Duration))
         };
         Storyboard.SetTarget(fade, window);
         Storyboard.SetTargetProperty(fade, new PropertyPath(System.Windows.Window.OpacityProperty));

         // Animate the height
         var shrink = new DoubleAnimation
         {
            To = 50.0,
            Duration = new Duration(TimeSpan.FromSeconds(Duration))
         };
         Storyboard.SetTarget(shrink, window);
         if (Dimension == Direction.Height)
         {
            Storyboard.SetTargetProperty(shrink, new PropertyPath(System.Windows.Window.HeightProperty));
         }
         else
         {
            Storyboard.SetTargetProperty(shrink, new PropertyPath(System.Windows.Window.WidthProperty));
         }

         // Animate the visibility
         var hide = new ObjectAnimationUsingKeyFrames();
         hide.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(Duration))));
         Storyboard.SetTarget(hide, window);
         Storyboard.SetTargetProperty(hide, new PropertyPath(System.Windows.Window.VisibilityProperty));

         LostFocus.Children.Add(shrink);
         LostFocus.Children.Add(fade);
         LostFocus.Children.Add(hide);

         // Animate the opacity 
         var unfade = new DoubleAnimation
         {
            To = 1.0,
            Duration = new Duration(TimeSpan.FromSeconds(Duration))
         };
         Storyboard.SetTarget(unfade, window);
         Storyboard.SetTargetProperty(unfade, new PropertyPath(System.Windows.Window.OpacityProperty));

         // Animate the height
         var grow = new DoubleAnimation
         {
            To = 250.0,
            Duration = new Duration(TimeSpan.FromSeconds(Duration))
         };
         var toBinding = new Binding("SizeProperty")
         {
            Source = this
         };
         BindingOperations.SetBinding(grow, DoubleAnimation.ToProperty, toBinding);

         Storyboard.SetTarget(grow, window);
         if (Dimension == Direction.Height)
         {
            Storyboard.SetTargetProperty(grow, new PropertyPath(System.Windows.Window.HeightProperty));
         }
         else
         {
            Storyboard.SetTargetProperty(grow, new PropertyPath(System.Windows.Window.WidthProperty));
         }

         // Animate the visibility
         var show = new ObjectAnimationUsingKeyFrames();
         show.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
         Storyboard.SetTarget(show, window);
         Storyboard.SetTargetProperty(show, new PropertyPath(System.Windows.Window.VisibilityProperty));

         GotFocus.Children.Add(show);
         GotFocus.Children.Add(grow);
         GotFocus.Children.Add(unfade);
         base.AddTo(window);
      }


      public override void RemoveFrom(Window window)
      {
         foreach (var trigger in _triggers)
         {
            window.Triggers.Remove(trigger);
         }
         base.RemoveFrom(window);
      }

      public override IEnumerable<MessageMapping> GetHandlers()
      {
         yield break;
      }

      public static readonly DependencyProperty EnabledProperty =
         DependencyProperty.Register("Enabled", typeof(bool), typeof(QuakeMode), new UIPropertyMetadata(true));

      public bool Enabled
      {
         get { return (bool)GetValue(EnabledProperty); }
         set { SetValue(EnabledProperty, value); }
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
