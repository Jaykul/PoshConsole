using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Markup;

namespace Huddled.PoshConsole
{
    partial class ConsoleRichTextBox
    {

        /// <summary>
        /// Reloads the content of the Bar.
        /// </summary>
        /// <returns><c>true</c> if we succeed in loading from the SourceFile; <c>false</c> otherwise.</returns>
        public List<Block> LoadBanner(string SourceFile)
        {
            List<Block> blocks = new List<Block>();
            if (!System.IO.File.Exists(SourceFile))
            {
                return blocks;
            }
            
            try
            {
                FlowDocument banner = (FlowDocument)XamlReader.Load(new System.IO.FileStream(SourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read));//, context);
                foreach (Block b in banner.Blocks)
                {
                    blocks.Add(b);
                }
                banner.Blocks.Clear();
                return blocks;
            }
            catch (Exception ex)
            {
                // ToDo: Offer some help about how to fix this.
                // ToDo: Show (at least one level of) InnerException if it's not null
                string message;
                if (null != ex.InnerException)
                {
                    message = ex.Message + "\n\nRoot Cause:\n" + ex.InnerException.Message;
                }
                else
                {
                    message = ex.Message;
                }

                MessageBox.Show(string.Format("Syntax error loading {0}\n{1}\n\nStack Trace:\n{2}", SourceFile, message, ex.StackTrace), "Error Loading XAML GeoBar", MessageBoxButton.OK);
                System.Diagnostics.Trace.TraceError("Syntax error loading {0}\n{1}", SourceFile, ex.Message);
                System.Diagnostics.Trace.TraceInformation(ex.StackTrace);
                blocks.Clear();
                return blocks;
            }
        }
        public override void EndInit()
        {
            base.EndInit();
            // LOAD the startup banner only when it's set (instead of removing it after)
            if (Properties.Settings.Default.StartupBanner)
            {
                try
                {
                    // Document.Blocks.InsertBefore(Document.Blocks.FirstBlock, new Paragraph(new Run("PoshConsole`nVersion 1.0.2007.8150")));
                    Document.Blocks.AddRange(LoadBanner("StartupBanner.xaml"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(@"Problem loading StartupBanner.xaml\n{0}", ex.Message);
                    Document.Blocks.Clear();
                    Write(Foreground, _consoleBrushes.Transparent, "PoshConsole 1.0.2007.8150");
                }
                ClearUndoBuffer();
            }
        }

        //protected override void OnInitialized(EventArgs e)
        //{
        //    base.OnInitialized(e);

        //    // LOAD the startup banner only when it's set (instead of removing it after)
        //    if (Properties.Settings.Default.StartupBanner)
        //    {
        //        try
        //        {
        //            Write(Foreground, _consoleBrushes.Transparent, "PoshConsole\nVersion 1.0.2007.8150");
        //            // Document.Blocks.InsertBefore(Document.Blocks.FirstBlock, new Paragraph(new Run("PoshConsole`nVersion 1.0.2007.8150")));
        //            Document.Blocks.InsertBefore(Document.Blocks.FirstBlock, LoadBanner("StartupBanner.xaml"));
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Trace.TraceError(@"Problem loading StartupBanner.xaml\n{1}", ex.Message);
        //            Document.Blocks.Clear();
        //        }
        //    }
        //}

        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            base.OnQueryContinueDrag(e);

            e.Action = DragAction.Cancel;
            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            CaretPosition = Document.ContentEnd;
            base.OnGotFocus(e);
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

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (!CaretInCommand)
            {
                CaretPosition = Document.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
            }

            ApplicationCommands.Paste.Execute(null, this);
            e.Handled = true;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            Trace.TraceInformation("Entering OnPreviewKeyDown:");
            Trace.Indent();
            Trace.WriteLine("Event:  " + e.RoutedEvent);
            Trace.WriteLine("Key:    " + e.Key);
            Trace.WriteLine("Source: " + e.Source);

            if (_commandStart == null || e.Source != this)
            {
                base.OnPreviewKeyDown(e);

                Trace.Unindent();
                Trace.TraceInformation("Exiting OnPreviewKeyDown:");

                return;
            }

            //if (_currentParagraph.Inlines.Count < _promptInlines)
            //{
            //    SetPrompt();
            //    _promptInlines = _currentParagraph.Inlines.Count;
            //}

            bool inCommand = CaretInCommand;

            switch (e.Key)
            {
                case Key.Tab:
                    OnTabPressed(e, inCommand);
                    break;

                case Key.F7:
                    OnHistoryMenu(e, inCommand);
                    break;

                case Key.Up:
                    OnUpPressed(e, inCommand);
                    break;

                case Key.Down:
                    OnDownPressed(e, inCommand);
                    break;

                case Key.Escape:
                    OnEscapePressed();
                    break;

                case Key.Enter:
                    OnEnterPressed(e);
                    break;

                case Key.Left:
                    OnLeftPressed(e);
                    break;

                case Key.Home:
                    OnHomePressed(e, inCommand);
                    break;

                case Key.End:
                    OnEndPressed(e);
                    break;

                case Key.Back:
                case Key.Delete:
                    OnBackspaceDeletePressed(e);
                    break;

                //// we're only handling this to avoid the default handler when you try to copy
                //// since that would de-select the text
                //case Key.X:
                //case Key.C:
                //    if (!Utilities.IsModifierOn(e, ModifierKeys.Control))
                //    {
                //        goto default;
                //    }

                //    break;

                case Key.PageUp:
                    OnPageUpPressed(e, inCommand);
                    break;

                case Key.PageDown:
                    OnPageDownPressed(e, inCommand);
                    break;

                // here's a few keys we want the base control to handle:
                case Key.Right:
                case Key.RightAlt:
                case Key.LeftAlt:
                case Key.RightCtrl:
                case Key.LeftCtrl:
                case Key.RightShift:
                case Key.LeftShift:
                case Key.RWin:
                case Key.LWin:
                case Key.CapsLock:
                case Key.Insert:
                case Key.NumLock:
                    break;

                // if a key isn't in the list above, then make sure we're in the prompt before we let it through
                default:

                    _expansion.Reset();

                    if (_commandStart == null || !inCommand)
                    {
                        CaretPosition = Document.ContentEnd.GetInsertionPosition(LogicalDirection.Forward);
                    }

                    // if they type anything, they're no longer using the autocopy
                    _autoCopy = false;
                    break;

            }

            base.OnPreviewKeyDown(e);

            Trace.Unindent();
            Trace.TraceInformation("Exiting OnPreviewKeyDown:");
        }


        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            // if they're trying to input text, they will overwrite the selection
            // lets make sure they don't overwrite the history buffer

            if (!Selection.IsEmpty)
            {
                RestrictSelectionToInputArea();
            }

            base.OnPreviewTextInput(e);
        }

