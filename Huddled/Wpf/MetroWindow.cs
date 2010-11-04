using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Huddled.Interop;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.NativeMethods.WindowMessage, Huddled.Interop.NativeMethods.MessageHandler>;

namespace Huddled.Wpf
{
   public class MetroWindow : NativeBehavior
   {

      /// <summary>Template for chromeless window.</summary>
      private ControlTemplate _template;

      
      #region ResizeBorder Dependency Property

      // Not exposing a public setter for this property as it would have surprising results to the end-user.
      private static readonly DependencyPropertyKey ResizeBorderPropertyKey = DependencyProperty.RegisterReadOnly(
          "ResizeBorder",
          typeof(Thickness),
          typeof(MetroWindow),
          new PropertyMetadata(
              new Thickness(
                  SystemParameters.ResizeFrameVerticalBorderWidth,
                  SystemParameters.ResizeFrameHorizontalBorderHeight,
                  SystemParameters.ResizeFrameVerticalBorderWidth,
                  SystemParameters.ResizeFrameHorizontalBorderHeight)));

      public static readonly DependencyProperty ResizeBorderProperty = ResizeBorderPropertyKey.DependencyProperty;

      /// <summary>Get the bounds of the resize grips on the Window.</summary>
      public Thickness ResizeBorder
      {
         get { return (Thickness)GetValue(ResizeBorderProperty); }
         // private set { SetValue(ResizeBorderPropertyKey, value); }
      }
      #endregion ResizeBorder Dependency Property

      #region CaptionHeight Dependency Property
      public static readonly DependencyProperty CaptionHeightProperty = DependencyProperty.Register(
          "CaptionHeight",
          typeof(double),
          typeof(MetroWindow),
          new PropertyMetadata(SystemParameters.CaptionHeight));

      /// <summary>The extent of the top of the window to treat as the caption.</summary>
      public double CaptionHeight
      {
         get { return (double)GetValue(CaptionHeightProperty); }
         set { SetValue(CaptionHeightProperty, value); }
      }
      #endregion CaptionHeight Dependency Property

      #region ButtonSize Dependency Property
      public static readonly DependencyProperty ButtonSizeProperty = DependencyProperty.Register(
          "ButtonSize",
          typeof(double),
          typeof(MetroWindow),
          new PropertyMetadata(SystemParameters.CaptionHeight));

      /// <summary>The extent of the top of the window to treat as the caption.</summary>
      public bool ButtonSize
      {
         get { return (bool)GetValue(ButtonSizeProperty); }
         set { SetValue(ButtonSizeProperty, value); }
      }
      #endregion ButtonSize Dependency Property

      #region HitTestable Attached Dependency Property
      public static readonly DependencyProperty HitTestableProperty = DependencyProperty.RegisterAttached(
          "HitTestable", typeof(bool), typeof(MetroWindow), new PropertyMetadata(false));

      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      public static bool GetHitTestable(UIElement element)
      {
         if (element == null) { throw new ArgumentNullException("element"); }
         return (bool)element.GetValue(HitTestableProperty);
      }

      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      public static void SetHitTestable(UIElement element, bool hitTestVisible)
      {
         if (element == null) { throw new ArgumentNullException("element"); }
         element.SetValue(HitTestableProperty, hitTestVisible);
      }
      #endregion HitTestable Attached Dependency Property



      protected override void OnAttached()
      {
         base.OnAttached();
         var resourceLocater = new Uri("/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/Wpf/MetroWindowTemplate.xaml", UriKind.RelativeOrAbsolute);
         _template = (ControlTemplate)Application.LoadComponent(resourceLocater);

         AssociatedObject.WindowStyle = WindowStyle.None;
         AssociatedObject.AllowsTransparency = true;

         AssociatedObject.Template = _template;
         AssociatedObject.StateChanged += OnWindowStateChanged;
         OnWindowStateChanged(AssociatedObject,null);
      }

      protected override void OnWindowSourceInitialized()
      {
         ((Button)AssociatedObject.Template.FindName("MinimizeButton", AssociatedObject)).Click += OnMinimizeClick;
         ((Button)AssociatedObject.Template.FindName("MaximizeButton", AssociatedObject)).Click += OnMaximizeClick;
         ((Button)AssociatedObject.Template.FindName("CloseButton", AssociatedObject)).Click += OnCloseClick;

         InitializeDockedViewStates();
      }

