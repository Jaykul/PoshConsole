// Copyright (c) 2008 Joel Bennett

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
using System.Windows;
using System.Windows.Input;

namespace Huddled.Wpf
{
    /// <summary>
    /// <para>A <see cref="WindowCommand"/> is a command which is <em>not</em> routed,
    /// instead, they target the Window directly.</para>
    /// <para>Because they are not routed, they don't have a "source", so we have to either set the Window, 
    /// or pass the Window in as an argument to the Execute command. The HotkeyManager does extra magic to 
    /// set the Window property, so you should inherit from <see cref="WindowCommand"/> if you want to create     
    /// additional global hotkey commands that will actually work when the Window is not focused.</para>
    /// <remarks>RoutedCommands can't be used as the target for a global hotkey command 
    /// because they always (CanExecute == False) if the Window isn't active.</remarks>
    /// </summary>
    public abstract class WindowCommand : ICommand
    {

        /// <summary>
        /// EventArgs class for the Execute events 
        /// </summary>
        public class WindowCommandArgs : EventArgs {

           /// <summary>
           /// Initializes a new instance of the <see cref="WindowCommandArgs"/> class.
           /// </summary>
           /// <param name="window">The Window.</param>
           /// <param name="parameter">The parameter.</param>
            public WindowCommandArgs( Window window, object parameter ){
                Window = window;
                Parameter = parameter;
            }
            /// <summary>
            /// The Window this command is for
            /// </summary>
            public Window Window;
            /// <summary>
            /// The provided parameters, if there are any (null otherwise)
            /// </summary>
            public object Parameter;
        }

        /// <summary>
        /// Arguments for the WindowCanExecute call
        /// </summary>
        public class WindowCanExecuteArgs : WindowCommandArgs
        {
           /// <summary>
           /// Initializes a new instance of the <see cref="WindowCanExecuteArgs"/> class.
           /// </summary>
           /// <param name="window">The Window.</param>
           /// <param name="parameter">The parameter.</param>
            public WindowCanExecuteArgs(Window window, object parameter) : base(window,parameter){}
            /// <summary>
            /// Should be set to TRUE if the command can execute.
            /// </summary>
            public bool CanExecute = false;
        }
        /// <summary>
        /// Arguments for the WindowOnExecute call
        /// </summary>
        public class WindowOnExecuteArgs : WindowCommandArgs
        {
            public WindowOnExecuteArgs(Window window, object parameter) : base(window, parameter) { }
            public bool Handled = false;
        }


        /// <summary>Default constructor for <see cref="WindowCommand"/> leaves the Window property unitialized.
        /// </summary>
        public WindowCommand() : base() { }
        
        /// <summary>Initializes a new instance of the <see cref="WindowCommand"/> class.
        /// </summary>
        /// <param name="window">The Window.</param>
        public WindowCommand(Window window) { _window = window; }

        private Window _window;

        /// <summary>Gets or sets the Window that is the target of this command
        /// </summary>
        /// <value>The Window.</value>
        
        public Window Window
        {
            get { return _window; }
            set { _window = value; }
        }

        #region ICommand Members
        /// <summary>Determines whether this instance can execute on specified Window 
        /// (or the default Window, if you pass in null).
        /// </summary>
        /// <param name="source">The Window.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can execute on the specified Window; otherwise, <c>false</c>.
        /// </returns>
        protected abstract void IfNoHandlerOnCanExecute(object source, WindowCanExecuteArgs e);
        /// <summary>Executes the hotkey action on the specified Window.
        /// </summary>
        /// <param name="source">The Window.</param>
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
        /// An instance of a <see cref="WindowCommand"/> which activates the Window
        /// </summary>
        public static ActivateCommand ActivateWindow = new ActivateCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which closes the Window
        /// </summary>
        public static CloseCommand CloseWindow = new CloseCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which hides the Window
        /// </summary>
        public static HideCommand HideWindow = new HideCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which unhides the Window
        /// </summary>
        public static ShowCommand ShowWindow = new ShowCommand();
        /// <summary>
        /// An instance of a <see cref="WindowCommand"/> which toggles the visibility of the Window
        /// </summary>
        public static ToggleCommand ToggleWindow = new ToggleCommand();


        /// <summary>
        /// A <see cref="WindowCommand"/> which activates the Window
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
                //Window wnd = (Window as Window) ?? Window;
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
        /// A <see cref="WindowCommand"/> which closes the Window
        /// </summary>
        public class CloseCommand : WindowCommand
        {
            public CloseCommand() : base() { }
            protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
            {
                //Window wnd = (Window as Window) ?? Window;
                e.CanExecute = e.Window.IsInitialized;
            }

            protected override void IfNoHandlerOnExecute(object window, WindowOnExecuteArgs e)
            {
                e.Window.Close();
            }
        }

        /// <summary>
        /// A <see cref="WindowCommand"/> which hides the Window
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
        /// A <see cref="WindowCommand"/> which unhides the Window
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
        /// A <see cref="WindowCommand"/> which toggles the visibility of the Window
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
