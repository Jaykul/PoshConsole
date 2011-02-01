using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Huddled.Interop;

namespace Huddled.Wpf
{
   /// <summary>
   /// <para>A <see cref="RoutedUICommand"/> is RoutedUICommand which is designed to target Window elements.</para>
   /// </summary>
   public static class WindowHelpers
   {

      public static void ToggleTopmost(this Window window)
      {
         // Verify.IsNotNull(window, "window");
         window.Topmost = !window.Topmost;
         if(window.Topmost)
            window.Activate();
      }
      public static void ToggleVisibility(this Window window)
      {
         // Verify.IsNotNull(window, "window");
         if (window.Visibility != Visibility.Visible)
         {
            window.Show();
            window.Activate();
         }
         else if (!window.IsActive)
         {
            window.Activate();
         }
         else
         {
            window.Hide();
         }
      }

      //public static void CloseWindow(Window window)
      //{
      //   // Verify.IsNotNull(window, "window");
      //   _PostSystemCommand(window, SC.CLOSE);
      //}

      //public static void MaximizeWindow(Window window)
      //{
      //   // Verify.IsNotNull(window, "window");
      //   _PostSystemCommand(window, SC.MAXIMIZE);
      //}

      //public static void MinimizeWindow(Window window)
      //{
      //   // Verify.IsNotNull(window, "window");
      //   _PostSystemCommand(window, SC.MINIMIZE);
      //}

      //public static void RestoreWindow(Window window)
      //{
      //   // Verify.IsNotNull(window, "window");
      //   _PostSystemCommand(window, SC.RESTORE);
      //}


      /// <summary>
      /// Shows the system menu at the current mouse location
      /// </summary>
      /// <param name="window">The window.</param>
      public static void ShowSystemMenuAtMouse(this Window window)
      {
         NativeMethods.ApiPoint point;
         NativeMethods.GetCursorPos(out point);

         // Verify.IsNotNull(window, "window");
         ShowSystemMenuAtPhysicalPoint(window, point);
      }


      /// <summary>
      /// Display the system menu at a specified location.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <param name="screenLocation">The location to display the system menu, in logical screen coordinates.</param>
      public static void ShowSystemMenuAtPoint(this Window window, Point screenLocation)
      {
         // Verify.IsNotNull(window, "window");
         ShowSystemMenuAtPhysicalPoint(window, DpiHelper.LogicalPixelsToDevice(screenLocation));
      }

      /// <summary>
      /// Shows the system menu at physical point.
      /// </summary>
      /// <param name="window">The window.</param>
      /// <param name="physicalScreenLocation">The physical screen location.</param>
      internal static void ShowSystemMenuAtPhysicalPoint(this Window window, Point physicalScreenLocation)
      {
         const uint TPM_RETURNCMD = 0x0100;
         const uint TPM_LEFTBUTTON = 0x0;

         // Verify.IsNotNull(window, "window");
         IntPtr hwnd = new WindowInteropHelper(window).Handle;
         if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
         {
            return;
         }

         IntPtr hmenu = NativeMethods.GetSystemMenu(hwnd, false);

         uint cmd = NativeMethods.TrackPopupMenuEx(hmenu, TPM_LEFTBUTTON | TPM_RETURNCMD, (int)physicalScreenLocation.X, (int)physicalScreenLocation.Y, hwnd, IntPtr.Zero);
         if (0 != cmd)
         {
            NativeMethods.PostMessage(hwnd, NativeMethods.WindowMessage.SysCommand, new IntPtr(cmd), IntPtr.Zero);
         }
      }
   }