      private void InitializeDockedViewStates()
      {
         var originalMargin = AssociatedObject.BorderThickness.Union(AssociatedObject.Margin);
         ((Grid)AssociatedObject.Template.FindName("ContentGrid", AssociatedObject)).Margin = originalMargin;
         // ((ThicknessAnimation)AssociatedObject.Template.FindName("NormalThickness", AssociatedObject)).To = originalMargin;

         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedFullHeightThickness", AssociatedObject)).To =
            originalMargin.Clone(top:0, bottom: 0);
            
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedFullLeftThickness", AssociatedObject)).To =
            originalMargin.Clone(left:0, top:0, bottom: 0);
            
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedFullRightThickness", AssociatedObject)).To =
            originalMargin.Clone(top: 0, right: 0, bottom: 0);

         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedFullTopThickness", AssociatedObject)).To =
            originalMargin.Clone(left: 0, right: 0, bottom: 0);

         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedFullBottomThickness", AssociatedObject)).To =
            originalMargin.Clone(left: 0, top: 0, right: 0);

         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedLeftThickness", AssociatedObject)).To =
            originalMargin.Clone(left: 0);
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedRightThickness", AssociatedObject)).To =
            originalMargin.Clone(right: 0);
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedTopThickness", AssociatedObject)).To =
            originalMargin.Clone(top: 0);
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedBottomThickness", AssociatedObject)).To =
            originalMargin.Clone(bottom: 0);
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedTopLeftThickness", AssociatedObject)).To =
            originalMargin.Clone(left: 0, top: 0);
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedTopRightThickness", AssociatedObject)).To =
            originalMargin.Clone(top:0, right: 0);
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedBottomLeftThickness", AssociatedObject)).To =
            originalMargin.Clone(left: 0, bottom: 0);
         ((ThicknessAnimation)AssociatedObject.Template.FindName("DockedBottomRightThickness", AssociatedObject)).To =
            originalMargin.Clone(bottom:0, right: 0);
      }


      void OnWindowStateChanged(object sender, EventArgs e)
      {
         var window = (Window)sender;
         switch (window.WindowState)
         {
            case WindowState.Normal:
               VisualStateManager.GoToState(window, "Normal", true);
               break;
            case WindowState.Minimized:
               VisualStateManager.GoToState(window, "Minimized", true);
               break;
            case WindowState.Maximized:
               VisualStateManager.GoToState(window, "Maximized", true);
               break;
         }
      }


      void OnMinimizeClick(object sender, RoutedEventArgs e)
      {
         AssociatedObject.WindowState = WindowState.Minimized;
      }

      void OnMaximizeClick(object sender, RoutedEventArgs e)
      {
         if(AssociatedObject.WindowState == WindowState.Maximized)
         {
            AssociatedObject.WindowState = WindowState.Normal;
            ((Button) sender).ToolTip = "Maximize";
         }
         else
         {
            AssociatedObject.WindowState = WindowState.Maximized;
            ((Button)sender).ToolTip = "Restore";
         }
      }

      void OnCloseClick(object sender, RoutedEventArgs e)
      {
         AssociatedObject.Close();
      }



      /// <summary>
      /// Gets the collection of active handlers.
      /// </summary>
      /// <value>
      /// A List of the mappings from <see cref="NativeMethods.WindowMessage"/>s
      /// to <see cref="NativeMethods.MessageHandler"/> delegates.
      /// </value>
      protected override IEnumerable<MessageMapping> Handlers
      {
         get
         {
            yield return new MessageMapping(NativeMethods.WindowMessage.GetMinMaxInfo, GetMinMaxInfo);
            yield return new MessageMapping(NativeMethods.WindowMessage.HitTestNonClientArea, _HandleNCHitTest);
            yield return new MessageMapping(NativeMethods.WindowMessage.ActivateNonClientArea, _HandleNCActivate);
            yield return new MessageMapping(NativeMethods.WindowMessage.CalculateNonClientSize, _HandleNCCalcSize);
            yield return new MessageMapping(NativeMethods.WindowMessage.NonClientRightButtonUp, _HandleNCRButtonUp);
         }
      }

