using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Huddled.Interop.Keyboard;
using Huddled.WPF.Controls.Interfaces;
using Huddled.WPF.Controls.Utility;
using System.Windows.Threading;
using System.Windows.Documents;
using Keyboard=System.Windows.Input.Keyboard;
using System.Management.Automation;
using System.Collections.ObjectModel;

namespace Huddled.WPF.Controls
{
   public partial class ConsoleControl
   {

      void _passwordBox_PreviewKeyDown(object sender, KeyEventArgs e)
      {
         if (_waitingForInput)
            switch (e.Key)
            {
               case Key.Enter:
                  {
                     // get the command
                     _lastPassword = _passwordBox.SecurePassword;
                     // clear the box
                     ((IPSRawConsole)this).FlushInputBuffer();
                     
                     lock (_commandContainer)
                     {
                        // put the text in instead
                        _current.Inlines.Add( new string( _passwordBox.PasswordChar, _lastPassword.Length) + "\n");
                        // and move the _commandContainer to the "next" paragraph
                        _current.Inlines.Remove(_commandContainer);
                        _next = new Paragraph(_commandContainer);
                     }
                     Document.Blocks.Add(_next);
                     // Wait until here to set the event, because we don't want to mess with that _commandContainer
                     _gotInputLine.Set();
                     UpdateLayout();
                     e.Handled = true;
                  }
                  break;
            }
      }

      /// <summary>
      /// Lets us intercept special keys for the control
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void _commandBox_PreviewKeyDown(object sender, KeyEventArgs e)
      {

         //Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate
         //{
         if (!_waitingForKey)
         {
            switch (e.Key)
            {
               case Key.Enter:
                  {
                     var errors = new Collection<PSParseError>();
                     PSParser.Tokenize( CurrentCommand, out errors);

                     if (errors.Count > 0)
                     {
                        TextRange error;
                        foreach (var err in errors)
                        {
                           //_commandBox.Document.ContentStart.GetLineStartPosition(err.Token.StartLine-1).GetPositionAtOffset(err.Token.StartColumn)
                           error  = new TextRange(
                              _commandBox.Document.ContentStart.GetPositionAtOffset(err.Token.Start + (2*(err.Token.StartLine-1)),LogicalDirection.Forward),
                              _commandBox.Document.ContentStart.GetPositionAtOffset(err.Token.Start + (2*(err.Token.EndLine-1)) + err.Token.Length,LogicalDirection.Backward));
                           error.ApplyPropertyValue( Run.TextDecorationsProperty, TextDecorations.Underline);
                           error.ApplyPropertyValue(Run.ForegroundProperty, System.Windows.Media.Brushes.Red);
                           //int errStart = _commandBox.Selection.Start.Document.sel.inser..sel.get.GetCharacterIndexFromLineIndex(.StartLine) + err.Token.StartColumn;
                           //int errEnd = _commandBox.GetCharacterIndexFromLineIndex(err.Token.EndLine) + err.Token.EndColumn;
                        }

                     } else 
                        OnEnterPressed(e);
                  } break;

               case Key.Tab:
                  OnTabPressed(e);
                  break;

               case Key.F7:
                  OnHistoryMenu(e);
                  break;

               case Key.Up:
                  OnUpPressed(e);
                  break;

               case Key.Down:
                  OnDownPressed(e);
                  break;

               case Key.PageUp:
                  OnPageUpPressed(e);
                  break;

               case Key.PageDown:
                  OnPageDownPressed(e);
                  break;
               default:
                  _expansion.Reset();
                  _cmdHistory.Reset();
                  break;
            }
         }
      }

      private void OnDownPressed(KeyEventArgs e)
      {
         if ((CurrentCommandLineCount == 1 || !e.KeyboardDevice.IsScrollLockToggled()) && !e.IsModifierOn(ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift))
         {
            CurrentCommand = _cmdHistory.Next(CurrentCommand);
            e.Handled = true;
         }
      }

