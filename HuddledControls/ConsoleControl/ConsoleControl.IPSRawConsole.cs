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
         // TODO: REIMPLEMENT PSHostRawUserInterface.ReadKey(ReadKeyOptions options)
         throw new NotImplementedException("The ReadKey method is not (yet) implemented!");
      }

      bool IPSRawConsole.KeyAvailable
      {
         get
         {
            if (Dispatcher.CheckAccess())
            {
               return _commandBox.Text.Length > 0;
            }
            else
            {
               return (bool)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<bool>)(() => _commandBox.Text.Length > 0));
            }
         }
      }


      int IPSRawConsole.CursorSize
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
               return (Size)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<Size>)(() => BufferSize));
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
            else if (!Dispatcher.HasShutdownStarted)
            {
               return (ConsoleColor)Dispatcher.Invoke(DispatcherPriority.Normal,
                  (Func<ConsoleColor>)(() => ForegroundColor));
            }
            else
            {
               return ConsoleColor.White;
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
         if (Dispatcher.CheckAccess())
         {
            _commandBox.Clear();
         }
         else
         {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => _commandBox.Clear()));
         }
      }

      BufferCell[,] IPSRawConsole.GetBufferContents(Rectangle rectangle)
      {
         // TODO: REIMPLEMENT PSHostRawUserInterface.GetBufferContents(Rectangle rectangle)
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
         // TODO: REIMPLEMENT PSHostRawUserInterface.SetBufferContents(Rectangle rectangle, BufferCell fill)
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
         // TODO: REIMPLEMENT PSHostRawUserInterface.SetBufferContents(Coordinates origin, BufferCell[,] contents)
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
         // TODO: REIMPLEMENT PSHostRawUserInterface.ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
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
