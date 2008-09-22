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
using Huddled.Interop;
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
      LinkedList<TextCompositionEventArgs> _textBuffer = new LinkedList<TextCompositionEventArgs>();
      readonly Queue<KeyInfo> _inputBuffer = new Queue<KeyInfo>();

      ///<summary>
      ///Provides a way for scripts to request user input ...
      ///</summary>
      ///<returns></returns>
      KeyInfo IPSRawConsole.ReadKey(ReadKeyOptions options)
      {
         if((options & (ReadKeyOptions.IncludeKeyUp| ReadKeyOptions.IncludeKeyDown)) == 0)
         {
            throw new MethodInvocationException("Cannot read key options. To read options either IncludeKeyDown, IncludeKeyUp or both must be set."); 
         }

         while (true)
         {
            if (_inputBuffer.Count == 0)
            {
               _waitingForKey = true;
               _gotInput.Reset();
               _gotInput.WaitOne();
               _waitingForKey = false;
            }
            else
            {

               var ki = _inputBuffer.Dequeue();
               if (ki.Character != 0)
               {
                  Dispatcher.BeginInvoke(
                     (Action) (() => ShouldEcho(ki.Character, (options & ReadKeyOptions.NoEcho) == 0)));
               }

               if ((((options & ReadKeyOptions.IncludeKeyDown) > 0) && ki.KeyDown) ||
                   (((options & ReadKeyOptions.IncludeKeyUp) > 0) && !ki.KeyDown))
               {
                  return ki;
               }
            }
         }
      }

      private void ShouldEcho(char ch, bool echo)
      {
         if (_commandBox.Text.Length > 0)
         {         
            if (ch == _commandBox.Text[0])
            {
               // emulate NoEcho by UN-echoing...
               if (_commandBox.Text.Length > 1)
               {
                  _commandBox.Text = _commandBox.Text.Substring(1);
               }
               else
               {
                  _commandBox.Text = "";
               }
               // if we're NOT NoEcho, then re-echo it:
               if (echo)
               {
                  Write(null, null, new string(ch, 1));
               }
            }
         }
      }



      private int _keyIndex = 0;

      bool IPSRawConsole.KeyAvailable
      {
         get
         {
            if (Dispatcher.CheckAccess())
            {
               return _commandBox.Text.Length > _keyIndex;
            }
            else
            {
               return (bool)Dispatcher.Invoke(DispatcherPriority.Normal, (Func<bool>)(() => _commandBox.Text.Length > _keyIndex));
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

      /// <summary>
      /// Implements the CLS command the way most command-lines do:
      /// Scroll the window until the prompt is at the top ...
      /// (as opposed to clearing the screen and leaving the prompt at the bottom)
      /// </summary>        
      public void ClearScreen()
      {
         if (Dispatcher.CheckAccess())
         {
            _current.Inlines.Remove(_commandContainer);
            Document.Blocks.Clear();
            //new TextRange(Document.ContentStart, Document.ContentEnd).Text = String.Empty;

            _current = _next = new Paragraph(_commandContainer);
            Document.Blocks.Add(_next);
         }
         else
         {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => {
               _current.Inlines.Remove(_commandContainer);
               Document.Blocks.Clear();
               //new TextRange(Document.ContentStart, Document.ContentEnd).Text = String.Empty;

               _current = _next = new Paragraph(_commandContainer);
               Document.Blocks.Add(_next);
            }));
         }
      }


      void IPSRawConsole.SetBufferContents(Rectangle rectangle, BufferCell fill)
      {
         if (rectangle.Left == -1 && rectangle.Right == -1)
         {
            ClearScreen();
         } else

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
