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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Huddled.Interop;
using Huddled.Interop.Windows;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;

namespace Huddled.Wpf
{
   public class SnapToBehavior : NativeBehavior
   {

      /// <summary>Handles the WindowPositionChanging Window Message.
      /// </summary>
      /// <param name="wParam">The wParam.</param>
      /// <param name="lParam">The lParam.</param>
      /// <param name="handled">Whether or not this message has been handled ... (we don't change it)</param>
      /// <returns>IntPtr.Zero</returns>      
      private IntPtr OnPreviewPositionChange(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         bool updated = false;
         var windowPosition = (NativeMethods.WindowPosition)Marshal.PtrToStructure(lParam, typeof(NativeMethods.WindowPosition));
         //if (_removeMargin)
         //{
         //   windowPosition.RemoveBorder(AssociatedObject.Margin);
         //}

         bool top = false, bottom = false, right = false, left = false;

         if ((windowPosition.Flags & NativeMethods.WindowPositionFlags.NoMove) == 0)
         {
            // If we use the WPF SystemParameters, these should be "Logical" pixels
         	var source = PresentationSource.FromVisual(AssociatedObject);
			 // MUST use the position from the lParam, NOT the current position of the AssociatedObject
			Rect validArea = windowPosition.GetLocalWorkAreaRect().DPITransformFromWindow(source);

			var innerBorder = new Rect(validArea.Left + SnapDistance.Left,
                                       validArea.Top    + SnapDistance.Top,
                                       validArea.Width  - (SnapDistance.Left + SnapDistance.Right),
                                       validArea.Height - (SnapDistance.Top  + SnapDistance.Bottom));

			var outerBorder = new Rect(validArea.Left - SnapDistance.Left,
										validArea.Top - SnapDistance.Top,
										validArea.Width + (SnapDistance.Left + SnapDistance.Right),
										validArea.Height + (SnapDistance.Top + SnapDistance.Bottom));
            // Enforce bottom boundary
			bottom = windowPosition.Bottom > innerBorder.Bottom && windowPosition.Bottom <= outerBorder.Bottom;
			right = windowPosition.Right > innerBorder.Right && windowPosition.Right <= outerBorder.Right;
			top = windowPosition.Top < innerBorder.Y && windowPosition.Top >= outerBorder.Top;
			left = windowPosition.Left < innerBorder.Left && windowPosition.Left >= outerBorder.Left;

            if (bottom && top)
            {
               windowPosition.Top = (int)(validArea.Top);
               windowPosition.Height = (int)validArea.Height;
               if(left && right)
               {
                  VisualStateManager.GoToState(AssociatedObject, "Maximized", true);
				      windowPosition.Left = (int)(validArea.Left);
                  windowPosition.Width = (int) validArea.Width;
               } 
               else if (left)
               {
                  VisualStateManager.GoToState(AssociatedObject, "DockedFullLeft", true);
				  windowPosition.Left = (int)(validArea.Left);
               }
               else if (right)
               {
                  VisualStateManager.GoToState(AssociatedObject, "DockedFullRight", true);
				  windowPosition.Left = (int)(validArea.Right - windowPosition.Width);
               }
               else
               {
                  VisualStateManager.GoToState(AssociatedObject, "DockedFullHeight", true);
               }

            }
            else if (bottom)
            {
				windowPosition.Top = (int)(validArea.Bottom - windowPosition.Height + SnapMargin);
               if (left && right)
               {
                  VisualStateManager.GoToState(AssociatedObject, "DockedFullBottom", true);
                  windowPosition.Left = (int)(validArea.Left);
                  windowPosition.Width = (int)validArea.Width;
               }
               else if (left)
               {
                  //VisualStateManager.GoToState(AssociatedObject, "DockedBottomLeft", true);
				  windowPosition.Left = (int)(validArea.Left - SnapMargin);
               }
               else if (right)
               {
                  //VisualStateManager.GoToState(AssociatedObject, "DockedBottomRight", true);
                  windowPosition.Left = (int)(validArea.Right - windowPosition.Width + SnapMargin);
               }
               else
               {
                  //VisualStateManager.GoToState(AssociatedObject, "DockedBottom", true);
				   VisualStateManager.GoToState(AssociatedObject, "Normal", true);
               }
            }
            else if (top)
            {
				   windowPosition.Top = (int)(validArea.Top - SnapMargin);
               if (left && right)
               {
                  VisualStateManager.GoToState(AssociatedObject, "DockedFullTop", true);
                  windowPosition.Left = (int)validArea.Left;
                  windowPosition.Width = (int)validArea.Width;
                  windowPosition.Top = (int)(validArea.Top);
               }
               else if (left)
               {
                  //VisualStateManager.GoToState(AssociatedObject, "DockedTopLeft", true);
				      windowPosition.Left = (int)(validArea.Left - SnapMargin);
               }
               else if (right)
               {
                  //VisualStateManager.GoToState(AssociatedObject, "DockedTopRight", true);
                  windowPosition.Left = (int)(validArea.Right - windowPosition.Width + SnapMargin);
               }
               else
               {
                  //VisualStateManager.GoToState(AssociatedObject, "DockedTop", true);
				   VisualStateManager.GoToState(AssociatedObject, "Normal", true);
               }
            }
            else if (right)
            {
               //VisualStateManager.GoToState(AssociatedObject, "DockedRight", true);
               windowPosition.Left = (int)((validArea.Right - windowPosition.Width) + (double)SnapMargin);
            }
            else if (left)
            {
               //VisualStateManager.GoToState(AssociatedObject, "DockedLeft", true);
               windowPosition.Left = (int)(validArea.Left - SnapMargin);
            }
            else
            {
               VisualStateManager.GoToState(AssociatedObject, "Normal", true);
            }
         }
         if (left || top || right || bottom)
         {
            Marshal.StructureToPtr(windowPosition, lParam, true);
         }

         return IntPtr.Zero;
      }

      #region Additional Dependency Properties
      /// <summary>
      /// The DependencyProperty as the backing store for SnapDistance. <remarks>Just you can set it from XAML.</remarks>
      /// </summary>
      public static readonly DependencyProperty SnapDistanceProperty =
          DependencyProperty.Register("SnapDistance", typeof(Thickness), typeof(SnapToBehavior), new UIPropertyMetadata(new Thickness(20)));

      /// <summary>
      /// Gets or sets the snap distance.
      /// </summary>
      /// <value>The snap distance.</value>
      public Thickness SnapDistance
      {
         get { return (Thickness)GetValue(SnapDistanceProperty); }
         set { SetValue(SnapDistanceProperty, value); }
      }
      #endregion Additional Dependency Properties


	  #region Additional Dependency Properties
	  /// <summary>
	  /// The DependencyProperty as the backing store for SnapDistance. <remarks>Just you can set it from XAML.</remarks>
	  /// </summary>
	  public static readonly DependencyProperty SnapMarginProperty =
		  DependencyProperty.Register("SnapMargin", typeof(double), typeof(SnapToBehavior), new UIPropertyMetadata(0.0));

	  /// <summary>
	  /// Gets or sets the snap distance.
	  /// </summary>
	  /// <value>The snap distance.</value>
	  public double SnapMargin
	  {
		  get { return (double)GetValue(SnapMarginProperty); }
		  set { SetValue(SnapMarginProperty, value); }
	  }
	  #endregion Additional Dependency Properties


      protected override IEnumerable<MessageMapping> Handlers
      {
         get
         {
            yield return new MessageMapping(NativeMethods.WindowMessage.WindowPositionChanging, OnPreviewPositionChange);
         }
      }
   }
}
