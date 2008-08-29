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
using Huddled.WPF.Controls.Interfaces;
using Size = System.Management.Automation.Host.Size;

namespace Huddled.WPF.Controls
{
   ///<summary>
   ///Explicitly implements the IPSRawConsole interface.
   ///This implementation calls other existing methods on the ConsoleRichTextBox class, 
   ///and only exists to proxy the calls through the Dispatcher thread.
   ///</summary>
   public partial class ConsoleControl : IPSRawConsole  //, IPSConsole, IConsoleControlBuffered
   {
      #region IPSRawConsole Members

      bool _waitingForKey = false;
      ReadKeyOptions _readKeyOptions;
      LinkedList<TextCompositionEventArgs> _textBuffer = new LinkedList<TextCompositionEventArgs>();
      Queue<KeyInfo> _reInputBuffer;

      ///<summary>
      ///Provides a way for scripts to request user input ...
      ///</summary>
      ///<returns></returns>
      KeyInfo IPSRawConsole.ReadKey(ReadKeyOptions options)
      {
         throw new NotImplementedException("The ReadKey method is not (yet) implemented!");
      }
      bool IPSRawConsole.KeyAvailable
      {
         get
         {
            throw new NotImplementedException("The KeyAvailable property is not (yet) implemented!");
         }
      }


      int IPSRawConsole.CursorSize
      {
         get
         {
            return 25; // ToDo: create a customizable blinking cursor
         }
         set
         {
            /** It's currently meaningless to set our cursor **/
         }
      }

      System.Management.Automation.Host.Size IPSRawConsole.BufferSize
      {
         get
         {
            //throw new NotImplementedException("The BufferSize property getter is not (yet) implemented!");
            if (Dispatcher.CheckAccess())
            {
               return this.BufferSize;
            }
            else
            {
               return (Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => BufferSize));
            }
         }
         set
         {
            //throw new NotImplementedException("The BufferSize property setter is not (yet) implemented!");

            if (Dispatcher.CheckAccess())
            {
               this.BufferSize = value;
            }
            else
            {
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
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
            //throw new NotImplementedException("The MaxPhysicalWindowSize property getter is not (yet) implemented!");
            if (Dispatcher.CheckAccess())
            {
               return this.MaxPhysicalWindowSize;
            }
            else
            {
               return (Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => MaxPhysicalWindowSize));
            }
         }
      }

      System.Management.Automation.Host.Size IPSRawConsole.MaxWindowSize
      {
         get
         {
            //throw new NotImplementedException("The MaxWindowSize property getter is not (yet) implemented!");
            if (Dispatcher.CheckAccess())
            {
               return this.MaxWindowSize;
            }
            else
            {
               return (Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => MaxWindowSize));
            }
         }
      }

      System.Management.Automation.Host.Size IPSRawConsole.WindowSize
      {
         get
         {
            //throw new NotImplementedException("The WindowSize property getter is not (yet) implemented!");

            if (Dispatcher.CheckAccess())
            {
               return this.WindowSize;
            }
            else
            {
               return (Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => WindowSize));
            }
         }
         set
         {
            //throw new NotImplementedException("The WindowSize property setter is not (yet) implemented!");


            if (Dispatcher.CheckAccess())
            {
               this.WindowSize = value;
            }
            else
            {
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
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
            //throw new NotImplementedException("The CursorPosition property getter is not (yet) implemented!");


            if (Dispatcher.CheckAccess())
            {
               return this.CursorPosition;
            }
            else
            {
               return (Coordinates)Dispatcher.Invoke(DispatcherPriority.Normal,
                            (Func<Coordinates>)(() => this.CursorPosition));
            }
         }
         set
         {
            //throw new NotImplementedException("The CursorPosition property setter is not (yet) implemented!");

            if (Dispatcher.CheckAccess())
            {
               this.CursorPosition = value;
            }
            else
            {
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
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
            //throw new NotImplementedException("The WindowPosition property getter is not (yet) implemented!");
            if (Dispatcher.CheckAccess())
            {
               return this.WindowPosition;
            }
            else
            {
               return (Coordinates)Dispatcher.Invoke(DispatcherPriority.Normal,
                             (Func<Coordinates>)(() => this.WindowPosition));
            }
         }
         set
         {
            //throw new NotImplementedException("The WindowPosition property setter is not (yet) implemented!");
            if (Dispatcher.CheckAccess())
            {
               this.WindowPosition = value;
            }
            else
            {
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
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
               return BackgroundColor;
            }
            else
            {
               return (ConsoleColor)Dispatcher.Invoke(DispatcherPriority.Normal,
                  (Func<ConsoleColor>)(() => BackgroundColor));
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
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
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
               return ForegroundColor;
            }
            else
            {
               return (ConsoleColor)Dispatcher.Invoke(DispatcherPriority.Normal,
                  (Func<ConsoleColor>)(() => ForegroundColor));
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
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                {
                   ForegroundColor = value;
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
               return this.Title;
            }
            else
            {
               return (string)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<string>)(() => this.Title));
            }
         }
         set
         {
            if (Dispatcher.CheckAccess())
            {
               this.Title = value;
            }
            else
            {
               Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                {
                   this.Title = value;
                });
            }
         }

      }

      void IPSRawConsole.FlushInputBuffer()
      {
         //throw new NotImplementedException("The FlushInputBuffer method is not (yet) implemented!");
         if (Dispatcher.CheckAccess())
         {
            this.FlushInputBuffer();
            //_keyBuffer.Clear();
         }
         else
         {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => this.FlushInputBuffer()));
         }
      }

      BufferCell[,] IPSRawConsole.GetBufferContents(Rectangle rectangle)
      {
         throw new NotImplementedException("The GetBufferContents method is not (yet) implemented!");
         //if (Dispatcher.CheckAccess())
         // {
         //     return this.GetBufferContents(rectangle);
         // }
         // else
         // {
         //    return (BufferCell[,])Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate
         //     {
         //         return this.GetBufferContents(rectangle);
         //     });
         // }
      }

      void IPSRawConsole.SetBufferContents(Rectangle rectangle, BufferCell fill)
      {
         throw new NotImplementedException("The SetBufferContents method is not (yet) implemented!");
         //if (Dispatcher.CheckAccess())
         // {
         //     this.SetBufferContents(rectangle, fill);
         // }
         // else
         // {
         //    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
         //     {
         //         this.SetBufferContents(rectangle, fill);
         //     });
         // }
      }

      void IPSRawConsole.SetBufferContents(Coordinates origin, BufferCell[,] contents)
      {
         throw new NotImplementedException("The SetBufferContents method is not (yet) implemented!");
         //if (Dispatcher.CheckAccess())
         // {
         //     this.SetBufferContents(origin, contents);
         // }
         // else
         // {
         //    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
         //     {
         //         this.SetBufferContents(origin, contents);
         //     });
         // }
      }

      void IPSRawConsole.ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
      {
         throw new NotImplementedException("The ScrollBufferContents method is not (yet) implemented!");

         //if (Dispatcher.CheckAccess())
         //{
         //    this.ScrollBufferContents(source, destination, clip, fill);
         //}
         //else
         //{
         //   Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
         //    {
         //        this.ScrollBufferContents(source, destination, clip, fill);
         //    });
         //}
      }
      #endregion
   }
}