        private void OnBackspaceDeletePressed(KeyEventArgs e)
        {
            if (!Selection.IsEmpty)
            {
                RestrictSelectionToInputArea();
                Selection.Text = string.Empty;
                e.Handled = true;
            }
            else
            {
                int offset = _commandStart.GetInsertionPosition(LogicalDirection.Forward).GetOffsetToPosition(CaretPosition);
                e.Handled = (offset < 0 || (offset == 0 && e.Key == Key.Back) || (CurrentCommand.Length <= 0));
            }
        }

        private void OnEndPressed(KeyEventArgs e)
        {
            if (Utilities.IsModifierOn(e, ModifierKeys.Control) &&
                Utilities.IsModifierOn(e, ModifierKeys.Shift))
            {
                Selection.Select(Selection.Start, CaretPosition.DocumentEnd);
            }

            else if (Utilities.IsModifierOn(e, ModifierKeys.Control))
            {
                CaretPosition = CaretPosition.DocumentEnd;
            }

            else if (Utilities.IsModifierOn(e, ModifierKeys.Shift))
            {
                TextPointer nextLine = (CaretPosition.GetLineStartPosition(1) ?? CaretPosition.DocumentEnd);
                Selection.Select(Selection.Start, nextLine.GetInsertionPosition(LogicalDirection.Backward));
            }

            else
            {
                TextPointer nextLine = (CaretPosition.GetLineStartPosition(1) ?? CaretPosition.DocumentEnd);
                CaretPosition = nextLine.GetNextInsertionPosition(LogicalDirection.Backward);
            }

            e.Handled = true;
        }

        private void OnEnterPressed(KeyEventArgs e)
        {
            ClearUndoBuffer();

            // the "EndOfOutput" marker gets stuck just before the last character of the prompt...
            string cmd = CurrentCommand;

            _currentParagraph.ContentEnd.InsertLineBreak();

            _cmdHistory.Reset();
            _cmdHistory.Add(cmd);
            OnCommand(cmd);

            e.Handled = true;
        }


        private void OnEscapePressed()
        {
            _cmdHistory.ResetCurrentCommand();
            CurrentCommand = string.Empty;
        }