      private IntPtr GetMinMaxInfo(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         var mmi = (NativeMethods.MinMaxInfo)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MinMaxInfo));

         // Adjust the maximized size and position to fit the work area of the correct monitor
         IntPtr hMon = NativeMethods.MonitorFromWindow(WindowHandle, NativeMethods.MonitorDefault.ToNearest);

         if (hMon != IntPtr.Zero)
         {
            //Rect r = Target.GetWindowRect();
            //var left = (int)r.Left;
            //var top = (int)r.Top;

            NativeMethods.MonitorInfo mi = NativeMethods.GetMonitorInfo(hMon);
            NativeMethods.ApiRect rcWorkArea = mi.MonitorWorkingSpaceRect;
            NativeMethods.ApiRect rcMonitor = mi.MonitorRect;

            // The location of maximized window takes into account the border that Windows was
            // going to remove, so we also need to consider it.
            //rcMax.Offset(-left, -top);

            mmi.MaxPosition.x = Math.Abs(rcWorkArea.Left - rcMonitor.Left);
            mmi.MaxPosition.y = Math.Abs(rcWorkArea.Top - rcMonitor.Top);
            mmi.MaxSize.x     = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
            mmi.MaxSize.y     = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
         }

         Marshal.StructureToPtr(mmi, lParam, true);
         // handled = true;
         return IntPtr.Zero;
      }



      /// <summary>Display the system menu at a specified location.</summary>
      /// <param name="screenLocation">The location to display the system menu, in logical screen coordinates.</param>
      public void ShowSystemMenu(Point screenLocation)
      {
         Point physicalScreenLocation = DpiHelper.LogicalPixelsToDevice(screenLocation);
         _ShowSystemMenu(physicalScreenLocation);
      }

      ///// <summary>
      ///// Generic DP callback.
      ///// Most dependency properties affect the Window in a way that requires it to be repainted for the new property to visibly take effect.
      ///// </summary>
      ///// <param name="d">The CustomChrome object</param>
      ///// <param name="e">Old and New values are compared for equality to short-circuit the redraw.</param>
      //private static void _OnPropertyChangedThatRequiresRepaint(DependencyObject d, DependencyPropertyChangedEventArgs e)
      //{
      //   var c = d as MetroWindow;
      //   Debug.Assert(null != c);

      //   //if (e.OldValue != e.NewValue)
      //   //{
      //   //   c._UpdateFrameState(true);
      //   //}
      //}

      ///// <summary>
      ///// Generic DP coersion.  There are several properties of type Double that only can't be negative.
      ///// </summary>
      ///// <param name="d">The CustomChrome object</param>
      ///// <param name="value">The double that shouldn't be negative.</param>
      //private static object _CoerceNonNegativeDouble(DependencyObject d, object value)
      //{
      //   if ((double)value < 0)
      //   {
      //      throw new ArgumentException("The property cannot be set to a negative value.");
      //   }

      //   return value;
      //}


      private IntPtr _HandleNCActivate(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         // Despite MSDN's documentation of lParam not being used,
         // calling DefWindowProc with lParam set to -1 causes Windows not to draw over the caption.

         // Directly call DefWindowProc with a custom parameter
         // which bypasses any other handling of the message.
         IntPtr lRet = NativeMethods.DefWindowProc(WindowHandle, NativeMethods.WindowMessage.ActivateNonClientArea, wParam, new IntPtr(-1));
         handled = true;
         return lRet;
      }

      /// <summary>
      /// Matrix of the HT values to return when responding to NC window messages.
      /// </summary>
      [SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
      private static readonly NativeMethods.HT[,] _HitTestBorders = new[,]
        {
            { NativeMethods.HT.TOPLEFT,    NativeMethods.HT.TOP,     NativeMethods.HT.TOPRIGHT    },
            { NativeMethods.HT.LEFT,       NativeMethods.HT.NOWHERE, NativeMethods.HT.RIGHT       },
            { NativeMethods.HT.BOTTOMLEFT, NativeMethods.HT.BOTTOM,  NativeMethods.HT.BOTTOMRIGHT },
        };
      
      private IntPtr _HandleNCCalcSize(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         int result = 0;
         // lParam is an [in, out] that can be either a ApiRect* (wParam == 0) or an CalculateNonClientSizeParameter*.
         // Since the first field of CalculateNonClientSizeParameter is a ApiRect and is the only field we care about
         // we can unconditionally treat it as a ApiRect.

         // When the window procedure receives the WM_NCCALCSIZE message, the first rectangle contains
         // the new coordinates of a window that has been moved or resized, that is, it is the proposed 
         // new window coordinates. The second contains the coordinates of the window before it was moved 
         // or resized. The third contains the coordinates of the window's client area before the window 
         // was moved or resized. If the window is a child window, the coordinates are relative to the 
         // client area of the parent window. If the window is a top-level window, the coordinates are 
         // relative to the screen origin.

         // When the window procedure returns, the first rectangle contains the coordinates of the new 
         // client rectangle resulting from the move or resize. The second rectangle contains the valid
         // destination rectangle, and the third rectangle contains the valid source rectangle. The last
         // two rectangles are used in conjunction with the return value of the WM_NCCALCSIZE message to
         // determine the area of the window to be preserved. 
         //if (wParam.ToInt32() == 1)
         //{
         //   CalculateNonClientSizeParameter ncParam = (CalculateNonClientSizeParameter)Marshal.PtrToStructure(lParam, typeof(CalculateNonClientSizeParameter));

         //   ApiRect r = new ApiRect()
         //               {
         //                  top = ncParam.r3.top + (ncParam.r1.top - ncParam.r2.top),
         //                  left = ncParam.r3.left + (ncParam.r1.left - ncParam.r2.left),
         //                  bottom = ncParam.r3.bottom + (ncParam.r1.bottom - ncParam.r2.bottom),
         //                  right = ncParam.r3.right + (ncParam.r1.right - ncParam.r2.right),
         //               };
         //   ncParam.r3 = ncParam.r2;
         //   ncParam.r2 = ncParam.r1;
         //   //ncParam.r1 = r;
         //   result = (int)CalculateNonClientSizeResults.ValidRects;
         //   Marshal.StructureToPtr(ncParam, lParam, true);
         //}
         //else
         //{
         //   result = 0;
         //}
         handled = true;
         return new IntPtr(result);
      }


      private IntPtr _HandleNCHitTest(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         IntPtr lRet = IntPtr.Zero;
         handled = false;

         // Handle letting the system know if we consider the mouse to be in our effective non-client area.
         // If DWM already handled this by way of DwmDefWindowProc, then respect their call.
         if (IntPtr.Zero == lRet)
         {
            var mousePosScreen = new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
            Rect windowPosition = AssociatedObject.GetWindowRect();

            NativeMethods.HT ht = _HitTestNca(
                DpiHelper.DeviceRectToLogical(windowPosition),
                DpiHelper.DevicePixelsToLogical(mousePosScreen));

            // Don't blindly respect HTCAPTION.
            // We want UIElements in the caption area to be actionable so run through a hittest first.
            if (ht == NativeMethods.HT.CAPTION)
            {
               Point mousePosWindow = mousePosScreen;
               mousePosWindow.Offset(-windowPosition.X, -windowPosition.Y);
               mousePosWindow = DpiHelper.DevicePixelsToLogical(mousePosWindow);
               if (_HitTestUIElements(mousePosWindow))
               {
                  ht = NativeMethods.HT.NOWHERE;
               }
            }
            handled = NativeMethods.HT.NOWHERE != ht;
            lRet = new IntPtr((int)ht);
         }
         return lRet;
      }


      private NativeMethods.HT _HitTestNca(Rect windowPosition, Point mousePosition)
      {
         // Determine if hit test is for resizing, default middle (1,1).
         int uRow = 1;
         int uCol = 1;
         bool onResizeBorder = false;

         // Determine if the point is at the top or bottom of the window.
         if (mousePosition.Y >= windowPosition.Top + AssociatedObject.Margin.Top &&
             mousePosition.Y < windowPosition.Top + ResizeBorder.Top + CaptionHeight + AssociatedObject.Margin.Top &&
             // and within the width of the window
             mousePosition.X > windowPosition.Left + AssociatedObject.Margin.Left && 
             mousePosition.X < windowPosition.Right - AssociatedObject.Margin.Right)
         {
            onResizeBorder = (mousePosition.Y < (windowPosition.Top + ResizeBorder.Top + AssociatedObject.Margin.Top));
            //if (!onResizeBorder && (_CaptionElement != null))
            //{
            //   Trace.WriteLine(String.Format("[{0},{1}]", mousePosition.X, mousePosition.Y));
            //   return HT.NOWHERE;
            //}
            uRow = 0; // top (caption or resize border)
         }
         else if (mousePosition.Y < windowPosition.Bottom - AssociatedObject.Margin.Bottom &&
                  mousePosition.Y >= windowPosition.Bottom - (int)ResizeBorder.Bottom - AssociatedObject.Margin.Bottom &&
                  // and within the width of the window
                  mousePosition.X > windowPosition.Left + AssociatedObject.Margin.Left &&
                  mousePosition.X < windowPosition.Right - AssociatedObject.Margin.Right)
         {
            uRow = 2; // bottom
         }

         // Determine if the point is at the left or right of the window.
         if (mousePosition.X >= windowPosition.Left + AssociatedObject.Margin.Left &&
             mousePosition.X < windowPosition.Left + (int)ResizeBorder.Left + AssociatedObject.Margin.Left &&
             // and within the height of the window
             mousePosition.Y > windowPosition.Top + AssociatedObject.Margin.Top &&
             mousePosition.Y < windowPosition.Bottom - AssociatedObject.Margin.Bottom)
         {
            uCol = 0; // left side
         }
         else if (mousePosition.X < windowPosition.Right - AssociatedObject.Margin.Right &&
                  mousePosition.X >= windowPosition.Right - ResizeBorder.Right - AssociatedObject.Margin.Right &&
                  // and within the height of the window
                  mousePosition.Y > windowPosition.Top + AssociatedObject.Margin.Top &&
                  mousePosition.Y < windowPosition.Bottom - AssociatedObject.Margin.Bottom)
         {
            uCol = 2; // right side
         }

         NativeMethods.HT ht = _HitTestBorders[uRow, uCol];
         if (ht == NativeMethods.HT.TOP && !onResizeBorder)
         {
            ht = NativeMethods.HT.CAPTION;
         }

         return ht;
      }


      private bool _HitTestUIElements(Point mousePosWindow)
      {
         bool ret = false;
         VisualTreeHelper.HitTest(
             AssociatedObject,
             target =>
             {
                UIElement uie = target as UIElement;
                if (null != uie && MetroWindow.GetHitTestable(uie))
                {
                   ret = true;
                   return HitTestFilterBehavior.Stop;
                }
                return HitTestFilterBehavior.Continue;
             },
             result => HitTestResultBehavior.Stop,
             new PointHitTestParameters(mousePosWindow));

         return ret;
      }

      private IntPtr _HandleNCRButtonUp(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         // Emulate the system behavior of clicking the right mouse button over the caption area
         // to bring up the system menu.
         if (NativeMethods.HT.CAPTION == (NativeMethods.HT)wParam.ToInt32())
         {
            _ShowSystemMenu(new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam)));
         }
         handled = false;
         return IntPtr.Zero;
      }

      private void _ShowSystemMenu(Point physicalScreenLocation)
      {
         const uint TPM_RETURNCMD = 0x0100;
         const uint TPM_LEFTBUTTON = 0x0;

         IntPtr hmenu = NativeMethods.GetSystemMenu(WindowHandle, false);

         uint cmd = NativeMethods.TrackPopupMenuEx(hmenu, TPM_LEFTBUTTON | TPM_RETURNCMD, (int) physicalScreenLocation.X,
                                                   (int) physicalScreenLocation.Y, WindowHandle, IntPtr.Zero);
         if (0 != cmd)
         {
            NativeMethods.PostMessage(WindowHandle, NativeMethods.WindowMessage.SysCommand, new IntPtr(cmd), IntPtr.Zero);
         }
      }
   }
}
