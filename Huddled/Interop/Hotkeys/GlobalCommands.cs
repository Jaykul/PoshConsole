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
        /// Default constructor for <see cref="WindowCommand"/> leaves the Window property unitialized.
        /// </summary>
        public WindowCommand() : base() { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCommand"/> class.
        /// </summary>
        /// <param name="window">The window.</param>
        public WindowCommand(Window window) { _window = window; }



        protected Window _window;

        /// <summary>
        /// Gets or sets the window that is the target of this command
        /// </summary>
        /// <value>The window.</value>
        public Window Window
        {
            get { return _window; }
            set { _window = value; }
        }


        #region ICommand Members

        /// <summary>
        /// Determines whether this instance can execute on specified window 
        /// (or the default window, if you pass in null).
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can execute on the specified window; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanExecute(object window);
        
        /// <summary>
        /// Occurs when changes occur which affect whether or not the command should execute.
        /// (Is never fired for WindowCommand).
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Executes the hotkey action on the specified window.
        /// </summary>
        /// <param name="window">The window.</param>
        public abstract void Execute(object window);

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
        /// A <see cref="WindowCommand"/> which activates the window
        /// </summary>
        public class ActivateCommand : WindowCommand
        {
            public ActivateCommand() : base() { }

            public override bool CanExecute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                return null != wnd && !wnd.IsFocused && wnd.IsLoaded;
            }

            public override void Execute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                if (null != wnd)
                {
                    if (!wnd.IsVisible)
                    {
                        wnd.Show();
                    }
                    // wnd.Focus();
                    wnd.Activate();
                }
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which closes the window
        /// </summary>
        public class CloseCommand : WindowCommand
        {
            public CloseCommand() : base() { }
            public override bool CanExecute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                return null != wnd && wnd.IsLoaded;
            }

            public override void Execute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                if (null != wnd)
                {
                    wnd.Close();
                }
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which hides the window
        /// </summary>
        public class HideCommand : WindowCommand
        {
            public HideCommand() : base() { }
            public override bool CanExecute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                return null != wnd && wnd.IsVisible;
            }

            public override void Execute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                if (null != wnd)
                {
                    wnd.Hide();
                }
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which unhides the window
        /// </summary>
        public class ShowCommand : WindowCommand
        {
            public ShowCommand() : base() { }

            public override bool CanExecute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                return null != wnd && !wnd.IsVisible && wnd.IsLoaded;
            }

            public override void Execute(object window)
            {
                Window wnd = (window as Window) ?? _window;
                if (null != wnd)
                {
                    wnd.Show();
                }
            }
        }
    }
}
