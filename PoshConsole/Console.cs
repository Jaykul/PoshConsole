using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Huddled.PoshConsole
{
    public class NativeMethods {
        [DllImport("kernel32")]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, ShowState nCmdShow);


        public enum ShowState : int
        {
            SW_HIDE = 0
        }
    }
    public class Console : IDisposable
    {
        // A nice handle to our console window
        private IntPtr handle;
        // Track whether Dispose has been called.
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Console"/> class.
        /// </summary>
        public Console()
        {
            // Make ourselves a nice console
            NativeMethods.AllocConsole();
            handle = NativeMethods.GetConsoleWindow();
            NativeMethods.ShowWindow(handle, NativeMethods.ShowState.SW_HIDE);
        }


        /// <summary>
        /// Implement IDisposable
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method. Therefore, we call GC.SupressFinalize 
            // to tell the runtime we dont' need to be finalized (we would clean up twice)
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Handles actual cleanup actions, under two different scenarios
        /// </summary>
        /// <param name="disposing">if set to <c>true</c> we've been called directly or 
        /// indirectly by user code and can clean up both managed and unmanaged resources.
        /// Otherwise it's been called from the destructor/finalizer and we can't
        /// reference other managed objects (they might already be disposed).
        ///</param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // // If disposing equals true, dispose all managed resources ALSO.
                // if (disposing){}

                // Clean up UnManaged resources
                // If disposing is false, only the following code is executed.
                NativeMethods.FreeConsole();
            }
            disposed = true;         
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Console"/> is reclaimed by garbage collection.
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        /// <remarks>NOTE: Do not provide destructors in types derived from this class.</remarks>
        ~Console()      
        {
            // Instead of cleaning up in BOTH Dispose() and here ...
            // We call Dispose(false) for the best readability and maintainability.
            Dispose(false);
        }
    }
}