        private void OnHomePressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt)
            {
                if (Utilities.IsModifierOn(e, ModifierKeys.Shift))
                {
                    Selection.Select(CaretPosition, _commandStart.GetNextInsertionPosition(LogicalDirection.Forward));
                }

                else if (Utilities.IsModifierOn(e, ModifierKeys.Control))
                {
                    CaretPosition = Document.ContentStart;
                }

                else
                {
                    CaretPosition = _commandStart.GetNextInsertionPosition(LogicalDirection.Forward);
                }

                e.Handled = true;
            }
        }

        private void OnLeftPressed(KeyEventArgs e)
        {
            // cancel the "left" if we're at the left edge of the prompt
            if (_commandStart.GetNextInsertionPosition(LogicalDirection.Forward).GetOffsetToPosition(CaretPosition) >= 0 &&
                _commandStart.GetNextInsertionPosition(LogicalDirection.Forward).GetOffsetToPosition(CaretPosition.GetInsertionPosition(LogicalDirection.Backward)) < 0)
            {
                e.Handled = true;
            }
        }

        private void OnDownPressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt)
            {
                CurrentCommand = _cmdHistory.Next(CurrentCommand);
                e.Handled = true;
            }
        }

        private void OnUpPressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt && (!Utilities.IsModifierOn(e, ModifierKeys.Control)))
            {
                CurrentCommand = _cmdHistory.Previous(CurrentCommand);
                e.Handled = true;
            }
        }

        private DateTime _tabTime = DateTime.Now;
        private void OnTabPressed(KeyEventArgs e, bool inPrompt)
        {
            if (!Utilities.IsModifierOn(e, ModifierKeys.Control))
            {
                if (inPrompt)
                {
                    List<string> choices = _expansion.GetChoices(CurrentCommand);

                    if( (Properties.Settings.Default.TabCompleteMenuThreshold > 0
                        && choices.Count > Properties.Settings.Default.TabCompleteMenuThreshold)
                    || (((TimeSpan)(DateTime.Now - _tabTime)).TotalMilliseconds < Properties.Settings.Default.TabCompleteDoubleTapMilliseconds))
                    {
                        _popup.ShowTabPopup(new Rect(CursorPosition.X, CursorPosition.Y, ActualWidth - CursorPosition.X, ActualHeight - CursorPosition.Y), choices, CurrentCommand);
                    }
                    else
                    {
                        if (Utilities.IsModifierOn(e, ModifierKeys.Shift))
                        {
                            CurrentCommand = _expansion.Previous(CurrentCommand);
                        }
                        else
                        {
                            CurrentCommand = _expansion.Next(CurrentCommand);
                        }
                    }
                }
                _tabTime = DateTime.Now;
                e.Handled = true;
            }
        }

        private void OnHistoryMenu(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt && !IsScrollLockToggled(e.KeyboardDevice))
            {
                _popup.ShowHistoryPopup(new Rect(CursorPosition.X, CursorPosition.Y, ActualWidth - CursorPosition.X, ActualHeight - CursorPosition.Y), _cmdHistory.GetChoices(CurrentCommand));
                e.Handled = true;
            }
        }


        private void OnPageDownPressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt && !IsScrollLockToggled(e.KeyboardDevice))
            {
                CurrentCommand = _cmdHistory.Last(CurrentCommand);
                e.Handled = true;
            }
        }

        private void OnPageUpPressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt && !IsScrollLockToggled(e.KeyboardDevice))
            {
                CurrentCommand = _cmdHistory.First(CurrentCommand);
                e.Handled = true;
            }
        }

        private void OnDataObjectPasting(object sender, DataObjectPastingEventArgs e)
        {
            //// ... this would let us parse the new text after the paste operation
            //this.selectionStart = this.Selection.Start;
            //this.selectionEnd = this.Selection.IsEmpty ?
            //    this.Selection.End.GetPositionAtOffset(0, LogicalDirection.Forward) :
            //    this.Selection.End;
            e.FormatToApply = "UnicodeText";
        }

        private bool IsScrollLockToggled(KeyboardDevice keyboard)
        {
            return keyboard.IsKeyToggled(Key.Scroll);
        }

        private void RestrictSelectionToInputArea()
        {
            TextPointer inputAreaIP = _commandStart.GetNextInsertionPosition(LogicalDirection.Forward);

            if (Selection.Start.GetOffsetToPosition(inputAreaIP) >= 0)
            {
                Selection.Select(inputAreaIP, Selection.End);
            }

            if (Selection.End.GetOffsetToPosition(inputAreaIP) >= 0)
            {
                Selection.Select(Selection.Start, inputAreaIP);
            }
        }
    }

}