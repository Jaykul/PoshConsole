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
// NOTE: YOU MAY *ALSO* DISTRIBUTE THIS FILE UNDER ANY OF THE FOLLOWING LICENSES:
// BSD:   http://opensource.org/licenses/bsd-license.php
// MIT:   http://opensource.org/licenses/mit-license.html
// Ms-PL: http://opensource.org/licenses/ms-pl.html
// GPL 2: http://opensource.org/licenses/gpl-2.0.php

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

namespace Huddled.Interop
{

   /// <summary>
   /// A wrapper around AllocConsole, with some nice eventing to handle
   /// </summary>
   public class NativeConsole : IDisposable
   {
      /// <summary>
      /// The API/Interop/PInvoke methods for the NativeConsole 
      /// </summary>
      internal class NativeMethods
      {

         #region  Fields (5)

         public const UInt32 DUPLICATE_SAME_ACCESS = 0x00000002;
         public const int LWA_ALPHA = 0x2;
         public const int LWA_COLORKEY = 0x1;
         public const int WS_EX_LAYERED = 0x80000;
         public const int WS_EX_TRANSPARENT = 0x00000020;

         #endregion

         #region  Enums (3)

         public enum ShowState : int
         {
            SW_HIDE = 0
            /// and lots of others
         }
         public enum GwlIndex : int
         {
            Id = (-12),
            Style = (-16),
            ExStyle = (-20)
         }
         public enum StdHandle : int
         {
            /// <summary>
            /// The standard input device
            /// </summary>
            INPUT_HANDLE = -10, //(DWORD)-10 	The standard input device.
            /// <summary>
            /// The standard output device.
            /// </summary>
            OUTPUT_HANDLE = -11, //(DWORD)-11 	The standard output device.
            /// <summary>
            /// The standard error device.
            /// </summary>
            ERROR_HANDLE = -12 //(DWORD)-12 	The standard error device.
         }

         #endregion

         #region  Methods (13)

         //  Public Methods (13)

         [DllImport("kernel32")]
         [return: MarshalAs(UnmanagedType.Bool)]
         public static extern bool AllocConsole();

         [DllImport("kernel32.dll", SetLastError = true)]
         [return: MarshalAs(UnmanagedType.Bool)]
         public static extern bool CloseHandle(IntPtr hHandle);

         [DllImport("kernel32.dll", SetLastError = true)]
         [return: MarshalAs(UnmanagedType.Bool)]
         public static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, 
                                    ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);

         [DllImport("kernel32.dll", SetLastError = true)]
         public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
            uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

         [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
         [return: MarshalAs(UnmanagedType.Bool)]
         public static extern bool FreeConsole();

         [DllImport("kernel32.dll")]
         public static extern IntPtr GetConsoleWindow();

         [DllImport("user32.dll", SetLastError = true)]
         public static extern int GetWindowLong(IntPtr hWnd, GwlIndex nIndex);

         [DllImport("kernel32.dll", SetLastError = true)]
         public static extern bool ReadFile(
             IntPtr hFile,                   // handle to file
             byte[] lpBuffer,                // data buffer
             int nNumberOfBytesToRead,       // number of bytes to read
             out int lpNumberOfBytesRead,    // number of bytes read
             IntPtr overlapped               // overlapped buffer
             );

         [DllImport("user32.dll")]
         public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, int crKey, 
                                                            byte bAlpha, int dwFlags);

         [DllImport("kernel32.dll")]
         [return: MarshalAs(UnmanagedType.Bool)]
         public static extern bool SetStdHandle(StdHandle nStdHandle, IntPtr hHandle);

         [DllImport("user32.dll")]
         public static extern int SetWindowLong(IntPtr hWnd, GwlIndex nIndex, int dwNewLong);

         [DllImport("user32.dll")]
         [return: MarshalAs(UnmanagedType.Bool)]
         public static extern bool ShowWindow(IntPtr hWnd, ShowState nCmdShow);

         [DllImport("kernel32.dll", SetLastError = true)]
         public static extern int WriteFile(IntPtr hFile, byte[] buffer,
           int numBytesToWrite, out int numBytesWritten, IntPtr lpOverlapped);

         #endregion

         [StructLayout(LayoutKind.Sequential)]
         public struct SECURITY_ATTRIBUTES
         {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
         }
      }

      public class OutputEventArgs
      {
         public string Text;
      }

      #region Delegate and Events (3)
      // Delegate, I changed this to be compatible with normal wpf/forms events
      public delegate void OutputDelegate(object source, OutputEventArgs args);

      // Events (2)
      public event OutputDelegate WriteErrorLine;
      public event OutputDelegate WriteOutputLine;
      #endregion

      #region  Private Fields (14)
      // Track whether Dispose has been called.
      private bool disposed = false;
      // A nice handle to our console window
      private IntPtr handle;
      // And our process
      private System.Diagnostics.Process process;
      // Our two threads
      private Thread outputThread, errorThread;
      // and the original handles to the console
      private IntPtr stdOutRead, stdOutWrite, stdInRead, stdInWrite, stdErrRead, stdErrWrite;
      // and the copy handles ...
      private IntPtr stdOutReadCopy, stdInWriteCopy, stdErrReadCopy;
      #endregion

