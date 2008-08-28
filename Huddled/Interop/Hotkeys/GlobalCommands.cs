using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows;

namespace Huddled.Interop.Hotkeys
{
    /// <summary>
    /// <para>A <see cref="WindowCommand"/> is a command which is <em>not</em> routed,
    /// instead, they target the window directly.</para>
    /// <para>Because they are not routed, they don't have a "source", so we have to either set the window, 
    /// or pass the window in as an argument to the Execute command. The HotkeyManager does extra magic to 
    /// set the Window property, so you should inherit from <see cref="WindowCommand"/> if you want to create     
    /// additional global hotkey commands that will actually work when the window is not focused.</para>
    /// <remarks>RoutedCommands can't be used as the target for a global hotkey command 
    /// because they always (CanExecute == False) if the window isn't active.</remarks>
    /// </summary>
    public abstract class WindowCommand : ICommand
    {

        /// <summary>
        /// EventArgs class for the Execute events 
        /// </summary>
        public class WindowCommandArgs : EventArgs {

            public WindowCommandArgs( Window window, object parameter ){
                Window = window;
                Parameter = parameter;
            }
            public Window Window;
            public object Parameter;
        }

        public class WindowCanExecuteArgs :WindowCommandArgs{
            public WindowCanExecuteArgs(Window window, object parameter) : base(window,parameter){}
            public bool CanExecute = false;
        }
        public class WindowOnExecuteArgs :WindowCommandArgs{
            public WindowOnExecuteArgs(Window window, object parameter) : base(window, parameter) { }
            public bool Handled = false;
        }


        /// <summary>Default constructor for <see cref="WindowCommand"/> leaves the Window property unitialized.
        /// </summary>
        public WindowCommand() : base() { }
        
        /// <summary>Initializes a new instance of the <see cref="WindowCommand"/> class.
        /// </summary>
        /// <param name="window">The window.</param>
        public WindowCommand(Window window) { _window = window; }

        private Window _window;

        /// <summary>Gets or sets the window that is the target of this command
        /// </summary>
        /// <value>The window.</value>
        
        public Window Window
        {
            get { return _window; }
            set { _window = value; }
        }

        #region ICommand Members
        /// <summary>Determines whether this instance can execute on specified window 
        /// (or the default window, if you pass in null).
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can execute on the specified window; otherwise, <c>false</c>.
        /// </returns>
        protected abstract void IfNoHandlerOnCanExecute(object source, WindowCanExecuteArgs e);
        /// <summary>Executes the hotkey action on the specified window.
        /// </summary>
        /// <param name="window">The window.</param>
        protected abstract void IfNoHandlerOnExecute(object source, WindowOnExecuteArgs e);

        public delegate void CanExecuteHandler(object source, WindowCanExecuteArgs e);
        public delegate void ExecuteHandler(object source, WindowOnExecuteArgs e);

        public event CanExecuteHandler OnCanExecute;
        public event ExecuteHandler OnExecute;

        public bool CanExecute(object parameter)
        {
            WindowCanExecuteArgs args = new WindowCanExecuteArgs((parameter as Window) ?? _window, parameter);

            if (args.Window != null)
            {
                CanExecuteHandler temp = OnCanExecute;
                if (temp != null)
                {
                    temp(this, args);
                }
                else
                {
                    IfNoHandlerOnCanExecute(this, args);
                }
            }
            return args.CanExecute;
        }

        public void Execute(object parameter)
        {
            WindowOnExecuteArgs args = new WindowOnExecuteArgs((parameter as Window) ?? _window, parameter);

            if (args.Window != null)
            {
                ExecuteHandler temp = OnExecute;
                if (temp != null)
                {
                    temp(this, args);
                }
                else
                {
                    IfNoHandlerOnExecute(this, args);
                }
            }
        }

        
        /// <summary>Occurs when changes occur which affect whether or not the command should execute.
        /// (Is never fired for WindowCommand).
        /// </summary>
        public event EventHandler CanExecuteChanged;

        #endregion
    }

    /// <summary>
    /// A collection of <see cref="WindowCommand"/>
    /// </summary>
    public class GlobalCommands
    {
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which activates the window
        /// </summary>
        public static ActivateCommand ActivateWindow = new ActivateCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which closes the window
        /// </summary>
        public static CloseCommand CloseWindow = new CloseCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which hides the window
        /// </summary>
        public static HideCommand HideWindow = new HideCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which unhides the window
        /// </summary>
        public static ShowCommand ShowWindow = new ShowCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which toggles the visibility of the window
        /// </summary>
        public static ToggleCommand ToggleWindow = new ToggleCommand();


        /// <summary>
        /// A <see cref="WindowCommand"/> which activates the window
        /// </summary>
        public class ActivateCommand : WindowCommand
        {
            public ActivateCommand() : base() 
            {
                //CanExecute += new CanExecuteHandler(MyCanExecute);
                //Execute += new ExecuteHandler(MyExecute);
            }

            protected override void IfNoHandlerOnCanExecute(object source, WindowCanExecuteArgs e)
            {
                //Window wnd = (window as Window) ?? _window;
                e.CanExecute = e.Window.IsLoaded;
            }

            protected override void IfNoHandlerOnExecute(object source, WindowOnExecuteArgs e)
            {
                if (!e.Window.IsVisible)
                {
                    e.Window.Show();
                }
                // wnd.Focus();
                e.Window.Activate();
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which closes the window
        /// </summary>
        public class CloseCommand : WindowCommand
        {
            public CloseCommand() : base() { }
            protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
            {
                //Window wnd = (window as Window) ?? _window;
                e.CanExecute = e.Window.IsInitialized;
            }

            protected override void IfNoHandlerOnExecute(object window, WindowOnExecuteArgs e)
            {
                e.Window.Close();
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which hides the window
        /// </summary>
        public class HideCommand : WindowCommand
        {
            public HideCommand() : base() { }
            protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
            {
                e.CanExecute = e.Window.IsVisible;
            }

            protected override void IfNoHandlerOnExecute(object window, WindowOnExecuteArgs e)
            {
                e.Window.Hide();
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which unhides the window
        /// </summary>
        public class ShowCommand : WindowCommand
        {
            public ShowCommand() : base() { }

            protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
            {
                e.CanExecute = !e.Window.IsVisible && e.Window.IsLoaded;
            }

            protected override void IfNoHandlerOnExecute(object window, WindowOnExecuteArgs e)
            {
                e.Window.Show();
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which toggles the visibility of the window
        /// </summary>
        public class ToggleCommand : WindowCommand
        {
            public ToggleCommand() : base() { }

            protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
            {
                e.CanExecute = e.Window.IsLoaded;
            }

            protected override void IfNoHandlerOnExecute(object window, WindowOnExecuteArgs e)
            {
                if (!e.Window.IsVisible)
                {
                    e.Window.Show();
                    e.Window.Activate();
                }
                else if (!e.Window.IsActive)
                {
                    e.Window.Activate();
                } 
                else 
                {
                    e.Window.Hide();
                }
            }
        }

    }
}
