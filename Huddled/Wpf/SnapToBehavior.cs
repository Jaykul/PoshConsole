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

namespace Huddled.Wpf
{
   public class SnapToBehavior : NativeBehavior
   {
      Window _target;
      public override void AddTo(Window window)
      {
         _target = window;
         base.AddTo(window);
      }

      /// <summary>
      /// Gets the <see cref="MessageMapping"/>s for this behavior:
      /// A single mapping of a handler for WM_WINDOWPOSCHANGING.
      /// </summary>
      /// <returns>A collection of <see cref="MessageMapping"/> objects.</returns>
      public override IEnumerable<MessageMapping> GetHandlers()
      {
         yield return new MessageMapping(NativeMethods.WindowMessage.WindowPositionChanging, OnPreviewPositionChange);          
      }
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

         if ((windowPosition.Flags & NativeMethods.WindowPositionFlags.NoMove) == 0)
         {
            // If we use the WPF SystemParameters, these should be "Logical" pixels
            Rect validArea = _target.GetLocalWorkArea();

            Rect snapToBorder = new Rect( validArea.Left   + SnapDistance.Left,
                                          validArea.Top    + SnapDistance.Top,
                                          validArea.Width  - (SnapDistance.Left + SnapDistance.Right),
                                          validArea.Height - (SnapDistance.Top  + SnapDistance.Bottom));

            // Enforce left boundary
            if (windowPosition.Left < snapToBorder.Left && windowPosition.Left >= validArea.Left)
            {
               windowPosition.Left = (int)validArea.Left;
               updated = true;
            }

            // Enforce top boundary
            if (windowPosition.Top < snapToBorder.Y && windowPosition.Top >= validArea.Top)
            {
               windowPosition.Top = (int)validArea.Top;
               updated = true;
            }

            // Enforce right boundary
            if (windowPosition.Right > snapToBorder.Right && windowPosition.Right <= validArea.Right)
            {
               windowPosition.Left = (int)(validArea.Right - windowPosition.Width);
               updated = true;
            }

            // Enforce bottom boundary
            if (windowPosition.Bottom > snapToBorder.Bottom && windowPosition.Bottom <= validArea.Bottom)
            {
               windowPosition.Top = (int)(validArea.Bottom - windowPosition.Height);
               updated = true;
            }

         }
         if (updated)
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
   }
}
