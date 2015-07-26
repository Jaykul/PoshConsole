using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Media;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using PoshCode.Controls.Properties;
using PoshCode.Controls.Utility;
using PoshCode.Interop;

namespace PoshCode.Controls
{
   public partial class ConsoleControl
   {

      void _passwordBox_PreviewKeyDown(object sender, KeyEventArgs e)
      {
         if (WaitingForInput)
            switch (e.Key)
            {
               case Key.Enter:
                  {
                     // get the command
                     _lastPassword = _passwordBox.SecurePassword;
                     // clear the box
                     FlushInputBuffer();
                     lock (_commandContainer)
                     {
                        // put the text in instead
                        Current.Inlines.Add( new string( _passwordBox.PasswordChar, _lastPassword.Length) + "\n");
                        // and move the _commandContainer to the "next" paragraph
                        Current.Inlines.Remove(_commandContainer);
                        Next = new Paragraph(_commandContainer);
                     }
                     Document.Blocks.Add(Next);
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
         // TODO: The special keys are hard-coded here. Need to switch these to the Command system, or at least make them configurable.
         //Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate
         //{
         if (!_waitingForKey)
         {
            switch (e.Key)
            {
               case Key.Enter:
                  CompleteBackgroundWorkItems();
                  OnEnterPressed(e);
                  break;

               case Key.Tab:
                  CompleteBackgroundWorkItems();
                  OnTabPressed(e);
                  break;

               case Key.F7:
                  CompleteBackgroundWorkItems();
                  OnHistoryMenu(e);
                  break;

               case Key.Up:
                  CompleteBackgroundWorkItems();
                  OnUpPressed(e);
                  break;

               case Key.Down:
                  CompleteBackgroundWorkItems();
                  OnDownPressed(e);
                  break;

               case Key.PageUp:
                  CompleteBackgroundWorkItems();
                  OnPageUpPressed(e);
                  break;

               case Key.PageDown:
                  CompleteBackgroundWorkItems();
                  OnPageDownPressed(e);
                  break;
               default:
                  _expansion.Reset();
                  _cmdHistory.Reset();
                  break;
            }
         }
      }

      /// <summary>
      /// Complete all work queued at background priority
      /// </summary>
      // TODO: Should be private or internal?
      public void CompleteBackgroundWorkItems() {
         // ConsoleControl (RichTextBox) uses asynchronous processing at background priority to
         // batch up the character update processing.   By synchronously executing an 
         // empty workitem at background priority, we ensure that all the prior keystrokes
         // have been completely processed.
         Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
      }


      private void OnDownPressed(KeyEventArgs e)
      {
         if (!e.KeyboardDevice.IsScrollLockToggled() || CurrentCommandLineCountPostCursor == 1)
         {
            CurrentCommand = _cmdHistory.Next(CurrentCommand);
            if (!e.IsModifierOn(ModifierKeys.Control))
               e.Handled = true;
         }
      }

      private void OnUpPressed(KeyEventArgs e)
      {
         if (!e.KeyboardDevice.IsScrollLockToggled() || CurrentCommandLineCountPreCursor == 1)
         {
            CurrentCommand = _cmdHistory.Previous(CurrentCommand);
            if(!e.IsModifierOn(ModifierKeys.Control))
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
         if (Selection == null || Selection.Text.Length == 0) {
            // if they didn't select anything, they were just clicking
            // Put the focus where it belongs
            if (!_popup.IsOpen) _commandContainer.Child.Focus();
         } 
         else if (Settings.Default.CopyOnMouseSelect) {
            try
            {
               Clipboard.SetText(Selection.Text, TextDataFormat.UnicodeText);
            }
            catch {
               // TODO: Should we warn if we can't set the clipboard?
            }
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
         Thread.Sleep(0);
         // CurrentCommand.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
         string cmdline = CurrentCommandPreCursor;
         bool hasMore = CurrentCommandPostCursor.Length > 0;

         if (!e.IsModifierOn(ModifierKeys.Control) && !hasMore)
         {
             List<string> choices = _expansion.GetChoices(cmdline);

            Trace.WriteLine((DateTime.Now - _tabTime).TotalMilliseconds);
            // DO NOT use menu mode if we're in _playbackMode 
            // OTHERWISE, DO USE it if there are more than TabCompleteMenuThreshold items
            // OR if they double-tapped
            if ((CurrentCommandPostCursor.Trim('\n', '\r').Length == 0) &&
                ((Settings.Default.TabCompleteMenuThreshold > 0
                && choices.Count > Settings.Default.TabCompleteMenuThreshold)
            || ((DateTime.Now - _tabTime).TotalMilliseconds < Settings.Default.TabCompleteDoubleTapMilliseconds)))
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
               string tabExpansion = e.IsModifierOn(ModifierKeys.Shift) ? _expansion.Previous(cmdline) : _expansion.Next(cmdline);
               CurrentCommand = cmdline.Substring(0, cmdline.Length - cmdline.GetLastWord(false).Length) + tabExpansion;// + (hasMore ? CurrentCommandPostCursor : string.Empty);
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
         // e.Handled = true;
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
         //if ((Keyboard.Modifiers & ModifierKeys.None) == 0 && !_popup.IsOpen) 
         //   _commandContainer.Child.Focus(); // Notice this is "whichever" is active ;)
         base.OnPreviewKeyDown(e);
      }

      //protected override void OnGotFocus(RoutedEventArgs e) {
      //   if (!_popup.IsOpen) _commandContainer.Child.Focus(); // Notice this is "whichever" is active ;)
      //   base.OnGotFocus(e);
      //}

      protected override void OnPreviewTextInput(TextCompositionEventArgs e)
      {
         if (!_popup.IsOpen) _commandContainer.Child.Focus(); // Notice this is "whichever" is active ;)
         //_commandBox.RaiseEvent(e);
         base.OnPreviewTextInput(e);
      }

      // TODO: change this to handle with the Tokenizer
      private void OnEnterPressed(KeyEventArgs e)
      {
         // if they CTRL+ENTER (or SHIFT+ENTER), we just let that pass
         if (!e.IsModifierOn(ModifierKeys.Control) && !e.IsModifierOn(ModifierKeys.Shift))
         {

            // we expect it to parse as legal script except when we're waiting for input
            if (!WaitingForInput)
            {
               // TODO: Find a way to do this IF v2, and not otherwise.
               // BUGBUG: Make sure we're handling the exceptions right.
               var errors = new Collection<PSParseError>();
               PSParser.Tokenize(CurrentCommand, out errors);

               if (errors.Count > 0)
               {
                  TextRange error;
                  foreach (var err in errors)
                  {
                     // TODO: what is a (locale-safe) test that the error is NOT just "Missing function body in function declaration." because they're still writing it?
                     error = new TextRange(
                        _commandBox.Document.ContentStart.GetPositionAtOffset(
                           err.Token.Start + (2*(err.Token.StartLine - 1)), LogicalDirection.Forward) ??
                        _commandBox.Document.ContentStart,
                        _commandBox.Document.ContentStart.GetPositionAtOffset(
                           err.Token.Start + (2*(err.Token.EndLine - 1)) + err.Token.Length, LogicalDirection.Backward) ??
                        _commandBox.Document.ContentEnd);
                     error.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
                     error.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Red);
                     // TODO: put in some sort of milder "clunk" sound here, since this isn't necessarily an error...
                     SystemSounds.Exclamation.Play();
                  }
                  e.Handled = true;
                  return; // out past the normal OnEnterPressed stuff...
               }
            }

            // get the command
            string cmd = CurrentCommand;
            // clear the box
            FlushInputBuffer();
            lock (_commandContainer)
            {
               // and move the _commandContainer to the "next" paragraph
               if (Current.Inlines.Contains(_commandContainer))
               {
                  Current.Inlines.Remove(_commandContainer);
                  Next = new Paragraph(_commandContainer);
                  Document.Blocks.Add(Next);
               }
            }
            UpdateLayout();

            cmd = cmd.TrimEnd();
            if (WaitingForInput)
            {
                _lastInputString = cmd;
                _gotInputLine.Set();
                Write(cmd + "\n");
            }
            else if (cmd.Length > 0)
            {
                OnCommand(new CommandEventArgs {Command = cmd, OutputBlock = Current});
            }

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
         //if (ScrollViewer.ViewportWidth > 0 && _next.Inlines.Count > 1 ) {
         //   var output = _commandContainer.SiblingInlines.Where(run => run != _commandContainer);
         //   var width = output.Sum(run => run.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Left);
         //   ((RichTextBox)_commandContainer.Child).MaxWidth = ScrollViewer.ViewportWidth - width;
         //}

         base.OnRenderSizeChanged(sizeInfo);
      }

   }
}