      private void OnUpPressed(KeyEventArgs e)
      {
         if ((CurrentCommandLineCount == 1 || !e.KeyboardDevice.IsScrollLockToggled()) && !e.IsModifierOn(ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift))
         {
            CurrentCommand = _cmdHistory.Previous(CurrentCommand);
            e.Handled = true;
         }
      }

      private void OnPageDownPressed(KeyEventArgs e)
      {
         //if (!e.KeyboardDevice.IsScrollLockToggled())
         //{
            CurrentCommand = _cmdHistory.Last(CurrentCommand);
            e.Handled = true;
         //}
      }

      private void OnPageUpPressed(KeyEventArgs e)
      {
         //if (!e.KeyboardDevice.IsScrollLockToggled())
         //{
            CurrentCommand = _cmdHistory.First(CurrentCommand);
            e.Handled = true;
         //}
      }
      protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
      {
         if (Properties.Settings.Default.CopyOnMouseSelect && Selection.Text.Length > 0)
         {
            Clipboard.SetText(Selection.Text, TextDataFormat.UnicodeText);
         }

         base.OnPreviewMouseLeftButtonUp(e);
      }

      protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
      {
         if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
         {
            if (e.Delta > 0)
            {
               NavigationCommands.IncreaseZoom.Execute(null, this);
            }
            else if (e.Delta < 0)
            {
               NavigationCommands.DecreaseZoom.Execute(null, this);
            }

            e.Handled = true;
         }
         base.OnPreviewMouseWheel(e);
      }

      private void OnTabPressed(KeyEventArgs e)
      {
         System.Threading.Thread.Sleep(0);

         string[] cmds = CurrentCommand.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

         if (!e.IsModifierOn(ModifierKeys.Control) && cmds.Length > 0)
         {
            List<string> choices = _expansion.GetChoices(cmds[0]);

            // DO NOT use menu mode if we're in _playbackMode 
            // OTHERWISE, DO USE it if there are more than TabCompleteMenuThreshold items
            // OR if they double-tapped
            System.Diagnostics.Trace.WriteLine(((TimeSpan)(DateTime.Now - _tabTime)).TotalMilliseconds);
            if ((cmds.Length == 1) &&
                ((Properties.Settings.Default.TabCompleteMenuThreshold > 0
                && choices.Count > Properties.Settings.Default.TabCompleteMenuThreshold)
            || (((TimeSpan)(DateTime.Now - _tabTime)).TotalMilliseconds < Properties.Settings.Default.TabCompleteDoubleTapMilliseconds)))
            {
               
               Point position = _commandBox.PointToScreen( _commandBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward).TopLeft );

               _popup.ShowTabPopup(
                 new Rect(
                    position.X,
                    position.Y,
                    Math.Abs(ScrollViewer.ViewportWidth - position.X),
                    Math.Abs(ScrollViewer.ViewportHeight - position.Y)),
                 choices, CurrentCommand);
            }
            else
            {
               if (e.IsModifierOn(ModifierKeys.Shift))
               {
                  CurrentCommand = _expansion.Previous(cmds[0]) + ((cmds.Length > 1) ? string.Join("\t", cmds, 1, cmds.Length - 1) : string.Empty);
               }
               else
               {
                  CurrentCommand = _expansion.Next(cmds[0]) + ((cmds.Length > 1) ? string.Join("\t", cmds, 1, cmds.Length - 1) : string.Empty);
               }
            }
            _tabTime = DateTime.Now;
            e.Handled = true;
         }
      }

      private void OnHistoryMenu(KeyEventArgs e)
      {
         //if (inPrompt && !IsScrollLockToggled(e.KeyboardDevice))
         //{
         Point position = _commandBox.PointToScreen(_commandBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward).TopLeft);

