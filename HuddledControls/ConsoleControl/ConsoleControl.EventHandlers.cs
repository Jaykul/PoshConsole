using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Huddled.WPF.Controls.Utility;
using System.Windows.Threading;
using System.Windows.Documents;

namespace Huddled.WPF.Controls
{
   public partial class ConsoleControl
   {

      /// <summary>
      /// Lets us intercept special keys for the control
      /// </summary>
      /// <param name="e"></param>
      void _commandBox_PreviewKeyDown(object sender, KeyEventArgs e)
      {
         //Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate
         //{
         switch (e.Key)
         {
            case Key.Enter:
               OnEnterPressed(e);
               break;

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
               break;
         }

      }

      private void OnDownPressed(KeyEventArgs e)
      {
         if (!e.IsModifierOn(ModifierKeys.Control) && !e.KeyboardDevice.IsScrollLockToggled())
         {
            CurrentCommand = _cmdHistory.Next(CurrentCommand);
            e.Handled = true;
         }
      }

      private void OnUpPressed(KeyEventArgs e)
      {
         if (!e.IsModifierOn(ModifierKeys.Control) && !e.KeyboardDevice.IsScrollLockToggled())
         {
            CurrentCommand = _cmdHistory.Previous(CurrentCommand);
            e.Handled = true;
         }
      }


      private void OnPageDownPressed(KeyEventArgs e)
      {
         if (!e.KeyboardDevice.IsScrollLockToggled())
         {
            CurrentCommand = _cmdHistory.Last(CurrentCommand);
            e.Handled = true;
         }
      }

      private void OnPageUpPressed(KeyEventArgs e)
      {
         if (!e.KeyboardDevice.IsScrollLockToggled())
         {
            CurrentCommand = _cmdHistory.First(CurrentCommand);
            e.Handled = true;
         }
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

         // the "EndOfOutput" marker gets stuck just before the last character of the prompt...
         string[] cmds = new string[] { CurrentCommand };
         // hackmode for when we're playing back buffered keystrokes 
         cmds = CurrentCommand.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
         //if (cmd[cmd.Length - 1] == '\t') cmd = cmd.Substring(0, cmd.Length - 1);

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
               Point position = _commandBox.PointToScreen(_commandBox.GetRectFromCharacterIndex(_commandBox.CaretIndex, true).TopRight);

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
         Point position = _commandBox.PointToScreen(_commandBox.GetRectFromCharacterIndex(_commandBox.CaretIndex, true).TopRight);

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

      protected override void OnPreviewTextInput(TextCompositionEventArgs e)
      {
         if (!_popup.IsVisible) _commandBox.Focus();
         //_commandBox.RaiseEvent(e);
         base.OnPreviewTextInput(e);
      }

      private void OnEnterPressed(KeyEventArgs e)
      {
         // get the command
         string cmd = _commandBox.Text;
         // clear the box
         _commandBox.Text = "";
         lock (_commandContainer)
         {
            // put the text in instead
            _current.Inlines.InsertBefore(_commandContainer, new Run(cmd + "\n"));
            // and move the _commandContainer to the "next" paragraph
            _current.Inlines.Remove(_commandContainer);
            _next = new Paragraph(_commandContainer);
         }
         Document.Blocks.Add(_next);

         UpdateLayout();

         OnCommand(cmd);
         e.Handled = true;
      }
   }
}
