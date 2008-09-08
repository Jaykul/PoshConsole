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
using Huddled.WPF.Controls.Interfaces;

namespace PoshConsole.PSHost
{
    /// <summary>
    /// An implementation of PSHostRawUserInterface based on an IConsoleControl 
    /// Basically this class is here because the PowerShell SDK has the UI and RawUI as abstract classes instead of interfaces.
    /// Since the PoshConsole is based on RichTextBox, it can't inherit from PSHostRawUserInterface
    /// 
    /// </summary>
    class PoshRawUI : PSHostRawUserInterface
    {
        
		#region  Fields (1)

		private IPSRawConsole myConsole;

		#endregion 

		#region  Constructors (1)

		/// <summary>
        /// Initializes a new instance of the <see cref="PoshRawUI"/> class.
        /// </summary>
        /// <param name="console">An implementation of <see cref="IPSRawConsole"/>.</param>
        public PoshRawUI(IPSRawConsole console )
        {
           myConsole = console;
        }
		
		#endregion 

        #region Stuff I should move into IConsoleControl
        /// <summary>
        /// Return the cursor size taken directly from the .NET Console cursor size.
        /// </summary>
        public override int CursorSize
        {
            get { return myConsole.CursorSize; }
            set { myConsole.CursorSize = value;  }
        }
        /// <summary>
        /// Return the MaxPhysicalWindowSize size adapted from the .NET Console
        /// LargestWindowWidth and LargestWindowHeight.
        /// </summary>
        public override System.Management.Automation.Host.Size MaxPhysicalWindowSize
        {
            get { return myConsole.MaxPhysicalWindowSize; }
        }
        /// <summary>
        /// Return the MaxWindowSize size adapted from the .NET Console
        /// LargestWindowWidth and LargestWindowHeight.
        /// </summary>
        public override System.Management.Automation.Host.Size MaxWindowSize
        {
            get { return myConsole.MaxWindowSize; }
        }
        /// <summary>
        /// This functionality is not currently implemented. The call simple returns silently.
        /// </summary>
        public override void FlushInputBuffer()
        {
            ;  //Do nothing...
        }
        #endregion Stuff I should move into IConsoleControl
        /// <summary>
        /// Map directly to the corresponding .NET Console property.
        /// </summary>
        public override bool KeyAvailable
        {
           get { return myConsole.KeyAvailable; }
        }
        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="options">Unused</param>
        /// <returns>Nothing</returns>
        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            return myConsole.ReadKey(options);
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
           myConsole.ScrollBufferContents(source, destination, clip, fill);
        }
        #region Implemented by IConsoleControl
        /// <summary>
        /// Return the host buffer size adapted from the .NET Console buffer size.
        /// </summary>
        public override Size BufferSize
        {
            get { return myConsole.BufferSize; }
            set { myConsole.BufferSize = value; }
        }
        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        public override Coordinates CursorPosition
        {
            get { return myConsole.CursorPosition; }
            set { myConsole.CursorPosition = value; }
        }
        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="rectangle">Unused</param>
        /// <returns>Returns nothing - call fails.</returns>
        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            return myConsole.GetBufferContents(rectangle);
        }
        /// <summary>
        /// This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="origin">Unused</param>
        /// <param name="contents">Unused</param>
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            myConsole.SetBufferContents(origin, contents); 
        }
        /// <summary>
        ///  This functionality is not currently implemented. The call fails with an exception.
        /// </summary>
        /// <param name="rectangle">Unused</param>
        /// <param name="fill">Unused</param>
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            myConsole.SetBufferContents(rectangle, fill);
        }
        /// <summary>
        /// Return the window position adapted from the Console window position information.
        /// </summary>
        public override Coordinates WindowPosition
        {
            get { return myConsole.WindowPosition; }
            set { myConsole.WindowPosition = value; }
        }
        /// <summary>
        /// Return the window size adapted from the corresponding .NET Console calls.
        /// </summary>
        public override Size WindowSize
        {
            get { return myConsole.WindowSize; }
            set { myConsole.WindowSize = value; }
        }
        public override string WindowTitle
        {
            get { return myConsole.WindowTitle; }
            set { myConsole.WindowTitle = value; }
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
        #endregion Implemented by IConsoleControl
    }
}