      #region  Constructors and Destructors (2)
      /// <summary>Initializes a new instance of the <see cref="NativeConsole"/> class.
      /// </summary>
      public NativeConsole()
      {
         // Make ourselves a nice console
         NativeMethods.AllocConsole();
         // hide the window ...
         handle = NativeMethods.GetConsoleWindow();
         NativeMethods.ShowWindow(handle, NativeMethods.ShowState.SW_HIDE);
         //NativeMethods.SetWindowLong(handle, NativeMethods.GwlIndex.ExStyle, 
         // (NativeMethods.GetWindowLong(handle, NativeMethods.GwlIndex.ExStyle) |
         // NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT));
         //NativeMethods.SetLayeredWindowAttributes(handle, 0, 0, NativeMethods.LWA_ALPHA);

         process = System.Diagnostics.Process.GetCurrentProcess();


         NativeMethods.SECURITY_ATTRIBUTES saAttr;

         // Set the bInheritHandle flag so pipe handles are inherited.
         saAttr.nLength = Marshal.SizeOf(typeof(NativeMethods.SECURITY_ATTRIBUTES));
         saAttr.bInheritHandle = true;
         saAttr.lpSecurityDescriptor = IntPtr.Zero;


         // The steps for redirecting STDOUT:
         // * Create anonymous pipe to be STDOUT for us.
         // * Set STDOUT of our process to be WRITE handle to the pipe.
         // * Create a (noninheritable) duplicate of the read handle, and...
         // * Close the inheritable read handle.
         if (!NativeMethods.CreatePipe(out stdOutRead, out stdOutWrite, ref saAttr, 0))
         {
            System.Diagnostics.Trace.TraceError("Couldn't create the STDOUT pipe");
         }
         if (!NativeMethods.SetStdHandle(NativeMethods.StdHandle.OUTPUT_HANDLE, stdOutWrite))
         {
            System.Diagnostics.Trace.TraceError("Couldn't redirect STDOUT!");
         }
         // Create noninheritable read handle and close the inheritable read handle.
         if (!NativeMethods.DuplicateHandle(process.Handle, stdOutRead, process.Handle,
                        out stdOutReadCopy, 0, false, NativeMethods.DUPLICATE_SAME_ACCESS))
         {
            System.Diagnostics.Trace.TraceError("Couldn't Duplicate STDOUT Handle");
         }
         NativeMethods.CloseHandle(stdOutRead);

         // For the output handles we need a thread to read them
         outputThread = new Thread(OutputThread);
         outputThread.SetApartmentState(ApartmentState.STA);
         outputThread.Start();

         // The steps for redirecting STDERR are the same:
         // * Create anonymous pipe to be STDERR for us.
         // * Set STDERR of our process to be WRITE handle to the pipe.
         // * Create a (noninheritable) duplicate of the read handle and 
         // * Close the inheritable read handle.
         if (!NativeMethods.CreatePipe(out stdErrRead, out stdErrWrite, ref saAttr, 0))
         {
            System.Diagnostics.Trace.TraceError("Couldn't create the STDERR pipe");
         }
         if (!NativeMethods.SetStdHandle(NativeMethods.StdHandle.ERROR_HANDLE, stdErrWrite))
         {
            System.Diagnostics.Trace.TraceError("Couldn't redirect STDERR!");
         }
         // Create noninheritable read handle and close the inheritable read handle.
         if (!NativeMethods.DuplicateHandle(process.Handle, stdErrRead, process.Handle, 
            out stdErrReadCopy, 0, false, NativeMethods.DUPLICATE_SAME_ACCESS))
         {
            System.Diagnostics.Trace.TraceError("Couldn't Duplicate STDERR Handle");
         }
         NativeMethods.CloseHandle(stdErrRead);

         // For the output handles we need a thread to read them
         errorThread = new Thread(ErrorThread);
         errorThread.SetApartmentState(ApartmentState.STA);
         errorThread.Start();

         // The steps for redirecting STDIN:
         // * Create anonymous pipe to be STDIN for us.
         // * Set STDIN of our process to be READ handle to the pipe.
         // * Create a (noninheritable) duplicate of the WRITE handle and 
         // * Close the inheritable WRITE handle.

         if (!NativeMethods.CreatePipe(out stdInRead, out stdInWrite, ref saAttr, 0))
         {
            System.Diagnostics.Trace.TraceError("Couldn't create the StdIn pipe");
         }
         if (!NativeMethods.SetStdHandle(NativeMethods.StdHandle.INPUT_HANDLE, stdInRead))
         {
            System.Diagnostics.Trace.TraceError("Couldn't redirect StdIn!");
         }
         // Create noninheritable read handle and close the inheritable read handle.
         if (!NativeMethods.DuplicateHandle(process.Handle, stdInWrite, process.Handle, 
               out stdInWriteCopy, 0, false, NativeMethods.DUPLICATE_SAME_ACCESS))
         {
            System.Diagnostics.Trace.TraceError("Couldn't Duplicate StdIn Handle");
         }
         NativeMethods.CloseHandle(stdInWrite);
      }