   public class WindowCommands
   {
      #region Commands
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which closes the target window
      /// </summary>
      public static RoutedUICommand Close { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which maximizes the target window
      /// </summary>
      public static RoutedUICommand Maximize { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which minimizes the target window
      /// </summary>
      public static RoutedUICommand Minimize { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which restores the target window
      /// </summary>
      public static RoutedUICommand Restore { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which activates the target window
      /// </summary>
      public static RoutedUICommand Activate { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which shows the system menu for the window
      /// </summary>
      public static RoutedUICommand ShowSystemMenu { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which hides the target Window
      /// </summary>
      public static RoutedUICommand Hide { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which unhides the target Window
      /// </summary>
      public static RoutedUICommand Show { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which toggles the visibility of the target Window
      /// </summary>
      public static RoutedUICommand VisibilityToggle { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which sets the target Window to be always on top
      /// </summary>
      public static RoutedUICommand TopmostOn { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which makes sure the target Window is not always on top
      /// </summary>
      public static RoutedUICommand TopmostOff { get; private set; }
      /// <summary>
      /// An instance of a <see cref="RoutedUICommand"/> which toggles the Topmost state of the target Window
      /// </summary>
      public static RoutedUICommand TopmostToggle { get; private set; }

      static WindowCommands()
      {
         Close             = new RoutedUICommand("Close"               ,"CloseWindow"       , typeof(RoutedUICommand));
         Maximize          = new RoutedUICommand("Maximize"            ,"MaximizeWindow"    , typeof(RoutedUICommand));
         Minimize          = new RoutedUICommand("Minimize"            ,"MinimizeWindow"    , typeof(RoutedUICommand));
         Restore           = new RoutedUICommand("Restore"             ,"RestoreWindow"     , typeof(RoutedUICommand));
         Activate          = new RoutedUICommand("Activate"            ,"ActivateWindow"    , typeof(RoutedUICommand));
         ShowSystemMenu    = new RoutedUICommand("Show the System Menu","ShowSystemMenu"    , typeof(RoutedUICommand));
         TopmostOn         = new RoutedUICommand("Set Topmost"         ,"OnTopmost"         , typeof(RoutedUICommand));
         TopmostOff        = new RoutedUICommand("Remove Topmost"      ,"OffTopmost"        , typeof(RoutedUICommand));
         TopmostToggle     = new RoutedUICommand("Topmost"             ,"ToggleTopmost"     , typeof(RoutedUICommand));
         Show              = new RoutedUICommand("Show"                ,"ShowWindow"        , typeof(RoutedUICommand));
         Hide              = new RoutedUICommand("Hide"                ,"HideWindow"        , typeof(RoutedUICommand));
         VisibilityToggle  = new RoutedUICommand("Show"                ,"ToggleVisibility"  , typeof(RoutedUICommand));
      }



      public static bool GetEnableCommands(DependencyObject obj)
      {
         return (bool)obj.GetValue(EnableCommandsProperty);
      }

      public static void SetEnableCommands(DependencyObject obj, bool value)
      {
         obj.SetValue(EnableCommandsProperty, value);
         var window = obj as Window;
         if (window != null)
         {
            window.CommandBindings.Add(new CommandBinding(Close, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Maximize, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Minimize, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Restore, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Activate, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(ShowSystemMenu, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(TopmostOn, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(TopmostOff, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(TopmostToggle, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Show, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Hide, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(VisibilityToggle, Execute, CanExecute));
         }
      }

      // Using a DependencyProperty as the backing store for Commands.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty EnableCommandsProperty =
          DependencyProperty.RegisterAttached("EnableCommands", typeof(bool), typeof(WindowCommands), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.NotDataBindable, EnableCommandsChanged));

      private static void EnableCommandsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
      {
         var window = obj as Window;
         if (window != null)
         {
            window.CommandBindings.Add(new CommandBinding(Close, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Maximize, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Minimize, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Restore, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Activate, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(ShowSystemMenu, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(TopmostOn, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(TopmostOff, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(TopmostToggle, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Show, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(Hide, Execute, CanExecute));
            window.CommandBindings.Add(new CommandBinding(VisibilityToggle, Execute, CanExecute));
         }
      }

      #endregion


      /// <summary>
      /// Determines whether this <see cref="T:System.Windows.Input.RoutedCommand"/> can execute in its current state.
      /// </summary>
      /// <param name="sender">The sender.</param>
      /// <param name="eventArgs">The <see cref="System.Windows.Input.CanExecuteRoutedEventArgs"/> instance containing the event data.</param>
      /// <returns>
      /// true if the command can execute on the current command target; otherwise, false.
      /// </returns>
      /// <exception cref="T:System.InvalidOperationException">
      /// 	<paramref name="target"/> is not a <see cref="T:System.Windows.UIElement"/> or <see cref="T:System.Windows.ContentElement"/>.</exception>
      public static void CanExecute(object sender, CanExecuteRoutedEventArgs eventArgs)
      {
         eventArgs.CanExecute = false;
         Window window = sender as Window;
         if (window == null)
         {
            var visual = sender as Visual;

            if (visual != null)
            {
               var control = PresentationSource.FromVisual(visual);
               if (control != null)
               {
                  window = control.RootVisual as Window;
               }
            }
         }
         if (window != null && window.IsInitialized)
         {
            switch (((RoutedCommand) eventArgs.Command).Name)
            {
               case "ShowSystemMenu":
                  eventArgs.CanExecute = true;
                  return;
               case "ToggleTopmost":
                  eventArgs.CanExecute = true;
                  return;
               case "ToggleVisibility":
                  eventArgs.CanExecute = true;
                  return;
               case "CloseWindow":
                  eventArgs.CanExecute = true;
                  return;
               case "MaximizeWindow":
                  eventArgs.CanExecute = window.WindowState != WindowState.Maximized;
                  return;
               case "MinimizeWindow":
                  eventArgs.CanExecute = window.WindowState != WindowState.Minimized;
                  return;
               case "RestoreWindow":
                  eventArgs.CanExecute = window.WindowState != WindowState.Normal;
                  return;
               case "ActivateWindow":
                  eventArgs.CanExecute = !window.IsActive;
                  return;
               case "OnTopmost":
                  eventArgs.CanExecute = !window.Topmost;
                  return;
               case "OffTopmost":
                  eventArgs.CanExecute = window.Topmost;
                  return;
               case "ShowWindow":
                  eventArgs.CanExecute = !window.IsVisible;
                  return;
               case "HideWindow":
                  eventArgs.CanExecute = window.IsVisible;
                  return;
               default:
                  eventArgs.CanExecute = false;
                  return;
            }
         }
      }

      /// <summary>
      /// Executes the <see cref="T:System.Windows.Input.RoutedCommand"/> on the current command target.
      /// </summary>
      /// <param name="target">Element at which to being looking for command handlers.</param>
      /// <param name="eventArgs">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
      /// <exception cref="T:System.InvalidOperationException">
      /// 	<paramref name="target"/> is not a <see cref="T:System.Windows.UIElement"/> or <see cref="T:System.Windows.ContentElement"/>.</exception>
      public static void Execute(Object target, ExecutedRoutedEventArgs eventArgs)
      {
         var window = target as Window;
         if (window == null || !window.IsInitialized)
         {
            return;
         }
         switch (((RoutedCommand)eventArgs.Command).Name)
         {
            case "ShowSystemMenu":
               {
                  if (eventArgs.Parameter is Point)
                  {
                     window.ShowSystemMenuAtPoint((Point)eventArgs.Parameter);
                  }
                  else if (target != null && !(target is Window) && target is UIElement)
                  {
                     window.ShowSystemMenuAtPoint((target as UIElement).PointToScreen(new Point(0, 0)));
                  }
                  else
                  {
                     window.ShowSystemMenuAtMouse();
                  }
               }
               break;
            case "ToggleTopmost":
               window.ToggleTopmost();
               break;
            case "ToggleVisibility":
               window.ToggleVisibility();
               break;
            case "CloseWindow":
               window.Close();
               break;
            case "MaximizeWindow":
               window.WindowState = WindowState.Maximized;
               break;
            case "MinimizeWindow":
               window.WindowState = WindowState.Minimized;
               break;
            case "RestoreWindow":
               window.WindowState = window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
               break;
            case "ActivateWindow":
               window.Activate();
               break;
            case "OnTopmost":
               window.Topmost = true;
               window.Activate();
               break;
            case "OffTopmost":
               window.Topmost = false;
               break;
            case "ShowWindow":
               window.Show();
               window.Activate();
               break;
            case "HideWindow":
               window.Hide();
               break;
         }
      }


   }
}