         _popup.ShowHistoryPopup(
            new Rect(
               position.X,
               position.Y,
               Math.Abs(ScrollViewer.ViewportWidth - position.X),
               Math.Abs(ScrollViewer.ViewportHeight - position.Y)),
            _cmdHistory.GetChoices(CurrentCommand));
         e.Handled = true;
         //}
      }

      /// <summary>
      /// Implement right-click the way the normal console does: paste into the prompt.
      /// </summary>
      /// <param name="e"></param>
      protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
      {
         ApplicationCommands.Paste.Execute(null, this);
         e.Handled = true;
      }

      protected override void OnPreviewKeyUp(KeyEventArgs e)
      {
         //Trace.WriteLine(string.Format("Preview KeyUp, queueing KeyInfo: {0} ({1})", e.Key, e.Handled));
         // NOTE: if it's empty, it must have been CLEARed during the KeyDown, 
         //       so we don't want to count the KeyUp either
         if (_inputBuffer.Count > 0)
         {
            _inputBuffer.Enqueue(e.ToKeyInfo());
            _gotInputKey.Set();
         }
         base.OnPreviewKeyUp(e);
      }

      protected override void OnPreviewKeyDown(KeyEventArgs e)
      {
         //Trace.WriteLine(string.Format("Preview KeyDown, queueing KeyInfo: {0}", e.Key));
         _inputBuffer.Enqueue(e.ToKeyInfo());
         if ((Keyboard.Modifiers & ModifierKeys.None) == 0) 
            _commandContainer.Child.Focus(); // Notice this is "whichever" is active ;)
         base.OnPreviewKeyDown(e);
      }

      protected override void OnPreviewTextInput(TextCompositionEventArgs e)
      {
         if (!_popup.IsVisible) _commandContainer.Child.Focus(); // Notice this is "whichever" is active ;)
         //_commandBox.RaiseEvent(e);
         base.OnPreviewTextInput(e);
      }

      // TODO: change this to handle with the Tokenizer
      private void OnEnterPressed(KeyEventArgs e)
      {
         // if they CTRL+ENTER, let it through
         if (!e.IsModifierOn(ModifierKeys.Control) && !e.IsModifierOn(ModifierKeys.Shift))
         {
            // get the command
            string cmd = CurrentCommand;
            // clear the box
            ((IPSRawConsole)this).FlushInputBuffer();
            lock (_commandContainer)
            {
               // put the text in instead
               _current.Inlines.Add(cmd + "\n");
               // and move the _commandContainer to the "next" paragraph
               _current.Inlines.Remove(_commandContainer);
               _next = new Paragraph(_commandContainer);
            }
            Document.Blocks.Add(_next);
            UpdateLayout();
            OnCommand(cmd);

            e.Handled = true;
         } else {
            //var indx = _commandBox.CaretPosition.GetOffsetToPosition.CaretIndex;
            _commandBox.CaretPosition = _commandBox.CaretPosition.InsertLineBreak();
            e.Handled = true;
         }
      }

      protected override void OnDragEnter(DragEventArgs e)
      {
         if (e.Data.GetDataPresent(DataFormats.Text))
         {
            e.Effects |= DragDropEffects.Copy;
         }
         else
         {
            e.Effects = DragDropEffects.None;
         }
         base.OnDragEnter(e);
      }
      protected override void OnDrop(DragEventArgs e)
      {
         if (e.Data.GetDataPresent(DataFormats.Text))
         {
            _commandBox.CaretPosition.InsertTextInRun( (string)e.Data.GetData(DataFormats.Text) );
            e.Handled = true;
         }
         base.OnDrop(e);
      }

      protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
      {
         if (ScrollViewer.ViewportWidth > 0 && _next.Inlines.Count > 1 )
         {
            ((RichTextBox)_commandContainer.Child).MaxWidth = ScrollViewer.ViewportWidth - _next.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left;
         }

         base.OnRenderSizeChanged(sizeInfo);
      }

   }
}
