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

        private RichTextConsole myConsole;

        // TODO: when we move the prompt into the output buffer, we should look at implementing a cursor...
        internal int _CursorSize = 10; // we hard-code this, 'cause we don't actually have a cursor, just a caret
        internal System.Management.Automation.Host.Size _MaxWindowSize = new System.Management.Automation.Host.Size();
        internal System.Management.Automation.Host.Size _MaxPhysicalWindowSize = new System.Management.Automation.Host.Size();

        /// <summary>
        /// Initializes a new instance of the <see cref="PoshRawUI"/> class.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="windowTitleDelegate">The window title delegate.</param>
        /// <param name="windowSizeDelegate">The window size delegate.</param>
        /// <param name="windowPositionDelegate">The window position delegate.</param>
        public PoshRawUI(RichTextConsole console )
        {
            myConsole = console;
        }

        /// <summary>
        /// Return the host buffer size adapted from the .NET Console buffer size.
        /// </summary>
        public override System.Management.Automation.Host.Size BufferSize
        {
            get { return (Size)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate { return myConsole.BufferSize; }); }
            set { myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate { myConsole.BufferSize = value; }); }
        }

        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        public override Coordinates CursorPosition
        {
            get { return (Coordinates)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate { return myConsole.CursorPosition; }); }
            set { myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate { myConsole.CursorPosition = value; }); }
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
            return (BufferCell[,])myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate { return myConsole.GetBufferContents(rectangle); }); 
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
        public override System.Management.Automation.Host.Size MaxPhysicalWindowSize
        {
            get { return _MaxPhysicalWindowSize; }
        }

        /// <summary>
        /// Return the MaxWindowSize size adapted from the .NET Console
        /// LargestWindowWidth and LargestWindowHeight.
        /// </summary>
        public override System.Management.Automation.Host.Size MaxWindowSize
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
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate {myConsole.SetBufferContents(origin, contents); }); 
        }

        /// <summary>
        ///  This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="rectangle">Unused</param>
        /// <param name="fill">Unused</param>
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate { myConsole.SetBufferContents(rectangle, fill); }); 
        }

        /// <summary>
        /// Return the window position adapted from the Console window position information.
        /// </summary>
        public override Coordinates WindowPosition
        {
            get { return (Coordinates)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate { return myConsole.WindowPosition; }); }
            set { myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate { myConsole.WindowPosition = value; }); }
        }


        /// <summary>
        /// Return the window size adapted from the corresponding .NET Console calls.
        /// </summary>
        public override System.Management.Automation.Host.Size WindowSize
        {
            get { return (Size)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate { return myConsole.WindowSize; }); }
            set { myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate { myConsole.WindowSize = value; }); }
        }

        /// <summary>
        /// Mapped to the Console.Title property.
        /// </summary>
        public override string WindowTitle
        {
            get { return (string)myConsole.Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate { return (string)myConsole.GetValue(RichTextConsole.TitleProperty); }); }
            set { myConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate { myConsole.SetValue(RichTextConsole.TitleProperty, value); }); }
        }

        public override ConsoleColor BackgroundColor
        {
            get { return myConsole.BackgroundColor;  }
            set { myConsole.BackgroundColor = value; }
        }

        public override ConsoleColor ForegroundColor
        {
            get { return myConsole.ForegroundColor;  }
            set { myConsole.ForegroundColor = value; }
        }
    }
}