      /// <summary>Releases unmanaged resources and performs other cleanup operations 
      /// before the <see cref="Console"/> is reclaimed by garbage collection.
      /// Use C# destructor syntax for finalization code.
      /// This destructor will run only if the Dispose method does not get called.
      /// </summary>
      /// <remarks>NOTE: Do not provide destructors in types derived from this class.
      /// </remarks>
      ~NativeConsole()
      {
         // Instead of cleaning up in BOTH Dispose() and here ...
         // We call Dispose(false) for the best readability and maintainability.
         Dispose(false);
      }

      #endregion

      #region Methods (5)

      //  Public Methods (2)

      /// <summary>
      /// Implement IDisposable
      /// Performs application-defined tasks associated with 
      /// freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose()
      {
         // This object will be cleaned up by the Dispose method.
         // Therefore, we call GC.SupressFinalize to tell the runtime 
         // that we dont' need to be finalized (we would clean up twice)
         GC.SuppressFinalize(this);

         Dispose(true);
      }

      /// <summary>
      /// Writes the input.
      /// </summary>
      /// <param name="input">The input.</param>
      public void WriteInput(string input)
      {
         byte[] bytes = System.Text.UTF8Encoding.Default.GetBytes(input);
         int written;

         int hresult = NativeMethods.WriteFile(stdInWriteCopy, bytes, 
                                 bytes.Length, out written, IntPtr.Zero);
         if (hresult != 1)
         {
            throw new Exception("Error Writing to StdIn, HRESULT: " + hresult.ToString());
         }
      }

      //  Private Methods (3)

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
         if (!disposed)
         {
            try
            {
               // // If disposing equals true, dispose all managed resources ALSO.
               if (disposing)
               {
                  errorThread.Abort();
                  outputThread.Abort();
                  //WriteInput("\n");

                  byte[] bytes = System.Text.UTF8Encoding.Default.GetBytes("\n" + (char)26);
                  int written;
                  NativeMethods.WriteFile(stdErrWrite, bytes, bytes.Length, 
                                                      out written, IntPtr.Zero);
                  NativeMethods.WriteFile(stdOutWrite, bytes, bytes.Length, 
                                                      out written, IntPtr.Zero);
                  //errorThread.Join();
                  //outputThread.Join();
               }

               // Clean up UnManaged resources
               // If disposing is false, only the following code is executed.
               NativeMethods.FreeConsole();
               //NativeMethods.CloseHandle(stdOutWrite);
               //NativeMethods.CloseHandle(stdOutReadCopy);
               //NativeMethods.CloseHandle(stdErrWrite);
               //NativeMethods.CloseHandle(stdErrReadCopy);
               NativeMethods.CloseHandle(stdInWriteCopy);
               NativeMethods.CloseHandle(stdInRead);
            }
            catch (Exception e)
            {
               Trace.WriteLine(e.Message);
               Trace.WriteLine(e.StackTrace);
               throw;
            }

         }
         disposed = true;
      }

      /// <summary>
      /// The ErrorThread ThreadStart delegate
      /// </summary>
      private void ErrorThread()
      {
         int BytesRead;
         byte[] BufBytes = new byte[4096];
         // consider wrapping this in a System.IO.FileStream
         try
         {
            while (NativeMethods.ReadFile(stdErrReadCopy, BufBytes, 4096, 
                                                      out BytesRead, IntPtr.Zero))
            {
               if (WriteErrorLine != null)
               {
                  WriteErrorLine(this, new OutputEventArgs {
                     Text = UTF8Encoding.Default.GetString(BufBytes, 0, BytesRead) });
               }
            }
         }
         catch (ThreadAbortException) { }
         finally
         {
            NativeMethods.CloseHandle(stdErrWrite);
            NativeMethods.CloseHandle(stdErrReadCopy);
         }
      }

      /// <summary>
      /// The OutputThread ThreadStart delegate
      /// </summary>
      private void OutputThread()
      {
         int BytesRead;
         byte[] BufBytes = new byte[4096];
         // consider wrapping this in a System.IO.FileStream
         try
         {
            while (NativeMethods.ReadFile(stdOutReadCopy, BufBytes, 4096, 
                                                      out BytesRead, IntPtr.Zero))
            {
               if (WriteOutputLine != null)
               {
                  WriteOutputLine(this, new OutputEventArgs { 
                     Text = UTF8Encoding.Default.GetString(BufBytes, 0, BytesRead) });
               }
            }
         }
         catch (ThreadAbortException) { }
         finally
         {
            NativeMethods.CloseHandle(stdOutWrite);
            NativeMethods.CloseHandle(stdOutReadCopy);
         }
      }
      #endregion
   }
}
