//
// Copyright (c) 2006 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
// PARTICULAR PURPOSE.
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Windows.Threading;
using System.Windows.Documents;

namespace Huddled.PoshConsole
{
    /// <summary>
    /// A sample implementation of the PSHostRawUserInterface for a console
    /// application. Members of this class that map trivially to the .NET console
    /// class are implemented. More complex methods are not implemented and will
    /// throw a NotImplementedException.
    /// </summary>
    class PoshRawUI : PSHostRawUserInterface
    {
        // SET delegates
        public delegate void SetSizeDelegate(Size size);
        public delegate void SetStringDelegate(String text);
        public delegate void SetCoordinatesDelegate(Coordinates coords);
        public delegate void SetIntDelegate(int val);
        public delegate void SetConsoleColorDelegate(ConsoleColor color);

        // GET delegates
        public delegate BufferCell[,] GetBufferDelegate(Rectangle source);
        public delegate Size GetSizeDelegate();
        public delegate Coordinates GetCoordinatesDelegate();


        private ConsoleTextBox myConsole;

        // TODO: when we move the prompt into the output buffer, we should look at implementing a cursor...
        internal int _CursorSize = 10; // we hard-code this, 'cause we don't actually have a cursor, just a caret
//        internal System.Management.Automation.Host.Size _BufferSize = new System.Management.Automation.Host.Size();
//        internal System.Management.Automation.Host.Size _WindowSize = new System.Management.Automation.Host.Size();
        internal System.Management.Automation.Host.Size _MaxWindowSize = new System.Management.Automation.Host.Size();
        internal System.Management.Automation.Host.Size _MaxPhysicalWindowSize = new System.Management.Automation.Host.Size();
//        internal System.Management.Automation.Host.Coordinates _WindowPosition = new Coordinates();
//        internal System.Management.Automation.Host.Coordinates _CursorPosition = new Coordinates();

        private ConsoleColor myBackground;
        private ConsoleColor myForeground;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoshRawUI"/> class.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="windowTitleDelegate">The window title delegate.</param>
        /// <param name="windowSizeDelegate">The window size delegate.</param>
        /// <param name="windowPositionDelegate">The window position delegate.</param>
        public PoshRawUI(ConsoleTextBox console )
        {
            myConsole = console;
            myBackground = Properties.Settings.Default.ConsoleDefaultBackground;
            myForeground = Properties.Settings.Default.ConsoleDefaultForeground;
        }

        /// <summary>
        /// Get and set the background color of text to be written.
        /// This maps pretty directly onto the corresponding .NET Console
        /// property.
        /// </summary>
        public override ConsoleColor BackgroundColor
        {
            get { return myBackground; }
            set { SetBackgroundColor( value); }
        }


        /// <summary>
        /// Get and set the foreground color of text to be written.
        /// This maps closely onto the corresponding .NET Console property.
        /// </summary>
        public override ConsoleColor ForegroundColor
        {
            get { return myForeground; }
            set { SetForegroundColor(value); }
        }

