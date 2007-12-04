using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Management.Automation.Host;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Management.Automation;
using System.Threading;
using System.Collections.ObjectModel;
using System.Text;

namespace PoshConsole.Controls
{
    /// <summary>
    /// Explicitly implements the IPSRawConsole interface.
    /// This implementation calls other existing methods on the ConsoleRichTextBox class, 
    /// and only exists to proxy the calls through the Dispatcher thread.
    /// </summary>
    public partial class ConsoleRichTextBox : IPSRawConsole  //, IPSConsole, IConsoleControlBuffered
    {
        #region IPSRawConsole Members

        int IPSRawConsole.CursorSize
        {
            get
            {
                if (Dispatcher.CheckAccess()) { 
                    return this.CursorSize; 
                }
                else
                {
                    return (int)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.CursorSize;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.CursorSize = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.CursorSize = value;
                    });
                }
            }
        }

        System.Management.Automation.Host.Size IPSRawConsole.BufferSize
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.BufferSize;
                }
                else
                {
                    return (System.Management.Automation.Host.Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.BufferSize;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.BufferSize = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.BufferSize = value;
                    });
                }
            }

        }

        System.Management.Automation.Host.Size IPSRawConsole.MaxPhysicalWindowSize
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.MaxPhysicalWindowSize;
                }
                else
                {
                    return (System.Management.Automation.Host.Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.MaxPhysicalWindowSize;
                    });
                }
            }
        }

        System.Management.Automation.Host.Size IPSRawConsole.MaxWindowSize
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.MaxWindowSize;
                }
                else
                {
                    return (System.Management.Automation.Host.Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.MaxWindowSize;
                    });
                }
            }
        }

        System.Management.Automation.Host.Size IPSRawConsole.WindowSize
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.WindowSize;
                }
                else
                {
                    return (System.Management.Automation.Host.Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.WindowSize;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.WindowSize = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.WindowSize = value;
                    });
                }
            }
        }

        Coordinates IPSRawConsole.CursorPosition
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.CursorPosition;
                }
                else if (Dispatcher.HasShutdownStarted) 
                {
                    return new Coordinates(); 
                }
                else
                {
                    return (Coordinates)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.CursorPosition;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.CursorPosition = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.CursorPosition = value;
                    });
                }
            }
        }

        Coordinates IPSRawConsole.WindowPosition
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.WindowPosition;
                }
                else
                {
                    return (Coordinates)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.WindowPosition;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.WindowPosition = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.WindowPosition = value;
                    });
                }
            }
        }

        ConsoleColor IPSRawConsole.BackgroundColor
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.BackgroundColor;
                }
                else
                {
                    return (ConsoleColor)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.BackgroundColor;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.BackgroundColor = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.BackgroundColor = value;
                    });
                }
            }

        }

        ConsoleColor IPSRawConsole.ForegroundColor
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.ForegroundColor;
                }
                else
                {
                    return (ConsoleColor)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.ForegroundColor;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.ForegroundColor = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.ForegroundColor = value;
                    });
                }
            }

        }

        string IPSRawConsole.WindowTitle
        {
            get
            {
                if (Dispatcher.CheckAccess())
                {
                    return this.WindowTitle;
                }
                else
                {
                    return (string)Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                    {
                        return this.WindowTitle;
                    });
                }
            }
            set
            {
                if (Dispatcher.CheckAccess())
                {
                    this.WindowTitle = value;
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                    {
                        this.WindowTitle = value;
                    });
                }
            }

        }

        void IPSRawConsole.FlushInputBuffer()
        {
            if (Dispatcher.CheckAccess())
            {
                this.FlushInputBuffer();
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                {
                    this.FlushInputBuffer();
                });
            }
        }

        BufferCell[,] IPSRawConsole.GetBufferContents(Rectangle rectangle)
        {
            if (Dispatcher.CheckAccess())
            {
                return this.GetBufferContents(rectangle);
            }
            else
            {
                return (BufferCell[,])Dispatcher.Invoke(DispatcherPriority.Normal, (Invoke)delegate
                {
                    return this.GetBufferContents(rectangle);
                });
            }
        }

        void IPSRawConsole.SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            if (Dispatcher.CheckAccess())
            {
                this.SetBufferContents(rectangle, fill);
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                {
                    this.SetBufferContents(rectangle, fill);
                });
            }
        }

        void IPSRawConsole.SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            if (Dispatcher.CheckAccess())
            {
                this.SetBufferContents(origin, contents);
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                {
                    this.SetBufferContents(origin, contents);
                });
            }
        }

        void IPSRawConsole.ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            if (Dispatcher.CheckAccess())
            {
                this.ScrollBufferContents(source, destination, clip, fill);
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
                {
                    this.ScrollBufferContents(source, destination, clip, fill);
                });
            }
        }
        #endregion
    }
}
