using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Markup;

namespace PoshConsole.Controls
{
    partial class ConsoleRichTextBox
    {
        
		#region [rgn] Methods (13)

		// [rgn] Public Methods (1)

		//<Floater Margin="0,0,0,0" Padding="0,0,0,0" HorizontalAlignment="Left">
        //	<Paragraph>
        //		<Image Source="http://blogs.msdn.com/photos/llester/images/1566947/original.aspx" SnapsToDevicePixels="True" Stretch="None"/>
        //	</Paragraph>
        //</Floater>
        public override void EndInit()
        {
            base.EndInit();
            // LOAD the startup banner only when it's set (instead of removing it after)
            if (Properties.Settings.Default.StartupBanner && System.IO.File.Exists("StartupBanner.xaml"))
            {
                try
                {
                    Paragraph banner = (Paragraph)XamlReader.Load(System.Xml.XmlReader.Create("StartupBanner.xaml"));
                    if (banner != null)
                    {
                        // Copy over *all* resources from the DOCUMENT to the BANNER
                        // NOTE: be careful not to put resources in the document you're not willing to expose
                        // NOTE: this will overwrite resources with matching keys, so banner-makers need to be aware
                        foreach (string key in Document.Resources.Keys)
                        {
                            banner.Resources[key] = Document.Resources[key];
                        }
                        banner.Padding = new Thickness(5);
                        Document.Blocks.Add(banner);
                        _currentParagraph = new Paragraph();
                        _currentParagraph.ClearFloaters = WrapDirection.Both;
                        Document.Blocks.Add(_currentParagraph);
                    }
                    else
                    {
                        Write(Foreground, _consoleBrushes.Transparent, "PoshConsole 1.0.2007.8170");
                    }

                    // Document.Blocks.InsertBefore(Document.Blocks.FirstBlock, new Paragraph(new Run("PoshConsole`nVersion 1.0.2007.8150")));
                    // Document.Blocks.AddRange(LoadXamlBlocks("StartupBanner.xaml"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(@"Problem loading StartupBanner.xaml\n{0}", ex.Message);
                    Document.Blocks.Clear();
                    Write(Foreground, _consoleBrushes.Transparent, "PoshConsole 1.0.2007.8300");
                }
            }
            else
            {
                Write(Foreground, _consoleBrushes.Transparent, "PoshConsole 1.0.2007.8300");
            }

            ClearUndoBuffer();
        }
		
		// [rgn] Protected Methods (7)

		protected override void OnGotFocus(RoutedEventArgs e)
        {
            CaretPosition = Document.ContentEnd;
            base.OnGotFocus(e);
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


        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            Trace.TraceInformation("Entering OnPreviewKeyUp:");
            Trace.Indent();
            Trace.WriteLine("Event:  " + e.RoutedEvent);
            Trace.WriteLine("Key:    " + e.Key + " " + System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key));
            Trace.WriteLine("Now!:   " + DateTime.Now.ToString("hh:mm:ss.fff"));

            //Trace.WriteLine("Source: " + e.Source);

            base.OnPreviewKeyUp(e);

            Trace.Unindent();
            Trace.TraceInformation("Exiting OnPreviewKeyUp:");
        }


		protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            Trace.TraceInformation("Entering OnPreviewKeyDown:");
            Trace.Indent();
            Trace.WriteLine("Event:  " + e.RoutedEvent);
            Trace.WriteLine("Key:    " + e.Key);
            Trace.WriteLine("Now!:   " + DateTime.Now.ToString("hh:mm:ss.fff"));

            //Trace.WriteLine("Source: " + e.Source);

            if (_promptEnd == null || e.Source != this)
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

                case Key.Left:
                    OnLeftPressed(e, inCommand);
                    break;

                case Key.Enter:
                    OnEnterPressed(e);
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

                // we're handling this to avoid the default handler when you try to copy since that 
                // would de-select the text -- and result in copying the whole (first) paragraph
                case Key.X:
                case Key.C:
                    if (!Utilities.IsModifierOn(e, ModifierKeys.Control))
                    {
                        goto default;
                    }
                    break;

                case Key.PageUp:
                    OnPageUpPressed(e, inCommand);
                    break;

                case Key.PageDown:
                    OnPageDownPressed(e, inCommand);
                    break;

                // here's a few keys we want the base control to handle:
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

                    if (_promptEnd == null || !inCommand)
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
		
		protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            Trace.TraceInformation("Entering OnPreviewTextInput:");
            Trace.Indent();
            Trace.WriteLine("Event:  " + e.RoutedEvent);
            Trace.WriteLine("Text:   " + e.Text);
            Trace.WriteLine("Now!:   " + DateTime.Now.ToString("hh:mm:ss.fff"));

            //Trace.WriteLine("Source: " + e.OriginalSource);

            // if they're trying to input text, they will overwrite the selection
            // lets make sure they don't overwrite the history buffer

            if (!Selection.IsEmpty)
            {
                RestrictSelectionToInputArea();
            }

            base.OnPreviewTextInput(e);
            Trace.Unindent();
            Trace.TraceInformation("Exiting OnPreviewTextInput");
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
		
		// [rgn] Private Methods (5)

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

                // for "Delete" it would be enough to test "CaretInCommand" but for backspace we need to know if they're on the end
                int offset = CommandStart.GetOffsetToPosition(CaretPosition);
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

            if (!string.IsNullOrEmpty(cmd))
            {
                _cmdHistory.Add(cmd);
            }

            OnCommand(cmd);

            e.Handled = true;
        }
		
		private void OnHomePressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt)
            {
                if (Utilities.IsModifierOn(e, ModifierKeys.Shift))
                {
                    Selection.Select(CaretPosition, CommandStart);
                }

                else if (Utilities.IsModifierOn(e, ModifierKeys.Control))
                {
                    CaretPosition = Document.ContentStart;
                }

                else
                {
                    CaretPosition = CommandStart;
                }

                e.Handled = true;
            }
        }
		
		private void OnLeftPressed(KeyEventArgs e, bool inPrompt)
        {
            if( CommandStart.GetOffsetToPosition(CaretPosition.GetInsertionPosition(LogicalDirection.Backward)) == 0
            //if(inPrompt ||
             && !Utilities.IsModifierOn(e, ModifierKeys.Control) 
             && !IsScrollLockToggled(e.KeyboardDevice))
            {
            //// cancel the "left" if we're at the left edge of the prompt
            //if (CommandStart.GetOffsetToPosition(CaretPosition) >= 0 &&
            //     <= 0)
            //{
                e.Handled = true;
            }
        }

        private void OnDownPressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt
            && !Utilities.IsModifierOn(e, ModifierKeys.Control)
            && !IsScrollLockToggled(e.KeyboardDevice))
            {
                CurrentCommand = _cmdHistory.Next(CurrentCommand);
                e.Handled = true;
            }
        }

        private void OnUpPressed(KeyEventArgs e, bool inPrompt)
        {
            if (inPrompt 
            && !Utilities.IsModifierOn(e, ModifierKeys.Control) 
            && !IsScrollLockToggled(e.KeyboardDevice))
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
            //if (inPrompt && !IsScrollLockToggled(e.KeyboardDevice))
            //{
                _popup.ShowHistoryPopup(new Rect(CursorPosition.X, CursorPosition.Y, ActualWidth - CursorPosition.X, ActualHeight - CursorPosition.Y), _cmdHistory.GetChoices(CurrentCommand));
                e.Handled = true;
            //}
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
            TextPointer inputAreaIP = CommandStart;

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
		
		#endregion [rgn]

}