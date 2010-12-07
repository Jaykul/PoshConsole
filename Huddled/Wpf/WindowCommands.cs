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

   public static class WindowHelpers
   {

      public static void ToggleTopmost(this Window window, bool? topMost = null)
      {
         // Verify.IsNotNull(window, "window");
         window.Topmost = topMost ?? !window.Topmost;

         if (window.Topmost)
         {
            // window.Activate();
            window.Focus();
         }
      }
      public static void ToggleVisibility(this Window window, bool? visible = null)
      {
         bool forceHide = visible.HasValue && !visible.Value;
         // Verify.IsNotNull(window, "window");
         if (window.Visibility != Visibility.Visible && !forceHide)
         {
            window.Show();
         }
         else
         {
            window.Hide();
         }

         if (!window.IsActive && !forceHide)
         {
            window.Activate();
            window.Focus();
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
      /// An instance of a <see cref="WindowUICommand"/> which closes the target window
      /// </summary>
      public static WindowUICommand Close { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which maximizes the target window
      /// </summary>
      public static WindowUICommand Maximize { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which minimizes the target window
      /// </summary>
      public static WindowUICommand Minimize { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which restores the target window
      /// </summary>
      public static WindowUICommand Restore { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which activates the target window
      /// </summary>
      public static WindowUICommand Activate { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which shows the system menu for the window
      /// </summary>
      public static WindowUICommand ShowSystemMenu { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which hides the target Window
      /// </summary>
      public static WindowUICommand Hide { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which unhides the target Window
      /// </summary>
      public static WindowUICommand Show { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which toggles the visibility of the target Window
      /// </summary>
      public static WindowUICommand VisibilityToggle { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which sets the target Window to be always on top
      /// </summary>
      public static WindowUICommand TopmostOn { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which makes sure the target Window is not always on top
      /// </summary>
      public static WindowUICommand TopmostOff { get; private set; }
      /// <summary>
      /// An instance of a <see cref="WindowUICommand"/> which toggles the Topmost state of the target Window
      /// </summary>
      public static WindowUICommand TopmostToggle { get; private set; }

      static WindowCommands()
      {
         Close             = new WindowUICommand("Close"               ,"CloseWindow"      ,(s,e) => e.CanExecute = true, (s,e) => e.Window.Close());
         Maximize          = new WindowUICommand("Maximize"            ,"MaximizeWindow"   ,(s,e) => e.CanExecute = e.Window.WindowState != WindowState.Maximized, (s, e) => e.Window.WindowState = WindowState.Maximized);
         Minimize          = new WindowUICommand("Minimize"            ,"MinimizeWindow"   ,(s,e) => e.CanExecute = e.Window.WindowState != WindowState.Minimized, (s,e) => e.Window.WindowState = WindowState.Minimized);
         Restore           = new WindowUICommand("Restore"             ,"RestoreWindow"    ,(s,e) => e.CanExecute = true, (s, e) => e.Window.WindowState = e.Window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal);
         Activate          = new WindowUICommand("Activate"            ,"ActivateWindow"   ,(s,e) => e.CanExecute = !e.Window.IsActive, (s,e) => e.Window.Activate());
         TopmostOn         = new WindowUICommand("Set Topmost"         ,"OnTopmost"        ,(s,e) => e.CanExecute = !e.Window.Topmost, (s,e) => e.Window.ToggleTopmost(true));
         TopmostOff        = new WindowUICommand("Remove Topmost"      ,"OffTopmost"       ,(s,e) => e.CanExecute = e.Window.Topmost, (s,e) => e.Window.ToggleTopmost(false));
         TopmostToggle     = new WindowUICommand("Topmost"             ,"ToggleTopmost"    ,(s,e) => e.CanExecute = true, (s,e) => e.Window.ToggleTopmost());
         Show              = new WindowUICommand("Show"                ,"ShowWindow"       ,(s,e) => e.CanExecute = !e.Window.IsFocused, (s,e) => e.Window.ToggleVisibility(true));
         Hide              = new WindowUICommand("Hide"                ,"HideWindow"       ,(s,e) => e.CanExecute = e.Window.IsVisible, (s,e) => e.Window.ToggleVisibility(false));
         VisibilityToggle  = new WindowUICommand("Show"                ,"ToggleVisibility" ,(s,e) => e.CanExecute = true, (s, e) => e.Window.ToggleVisibility());
         ShowSystemMenu    = new WindowUICommand("Show the System Menu", "ShowSystemMenu"  ,(s,e) => e.CanExecute = true, (source, eventArgs) =>
         {
            var window = eventArgs.Window;
            if (eventArgs.Parameter is Point)
            {
               window.ShowSystemMenuAtPoint((Point)eventArgs.Parameter);
            }
            else if (source != null && !(source is Window) && source is UIElement)
            {
               window.ShowSystemMenuAtPoint((source as UIElement).PointToScreen(new Point(0, 0)));
            }
            else
            {
               window.ShowSystemMenuAtMouse();
            }
         });
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
            Close.Window = window;
            Maximize.Window = window;
            Minimize.Window = window;
            Restore.Window = window;
            Activate.Window = window;
            ShowSystemMenu.Window = window;
            TopmostOn.Window = window;
            TopmostOff.Window = window;
            TopmostToggle.Window = window;
            Show.Window = window;
            Hide.Window = window;
            VisibilityToggle.Window = window;
         }

         Close.Enabled = Maximize.Enabled = Minimize.Enabled = 
         Restore.Enabled = Activate.Enabled = ShowSystemMenu.Enabled =
         TopmostOn.Enabled = TopmostOff.Enabled = TopmostToggle.Enabled = 
         Show.Enabled = Hide.Enabled = VisibilityToggle.Enabled = value;
      }

      // Using a DependencyProperty as the backing store for Commands.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty EnableCommandsProperty = DependencyProperty.RegisterAttached(
         "EnableCommands", 
         typeof(bool), 
         typeof(WindowCommands), 
         new PropertyMetadata(false, EnableCommandsChanged));

      private static void EnableCommandsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
      {
         var window = obj as Window;
         if (window != null)
         {
            Close.Window = window;
            Maximize.Window = window;
            Minimize.Window = window;
            Restore.Window = window;
            Activate.Window = window;
            ShowSystemMenu.Window = window;
            TopmostOn.Window = window;
            TopmostOff.Window = window;
            TopmostToggle.Window = window;
            Show.Window = window;
            Hide.Window = window;
            VisibilityToggle.Window = window;
         }
      }

      #endregion

   }


   public class WindowUICommand : ICommand
   {
      public string Name { get; set; }

      public string Text { get; set; }

      public Window Window { get; set; }

      private bool _enabled;
      public bool Enabled
      {
         get { return _enabled; }
         set { 
            _enabled = value;
            var notify = CanExecuteChanged;
            if (notify != null)
            {
               notify(this, new EventArgs());
            }
         }
      }

      public WindowUICommand(string text, string name)
      {
         Text = text;
         Name = name;
         Enabled = true;
      }

      public WindowUICommand(string text, string name, CanExecuteHandler canExecute, ExecuteHandler execute) 
         : this(text, name)
      {
         OnCanExecute += canExecute;
         OnExecute += execute;
      }

      public WindowUICommand(string text, string name, Window window) 
         : this(text, name)
      {
         Window = window;
      }

      public WindowUICommand(string text, string name, Window window, CanExecuteHandler canExecute, ExecuteHandler execute) 
         : this(text, name, window)
      {
         OnCanExecute += canExecute;
         OnExecute += execute;
      }

      /// <summary>Delegate for the CanExecute event</summary>
      public delegate void CanExecuteHandler(object source, WindowCommand.WindowCanExecuteArgs e);
      /// <summary>Delegate for the Execute event</summary>
      public delegate void ExecuteHandler(object source, WindowCommand.WindowOnExecuteArgs e);

      /// <summary>
      /// Occurs when we're checking if we can execute this command.
      /// </summary>
      public event CanExecuteHandler OnCanExecute;
      /// <summary>
      /// Occurs when we're trying to execute this command.
      /// </summary>
      public event ExecuteHandler OnExecute;


      #region ICommand Members

      public bool CanExecute(object parameter)
      {
         if (!Enabled) return false;

         var args = new WindowCommand.WindowCanExecuteArgs((parameter as Window) ?? Window, parameter);

         if (args.Window != null)
         {
            var temp = OnCanExecute;
            if (temp != null)
            {
               temp(this, args);
            }
         }
         return args.CanExecute;
      }

      public event EventHandler CanExecuteChanged;

      public void Execute(object parameter)
      {
         if (!Enabled) return;

         var args = new WindowCommand.WindowOnExecuteArgs((parameter as Window) ?? Window, parameter);

         if (args.Window == null) return;

         var temp = OnExecute;
         if (temp != null)
         {
            temp(this, args);
         }
      }

      #endregion
   }

}
