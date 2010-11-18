// Copyright (c) 2008 Joel Bennett http://HuddledMasses.org/

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
      public class WindowCommandArgs : EventArgs
      {

         /// <summary>
         /// Initializes a new instance of the <see cref="WindowCommandArgs"/> class.
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="parameter">The parameter.</param>
         public WindowCommandArgs(Window window, object parameter)
         {
            Window = window;
            Parameter = parameter;
         }
         /// <summary>
         /// A reference to the Window this command is for
         /// </summary>
         private WeakReference _window;

         /// <summary>Gets or sets the Window that is the target of this command
         /// </summary>
         /// <value>The Window.</value>
         public Window Window
         {
            get
            {
               if (_window == null)
               {
                  return null;
               }
               else
               {
                  return _window.Target as Window;
               }
            }
            set { _window = value == null ? null : new WeakReference(value); }
         }

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
         public WindowCanExecuteArgs(Window window, object parameter) : base(window, parameter) { }
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
      public WindowCommand(Window window) { Window = window; }

      private WeakReference _window;

      /// <summary>Gets or sets the Window that is the target of this command
      /// </summary>
      /// <value>The Window.</value>
      public Window Window
      {
         get
         {
            if (_window == null)
            {
               return null;
            }
            else
            {
               return _window.Target as Window;
            }
         }
         set
         {
            if (value == null)
            {
               _window = null;
            }
            else
            {
               _window = new WeakReference(value);
            }
         }
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

      /// <summary>Delegate for the CanExecute event</summary>
      public delegate void CanExecuteHandler(object source, WindowCanExecuteArgs e);
      /// <summary>Delegate for the Execute event</summary>
      public delegate void ExecuteHandler(object source, WindowOnExecuteArgs e);

      /// <summary>
      /// Occurs when we're checking if we can execute this command.
      /// </summary>
      public event CanExecuteHandler OnCanExecute;
      /// <summary>
      /// Occurs when we're trying to execute this command.
      /// </summary>
      public event ExecuteHandler OnExecute;

      /// <summary>
      /// Defines the method that determines whether the command can execute in its current state.
      /// </summary>
      /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
      /// <returns>
      /// true if this command can be executed; otherwise, false.
      /// </returns>
      public bool CanExecute(object parameter)
      {
         WindowCanExecuteArgs args = new WindowCanExecuteArgs((parameter as Window) ?? Window, parameter);

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

      /// <summary>
      /// Defines the method to be called when the command is invoked.
      /// </summary>
      /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
      public void Execute(object parameter)
      {
         WindowOnExecuteArgs args = new WindowOnExecuteArgs((parameter as Window) ?? Window, parameter);

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
      /// (Is never fired for WindowCommand, but might be on derived types)
      /// </summary>
	  public virtual event EventHandler CanExecuteChanged
	  {
		  add { throw new NotSupportedException(); }
		  remove { }
	  }
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
         /// <summary>
         /// Determines whether this instance can execute on specified Window
         /// (or the default Window, if you pass in null).
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
         protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
         {
            e.CanExecute = e.Window.IsLoaded;
         }

         /// <summary>
         /// Executes the hotkey action on the specified Window.
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
         protected override void IfNoHandlerOnExecute(object window, WindowOnExecuteArgs e)
         {
            if (!e.Window.IsVisible)
            {
               e.Window.Show();
            }
            e.Window.Activate();
         }
      }

      /// <summary>
      /// A <see cref="WindowCommand"/> which closes the Window
      /// </summary>
      public class CloseCommand : WindowCommand
      {
         /// <summary>
         /// Determines whether this instance can execute on specified Window
         /// (or the default Window, if you pass in null).
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
         protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
         {
            e.CanExecute = e.Window.IsInitialized;
         }

         /// <summary>
         /// Executes the hotkey action on the specified Window.
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
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
         /// <summary>
         /// Determines whether this instance can execute on specified Window
         /// (or the default Window, if you pass in null).
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
         protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
         {
            e.CanExecute = e.Window.IsVisible;
         }

         /// <summary>
         /// Executes the hotkey action on the specified Window.
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
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
         /// <summary>
         /// Determines whether this instance can execute on specified Window
         /// (or the default Window, if you pass in null).
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
         protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
         {
            e.CanExecute = !e.Window.IsVisible && e.Window.IsLoaded;
         }

         /// <summary>
         /// Executes the hotkey action on the specified Window.
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
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
         }
      }

      /// <summary>
      /// A <see cref="WindowCommand"/> which toggles the visibility of the Window
      /// </summary>
      public class ToggleCommand : WindowCommand
      {
         /// <summary>
         /// Determines whether this instance can execute on specified Window
         /// (or the default Window, if you pass in null).
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
         protected override void IfNoHandlerOnCanExecute(object window, WindowCanExecuteArgs e)
         {
            e.CanExecute = e.Window.IsLoaded;
         }

         /// <summary>
         /// Executes the hotkey action on the specified Window.
         /// </summary>
         /// <param name="window">The Window.</param>
         /// <param name="e">The event arguments</param>
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
