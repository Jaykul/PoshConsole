using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Host;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Huddled.WPF.Controls.Utility;
namespace Huddled.WPF.Controls
{
   public class PopupMenu : Popup
   {

      #region [rgn] Fields (6)

      IPoshConsoleControl _console;
      int _intelliNum = -1;
      ListBox _intellisense = new ListBox();
      static Regex _number = new Regex(@"[0-9]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
      static Regex _tabseparator = new Regex(@"[.;,=\\ |/[\]()""']", RegexOptions.CultureInvariant | RegexOptions.Compiled);
      string _terminalString = string.Empty;

      #endregion [rgn]

      #region [rgn] Constructors (1)

      public PopupMenu(IPoshConsoleControl console)
      {
         Closed += ClosedTabComplete;
         Closed += ClosedHistory;

         StaysOpen = false;
         _intellisense.SelectionMode = SelectionMode.Single;
         _intellisense.SelectionChanged += IntellisenseSelectionChanged;
         _intellisense.IsTextSearchEnabled = true;
         //_intellisense.PreviewTextInput   += new TextCompositionEventHandler(popup_TextInput);

         _console = console;

         Child = _intellisense;
         PlacementTarget = _console.CommandBox;

      }

      #endregion [rgn]

      #region [rgn] Methods (9)

      // [rgn] Protected Methods (3)

      /// <summary>
      /// Responds when the value of the <see cref="P:System.Windows.Controls.Primitives.Popup.IsOpen"/> property changes from to true to false.
      /// </summary>
      /// <param name="e">The event data.</param>
      protected override void OnClosed(EventArgs e)
      {
         base.OnClosed(e);
         _intelliNum = -1;
         _terminalString = string.Empty;
         _intellisense.Items.Filter = null;
         _tabbing = null;
         _console.CommandBox.Focus();
      }

      /// <summary>
      /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyDown"/> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
      /// </summary>
      /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs"/> that contains the event data.</param>
      protected override void OnPreviewKeyDown(KeyEventArgs e)
      {
         Trace.TraceInformation("Entering popup_PreviewKeyDown:");
         Trace.Indent();
         //Trace.WriteLine("Event:  {0}" + e.RoutedEvent);
         Trace.WriteLine("Key:    " + e.Key);
         Trace.WriteLine("Index:  " + _intellisense.SelectedIndex);
         Trace.WriteLine("Count:  " + _intellisense.Items.Count);
         //Trace.WriteLine("Source: {0}" + e.Source);

         _terminalString = string.Empty;

         switch (e.Key)
         {
            case Key.Up:
               {
                 if(_intellisense.SelectedIndex <= 0)
                 {
                    _intellisense.SelectedIndex = _intellisense.Items.Count - 1;
                    _intellisense.ScrollIntoView(_intellisense.SelectedItem);
                    e.Handled = false;
                 }
                 Trace.WriteLine("Key.Up: " + _intellisense.SelectedIndex);
               } break;
            case Key.Down:
               {
                  if (_intellisense.SelectedIndex >= _intellisense.Items.Count - 1)
                  {
                     _intellisense.SelectedIndex = 0;
                    _intellisense.ScrollIntoView(_intellisense.SelectedItem);
                    e.Handled = false;
                  }
                  Trace.WriteLine("Key.Dn: " + _intellisense.SelectedIndex);
               } break;
            case Key.Space:
               {
                  _terminalString = " ";
                  e.Handled = true;
                  IsOpen = false;
               } break;
            case Key.Back:
               {
                  // BUGBUG: if you tab back past where TAB was pressed.
                  //         this doesn't actually expand the _intellsense!
                  //  WORSE: it doesn't update the text on the fly.
               
                  // Update the filter
                  _tabbing = (_tabbing.Length == 0) ? "" : _tabbing.Substring(0, _tabbing.Length - 1);
                  _lastWord = (_lastWord.Length == 0) ? "" : _lastWord.Substring(0, _lastWord.Length - 1);

                  _intellisense.Items.Filter = new Predicate<object>(TypeAheadFilter);
                  _intellisense.SelectedIndex = 0;
                  e.Handled = true;
               } break;
            case Key.Delete: goto case Key.Escape;
            case Key.Escape:
               {
                  _intellisense.SelectedIndex = -1;
                  e.Handled = false;
                  IsOpen = false;
               } break;
            case Key.Tab:
               {
                  if (e.IsModifierOn(ModifierKeys.Shift))
                  {
                     if (_intellisense.SelectedIndex > 0)
                     {
                        _intellisense.SelectedIndex -= 1;
                     }
                     else
                     {
                        _intellisense.SelectedIndex = _intellisense.Items.Count - 1;
                     }
                  }
                  else
                  {
                     if (_intellisense.SelectedIndex < _intellisense.Items.Count - 1)
                     {
                        _intellisense.SelectedIndex += 1;
                     }
                     else
                     {
                        _intellisense.SelectedIndex = 0;
                     }
                  }
                  e.Handled = true;
               } break;
            case Key.Enter:
               {
                  e.Handled = true;
                  IsOpen = false;
               } break;
         }
         Trace.Unindent();
         Trace.TraceInformation("Exiting popup_PreviewKeyDown");
         base.OnPreviewKeyDown(e);
      }

      /// <summary>
      /// Handles the TextInput event of the popup control 
      /// 1) to save the key if it's one we consider to toggle the tab-complete
      /// 2) to handle typing numbers for the history menu
      /// </summary>
      protected override void OnPreviewTextInput(TextCompositionEventArgs e)
      {
         if (_tabseparator.IsMatch(e.Text))
         {
            _terminalString = e.Text;
            IsOpen = false;
         }
         else if (_number.IsMatch(e.Text))
         {
            if (_intelliNum >= 0)
            {
               _intelliNum *= 10;
            }
            else _intelliNum = 0;

            _intelliNum += int.Parse(e.Text);
            if (_intelliNum > 0 && _intelliNum < _intellisense.Items.Count - 1)
            {
               _intellisense.SelectedIndex = _intelliNum;
            }
         }
         else if (_tabbing != null)
         {
            // Update the filter
            _tabbing += e.Text;
            _lastWord += e.Text;
            _intellisense.Items.Filter = new Predicate<object>(TypeAheadFilter);

            _intellisense.SelectedIndex = 0;
            if (_intellisense.Items.Count <= 1) IsOpen = false;
            //intellisense.Items.Refresh();
            // intellisense.Items.Count  //tabbingCount
         }
         base.OnTextInput(e);
         Focus();
         _intellisense.Focus();
      }

      // [rgn] Private Methods (4)

      /// <summary>
      /// Handles the Closed event of the History popup menu.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="ea">The <see cref="System.EventArgs"/> instance containing the event data.</param>
      void ClosedHistory(object sender, EventArgs ea)
      {
         if (_tabbing == null)
         {
            if (_intellisense.SelectedValue != null)
            {
               _console.CurrentCommand = TextSearch.GetText((ListBoxItem)_intellisense.SelectedValue);
            }
         }
      }

      /// <summary>
      /// Handles the Closed event of the TabComplete popup menu.
      /// </summary>
      /// <param name="sender">The source of the event.</param>
      /// <param name="ea">The <see cref="System.EventArgs"/> instance containing the event data.</param>
      void ClosedTabComplete(object sender, EventArgs ea)
      {
         if (_tabbing != null)
         {
            string cmd = _console.CurrentCommand;
            if (_intellisense.SelectedValue != null)
            {
               Trace.TraceInformation("CurrentCommand: {0}", cmd);
               Trace.TraceInformation("TabExpansion: {0}", _intellisense.SelectedValue);
               _console.CurrentCommand = cmd.Substring(0, cmd.Length - cmd.GetLastWord(false).Length) + _intellisense.SelectedValue + _terminalString;
            }
            else
            {
               _console.CurrentCommand = _tabbing;
            }
            _console.CommandBox.Focus();
         }
      }

      // /// <summary>
      // /// Called when [navigate history].
      // /// </summary>
      // /// <param name="count">The count.</param>
      //private void OnNavigateHistory(int count)
      //{
      //    historyIndex += count;

      //    if (null != GetHistory)
      //    {
      //        CurrentCommand = GetHistory(ref historyIndex);
      //    }
      //    else
      //    {
      //        if (historyIndex == -1)
      //        {
      //            historyIndex = myHistory.Count;
      //        }
      //        if (historyIndex > 0 && historyIndex <= myHistory.Count)
      //        {
      //            CurrentCommand = myHistory[myHistory.Count - historyIndex];
      //        }
      //        else
      //        {
      //            historyIndex = 0;
      //            CurrentCommand = string.Empty;
      //        }
      //    }
      //}
      ///// <summary>
      ///// Called when [tab complete].
      ///// </summary>
      ///// <param name="shift">if set to <c>true</c> [shift].</param>
      //private void OnTabComplete(int count)
      //{
      //    tabbingCount += count;
      //    if (tabbing == null)
      //    {
      //        tabbing = CurrentCommand;
      //        if (!string.IsNullOrEmpty(tabbing))
      //        {
      //            lastWord = GetLastWord(tabbing);
      //            Cursor = Cursors.Wait;
      //            completions = TabComplete(tabbing, lastWord);

      //            Cursor = Cursors.IBeam;
      //        } // make sure it's never an empty string.
      //        else tabbing = null;
      //    }
      //    if (tabbing != null)
      //    {
      //        if (tabbingCount >= completions.Count)
      //        {
      //            tabbingCount = 0;
      //        }
      //        else if (tabbingCount < 0)
      //        {
      //            tabbingCount = completions.Count - 1;
      //        }

      //        // show the menu if:
      //        // TabCompleteMenuThreshold > 0 and there are more items than the threshold
      //        // OR they tabbed twice really fast
      //        if ((Properties.Settings.Default.TabCompleteMenuThreshold > 0
      //                && completions.Count > Properties.Settings.Default.TabCompleteMenuThreshold)
      //            || (((TimeSpan)(DateTime.Now - tabTime)).TotalMilliseconds < Properties.Settings.Default.TabCompleteDoubleTapMilliseconds))
      //        {
      //            string prefix = tabbing.Substring(0, tabbing.Length - lastWord.Length);
      //            popupClosing = new EventHandler(TabComplete_Closed);
      //            popup.Closed += popupClosing;
      //            completions.Sort(); // TODO: Sort this intelligently...
      //            ShowPopup(completions, false, false);
      //        }
      //        else if (tabbingCount < completions.Count)
      //        {
      //            CurrentCommand = tabbing.Substring(0, tabbing.Length - lastWord.Length) + completions[tabbingCount];
      //        }
      //    }
      //    tabTime = DateTime.Now;
      //}
      //protected override void OnPreviewTextInput(TextCompositionEventArgs e)
      //{
      //    // if they're trying to input text, they will overwrite the selection
      //    // lets make sure they don't overwrite the history buffer
      //    if (!Selection.IsEmpty)
      //    {
      //        if (Selection.Start.GetOffsetToPosition(commandStart) >= 0)
      //        {
      //            Selection.Select(commandStart.GetInsertionPosition(LogicalDirection.Forward), Selection.End);
      //        }
      //        if (Selection.End.GetOffsetToPosition(commandStart) >= 0)
      //        {
      //            Selection.Select(Selection.Start, commandStart.GetInsertionPosition(LogicalDirection.Forward));
      //        }
      //    }
      //    base.OnPreviewTextInput(e);
      //}


      /// <summary>
      /// Shows the popup.
      /// </summary>
      /// <param name="placementRectangle">The placement rectangle.</param>
      /// <param name="items">The items.</param>
      /// <param name="number">if set to <c>true</c> [number].</param>
      /// <param name="filterDupes">if set to <c>true</c> [filter dupes].</param>
      private void ShowPopup(Rect placementRectangle, List<string> items, bool number, bool filterDupes)
      {
         _intellisense.Items.Clear();
         if (number)
         {
            _intellisense.Items.Filter = null;
            for (int i = items.Count - 1; i >= 0; i--)
            {
               if (filterDupes && _intellisense.Items.Contains(items[i])) continue;
               ListBoxItem item = new ListBoxItem();
               TextSearch.SetText(item, items[i]); // A name must start with a letter or the underscore character (_), and must contain only letters, digits, or underscores
               item.Content = string.Format("{0,2} {1}", i, items[i]);
               _intellisense.Items.Insert(0, item);
            }
         }
         else
         {
            for (int i = items.Count - 1; i >= 0; i--)
            {
               if (!filterDupes || !_intellisense.Items.Contains(items[i]))
               {
                  _intellisense.Items.Insert(0, items[i]);
               }
            }
         }

         _intellisense.Visibility = Visibility.Visible;
         // if it's numbered, default to the last item
         _intellisense.SelectedIndex = number ? items.Count - 1 : 0;
         _intellisense.ScrollIntoView(_intellisense.SelectedItem);

         PlacementRectangle = placementRectangle;
         Placement = PlacementMode.RelativePoint;

         IsOpen = true;          // show the popup
         Focus();
         _intellisense.Focus();  // focus the keyboard on the popup
      }

      /// <summary>
      /// Types the ahead filter.
      /// </summary>
      /// <param name="item">The item.</param>
      /// <returns></returns>
      private bool TypeAheadFilter(object item)
      {
         return (item.ToString()).ToLower().StartsWith(_lastWord.ToLower());
      }

      // [rgn] Internal Methods (2)

      internal void ShowHistoryPopup(Rect placementRectangle, List<string> list)
      {
         ShowPopup(placementRectangle, list, true, Properties.Settings.Default.HistoryMenuFilterDupes);
      }

      /// <summary>
      /// Shows the tab-expansion popup.
      /// </summary>
      /// <param name="placementRectangle">The position to show the popup in.</param>
      /// <param name="list">The list of options</param>
      /// <param name="currentCommand">The current command</param>
      internal void ShowTabPopup(Rect placementRectangle, List<string> list, string currentCommand)
      {
         // TODO: Minor: Sort this intelligently, by type...
         // list.Sort();
         list = list.Distinct().ToList(); 
         // And filter it too
         _tabbing = currentCommand.TrimEnd('\r','\n');
         _lastWord = _tabbing.GetLastWord();
         // the sweet typeahead auto-filter
         _intellisense.Items.Filter = new Predicate<object>(TypeAheadFilter);

         ShowPopup(placementRectangle, list, false, false);
      }

      #endregion [rgn]

      private string _lastWord, _tabbing;

      #region Handle Clicks on the Intellisense popup.
      private bool _popupClicked;
      private void IntellisenseSelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         // if they clicked, then when the selection changes we close.
         if (_popupClicked)
         {
            IsOpen = false;
            _popupClicked = false;
         }
      }
      protected override void OnMouseDown(MouseButtonEventArgs e)
      {
         _popupClicked = true;
         base.OnMouseDown(e);
      }
      protected override void OnMouseWheel(MouseWheelEventArgs e)
      {
         if(e.Delta > 0 )
         {
            if(_intellisense.SelectedIndex + e.Delta < _intellisense.Items.Count)
            {
               _intellisense.SelectedIndex = _intellisense.SelectedIndex + e.Delta;
               _intellisense.ScrollIntoView(_intellisense.SelectedItem);
            } else
            {
               _intellisense.SelectedIndex = (_intellisense.SelectedIndex + e.Delta) - _intellisense.Items.Count;
               _intellisense.ScrollIntoView(_intellisense.SelectedItem);
            }
         } 
         else
         {
            if (_intellisense.SelectedIndex - e.Delta > 0)
            {
               _intellisense.SelectedIndex = _intellisense.SelectedIndex - e.Delta;
               _intellisense.ScrollIntoView(_intellisense.SelectedItem);
            }
            else
            {
               _intellisense.SelectedIndex = _intellisense.Items.Count - (_intellisense.SelectedIndex - e.Delta);
               _intellisense.ScrollIntoView(_intellisense.SelectedItem);
            }
         }
         // base.OnMouseWheel(e);
      }
      #endregion

   }
}