        /// <summary>
        /// Sets the color of the foreground.
        /// </summary>
        /// <param name="color">The color.</param>
        private void SetForegroundColor(ConsoleColor color)
        {
            if (myConsole.Dispatcher.CheckAccess())
            {
                myForeground = color;
                myConsole.ConsoleForeground = color;
            }
            else
            {
                myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new SetConsoleColorDelegate(SetForegroundColor), color);
            }
        }
        /// <summary>
        /// Sets the color of the background.
        /// </summary>
        /// <param name="color">The color.</param>
        private void SetBackgroundColor(ConsoleColor color)
        {
            if (myConsole.Dispatcher.CheckAccess())
            {
                myBackground = color;
                myConsole.ConsoleBackground = color;
            }
            else
            {
                myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new SetConsoleColorDelegate(SetBackgroundColor), color);
            }
        }


        /// <summary>
        /// Return the host buffer size adapted from the .NET Console buffer size.
        /// </summary>
        public override Size BufferSize
        {
            get { return GetBufferSize(); }
            set { SetBufferSize(value); }
        }

        private Size GetBufferSize()
        {
            if (myConsole.Dispatcher.CheckAccess())
            {
                return myConsole.BufferSize;
            }
            else
            {
                return (Size)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, new GetSizeDelegate(GetBufferSize));
            }
        }

        private void SetBufferSize(Size bufferSize)
        {
            if (myConsole.Dispatcher.CheckAccess())
            {
                myConsole.BufferSize = bufferSize;
            }
            else
            {
                myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SetSizeDelegate(SetBufferSize), bufferSize);
            }
        }


        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        public override Coordinates CursorPosition
        {
            get { return GetCursorPosition(); }
            set { SetCursorPosition(value); }
        }

        private Coordinates GetCursorPosition()
        {
            if (myConsole.Dispatcher.CheckAccess())
            {
                return myConsole.CursorPosition;
            }
            else
            {
                return (Coordinates)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, new GetCoordinatesDelegate(GetCursorPosition));
            }
        }

        private void SetCursorPosition(Coordinates cursorPosition)
        {
            if (myConsole.Dispatcher.CheckAccess())
            {
                myConsole.CursorPosition = cursorPosition;
            }
            else
            {
                myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SetCoordinatesDelegate(SetCursorPosition), cursorPosition);
            }
        }

        /// <summary>
        /// Return the cursor size taken directly from the .NET Console cursor size.
        /// </summary>
        public override int CursorSize
        {
            get { return _CursorSize; }
            set { /* It's meaningless to set our cursor */ }
        }

        //private void SetCursorSize(int cursorSize)
        //{
        //    if (myConsole.Dispatcher.CheckAccess())
        //    {
        //        _CursorSize = cursorSize;
        //        myConsole.cu.CursorPosition = cursorSize;
        //    }
        //    else
        //    {
        //        myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SetIntDelegate(SetCursorPosition), cursorSize);
        //    }
        //}

        /// <summary>
        /// This functionality is not currently implemented. The call simple returns silently.
        /// </summary>
        public override void FlushInputBuffer()
        {
            ;  //Do nothing...
        }



        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="rectangle">Unused</param>
        /// <returns>Returns nothing - call fails.</returns>
        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            if(!myConsole.Dispatcher.CheckAccess())
            {
                return (BufferCell[,]) myConsole.Dispatcher.Invoke(DispatcherPriority.Render, new GetBufferDelegate(GetBufferContents), rectangle);
            }

            return myConsole.GetRectangle(rectangle);

        }

        /// <summary>
        /// Map directly to the corresponding .NET Console property.
        /// </summary>
        public override bool KeyAvailable
        {
            get
            {
                throw new NotImplementedException("The KeyAvailable method isn't implemented.");
            }
        }

        /// <summary>
        /// Return the MaxPhysicalWindowSize size adapted from the .NET Console
        /// LargestWindowWidth and LargestWindowHeight.
        /// </summary>
        public override Size MaxPhysicalWindowSize
        {
            get { return _MaxPhysicalWindowSize; }
        }

        /// <summary>
        /// Return the MaxWindowSize size adapted from the .NET Console
        /// LargestWindowWidth and LargestWindowHeight.
        /// </summary>
        public override Size MaxWindowSize
        {
            get { return _MaxWindowSize; }
        }

        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="options">Unused</param>
        /// <returns>Nothing</returns>
        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotImplementedException("The ReadKey() method is not implemented by MyRawUserInterface.");
        }

        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="source">A Rectangle that identifies the region of the screen to be scrolled.</param>
        /// <param name="destination">Coordinates that identify the upper-left corner 
        /// of the region to receive the source region contents.</param>
        /// <param name="clip">A Rectangle that identifies the region of the screen to include in the operation.  
        /// If a cell does not fall within the clip region, it will be unchanged.</param>
        /// <param name="fill">A BufferCell that identifies the character and attributes to be used to fill
        /// the intersection of the source rectangle and clip rectangle 
        /// if it is left "empty" by the move. </param>
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            BufferCell[,] copy = GetBufferContents(source);
            throw new NotImplementedException("The ScrollBufferContents() method is not implemented by MyRawUserInterface.");
        }

        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="origin">Unused</param>
        /// <param name="contents">Unused</param>
        private delegate void SetBufferDelegate(Coordinates origin, BufferCell[,] contents);
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            // throw new NotImplementedException("The SetBufferContents() methods are not implemented by MyRawUserInterface.");
            if (!myConsole.Dispatcher.CheckAccess())
            {
                myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SetBufferDelegate(SetBufferContents), origin, new object[] { contents });
            }
            else
                myConsole.SetBufferContents(origin, contents);
        }

        /// <summary>
        ///  This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="rectangle">Unused</param>
        /// <param name="fill">Unused</param>
        private delegate void FillBufferDelegate(Rectangle rectangle, BufferCell fill);
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            if (!myConsole.Dispatcher.CheckAccess())
            {
                myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FillBufferDelegate(SetBufferContents), rectangle, new object[] { fill });
            } else {
                myConsole.SetBufferContents(rectangle, fill);
            }
        }

        private Coordinates GetWindowPosition()
        {
            if (!myConsole.Dispatcher.CheckAccess())
            {
                return (Coordinates)myConsole.Dispatcher.Invoke(DispatcherPriority.Render, new GetCoordinatesDelegate(GetWindowPosition));
            }
            return myConsole.WindowPosition;
        }

        private void SetWindowPosition( Coordinates coords )
        {
            if (!myConsole.Dispatcher.CheckAccess())
            {
                myConsole.Dispatcher.Invoke(DispatcherPriority.Render, new SetCoordinatesDelegate(SetWindowPosition), coords);
            } else
                myConsole.WindowPosition = coords;
        }

        /// <summary>
        /// Return the window position adapted from the Console window position information.
        /// </summary>
        public override Coordinates WindowPosition
        {

            get { return GetWindowPosition(); }
            set { SetWindowPosition(value); }
        }


        /// <summary>
        /// Return the window size adapted from the corresponding .NET Console calls.
        /// </summary>
        public override Size WindowSize
        {
            get { return GetWindowSize(); }
            set { SetWindowSize(value); }
        }

        private void SetWindowSize(Size value)
        {
            if (!myConsole.Dispatcher.CheckAccess())
            {
                myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SetSizeDelegate( SetWindowSize ), value);
            }
            else
                myConsole.WindowSize = value;
        }

        private Size GetWindowSize()
        {
            if (!myConsole.Dispatcher.CheckAccess())
            {
                return (Size)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, new GetSizeDelegate(GetWindowSize));
            }
            else
                return myConsole.WindowSize;
        }

        private delegate void SetValueDelegate(System.Windows.DependencyProperty property, object value);
        private delegate object GetValueDelegate(System.Windows.DependencyProperty property);
        /// <summary>
        /// Mapped to the Console.Title property.
        /// </summary>
        public override string WindowTitle
        {
            get { 
                //(string)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, GetWindowTitle, value);
                if (!myConsole.Dispatcher.CheckAccess())
                {
                    return (string)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, new GetValueDelegate(myConsole.GetValue), ConsoleTextBox.TitleProperty);
                }
                else return (string)myConsole.GetValue(ConsoleTextBox.TitleProperty);

            }
            set
            {
                if (!myConsole.Dispatcher.CheckAccess())
                {
                    myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SetValueDelegate(myConsole.SetValue), ConsoleTextBox.TitleProperty, value);
                } 
                else myConsole.SetValue(ConsoleTextBox.TitleProperty, value);
            }
        }
    }
}