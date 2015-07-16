using System;
using System.Collections.Generic;
using System.Management.Automation.Host;
using System.Windows.Input;
using System.Windows.Threading;
using PoshCode.Wpf.Controls;

namespace PoshCode.PowerShell
{
	class HostRawUI : PSHostRawUserInterface
	{
        private readonly ConsoleControl _control;

        public HostRawUI(ConsoleControl control)
		{
			// TODO: Complete member initialization
			_control = control;
		}

        ///<summary>
        ///Provides a way for scripts to request user input ...
        ///</summary>
        ///<returns></returns>
        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            return _control.ReadKey(options);
        }

	    public override void FlushInputBuffer()
	    {
	        _control.FlushInputBuffer();
	    }

	    private int _keyIndex = 0;

	    public override bool KeyAvailable
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.CurrentCommand.Length > _keyIndex;
                }
                else
                {
                    return (bool)_control.Dispatcher.Invoke(DispatcherPriority.Normal, (Func<bool>)(() => _control.CurrentCommand.Length > _keyIndex));
                }
            }
        }


	    public override int CursorSize
        {
            get
            {
                return 25;
            }
            set
            {
                // ToDo: INVESTIGATE: can we change the caret size?
            }
        }

	    public override Size BufferSize
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.BufferSize;
                }
                else
                {
                    return (Size)_control.Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => BufferSize));
                }
            }
            set
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    _control.BufferSize = value;
                }
                else
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                    {
                        _control.BufferSize = value;
                    });
                }
            }

        }

	    public override Size MaxPhysicalWindowSize
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.MaxPhysicalWindowSize;
                }
                else
                {
                    return (Size)_control.Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => MaxPhysicalWindowSize));
                }
            }
        }

	    public override Size MaxWindowSize
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.MaxWindowSize;
                }
                else
                {
                    return (Size)_control.Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => MaxWindowSize));
                }
            }
        }

	    public override Size WindowSize
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.WindowSize;
                }
                else
                {
                    return (Size)_control.Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => WindowSize));
                }
            }
            set
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    _control.WindowSize = value;
                }
                else
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                    {
                        _control.WindowSize = value;
                    });
                }
            }
        }

	    public override Coordinates CursorPosition
        {
            get
            {
                _control.CompleteBackgroundWorkItems();
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.CursorPosition;
                }
                else
                {
                    return (Coordinates)_control.Dispatcher.Invoke(DispatcherPriority.Normal,
                                 (Func<Coordinates>)(() => _control.CursorPosition));
                }
            }
            set
            {
                _control.CompleteBackgroundWorkItems();
                if (_control.Dispatcher.CheckAccess())
                {
                    _control.CursorPosition = value;
                }
                else
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                    {
                        _control.CursorPosition = value;
                    });
                }
            }
        }

	    public override Coordinates WindowPosition
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.WindowPosition;
                }
                else
                {
                    return (Coordinates)_control.Dispatcher.Invoke(DispatcherPriority.Normal,
                                  (Func<Coordinates>)(() => _control.WindowPosition));
                }
            }
            set
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    _control.WindowPosition = value;
                }
                else
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                    {
                        _control.WindowPosition = value;
                    });
                }
            }
        }

	    public override ConsoleColor BackgroundColor
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.BackgroundColor;
                }
                else
                {
                    return (ConsoleColor)_control.Dispatcher.Invoke(DispatcherPriority.Normal,
                       (Func<ConsoleColor>)(() => _control.BackgroundColor));
                }
            }
            set
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    _control.BackgroundColor = value;
                }
                else
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                    {
                        _control.BackgroundColor = value;
                    });
                }
            }
        }

	    public override ConsoleColor ForegroundColor
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.ForegroundColor;
                }
                else if (!_control.Dispatcher.HasShutdownStarted)
                {
                    return (ConsoleColor)_control.Dispatcher.Invoke(DispatcherPriority.Normal,
                       (Func<ConsoleColor>)(() => _control.ForegroundColor));
                }
                else
                {
                    return ConsoleColor.White;
                }
            }
            set
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    _control.ForegroundColor = value;
                }
                else
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                    {
                        _control.ForegroundColor = value;
                    });
                }
            }

        }

	    public override string WindowTitle
        {
            get
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    return _control.Title;
                }
                else
                {
                    return (string)_control.Dispatcher.Invoke(DispatcherPriority.Normal, (Func<string>)(() => _control.Title));
                }
            }
            set
            {
                if (_control.Dispatcher.CheckAccess())
                {
                    _control.Title = value;
                }
                else
                {
                    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                    {
                        _control.Title = value;
                    });
                }
            }

        }


	    public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            // TODO: REIMPLEMENT PSHostRawUserInterface.GetBufferContents(Rectangle rectangle)
            // throw new NotImplementedException("The GetBufferContents method is not (yet) implemented!");
            _control.CompleteBackgroundWorkItems();
            if (_control.Dispatcher.CheckAccess())
            {
                return _control.GetBufferContents(rectangle);
            }
            else
            {
                return (BufferCell[,])_control.Dispatcher.Invoke(DispatcherPriority.Normal, (Func<BufferCell[,]>)(() => _control.GetBufferContents(rectangle)));
            }
        }



	    public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            _control.CompleteBackgroundWorkItems();
            if (rectangle.Left == -1 && rectangle.Right == -1)
            {
                _control.ClearScreen();
            }
            else

                // TODO: REIMPLEMENT PSHostRawUserInterface.SetBufferContents(Rectangle rectangle, BufferCell fill)
                throw new NotImplementedException("The SetBufferContents method is not (yet) implemented!");
            //if (_control.Dispatcher.CheckAccess())
            // {
            //     _control.SetBufferContents(rectangle, fill);
            // }
            // else
            // {
            //    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
            //     {
            //         _control.SetBufferContents(rectangle, fill);
            //     });
            // }
        }

	    public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            // TODO: REIMPLEMENT PSHostRawUserInterface.SetBufferContents(Coordinates origin, BufferCell[,] contents)
            throw new NotImplementedException("The SetBufferContents method is not (yet) implemented!");
            //if (_control.Dispatcher.CheckAccess())
            // {
            //     _control.SetBufferContents(origin, contents);
            // }
            // else
            // {
            //    _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
            //     {
            //         _control.SetBufferContents(origin, contents);
            //     });
            // }
        }

	    public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            // TODO: REIMPLEMENT PSHostRawUserInterface.ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
            throw new NotImplementedException("The ScrollBufferContents method is not (yet) implemented!");

            //if (_control.Dispatcher.CheckAccess())
            //{
            //    _control.ScrollBufferContents(source, destination, clip, fill);
            //}
            //else
            //{
            //   _control.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
            //    {
            //        _control.ScrollBufferContents(source, destination, clip, fill);
            //    });
            //}
        }

	}
}
