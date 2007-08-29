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

namespace PoshConsole.Controls
{
    partial class ConsoleRichTextBox
    {
        
		#region [rgn] Methods (13)

		// [rgn] Private Methods (13)

		/// <summary>
        /// A handler for the Application.Stop event...
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private static void OnApplicationStop(object sender, ExecutedRoutedEventArgs e)
        {
            ConsoleRichTextBox control = (ConsoleRichTextBox)sender;
            if (!control.IsRunning)
            {
                control.History.ResetCurrentCommand();
                control.CurrentCommand = string.Empty;
                e.Handled = true;
            }
            ApplicationCommands.Stop.Execute(null, (IInputElement)control.Parent);
        }
		
		private static void OnCanDecreaseZoom(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = ((ConsoleRichTextBox)(target)).FontSize > 1;
        }
		
		private static void OnCanExecuteCopy(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = ((TextBoxBase)(target)).IsEnabled;
        }
		
		private static void OnCanExecuteCut(object target, CanExecuteRoutedEventArgs args)
        {
            ConsoleRichTextBox box = (ConsoleRichTextBox)(target);
            args.CanExecute = box.IsEnabled && !box.IsReadOnly && !box.Selection.IsEmpty;
        }
		
		private static void OnCanExecutePaste(object target, CanExecuteRoutedEventArgs args)
        {
            ConsoleRichTextBox box = (ConsoleRichTextBox)target;
            args.CanExecute = box.IsEnabled && !box.IsReadOnly && Clipboard.ContainsText();
        }
		
		private static void OnCanIncreaseZoom(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }
		
		private static void OnCopy(object sender, ExecutedRoutedEventArgs e)
        {
            ((ConsoleRichTextBox)sender).Copy();
            e.Handled = true;
        }
		
		private static void OnCut(object sender, ExecutedRoutedEventArgs e)
        {
            // Clipboard.SetText(((ConsoleRichTextBox)sender).Selection.Text);
            // ((ConsoleRichTextBox)sender).Selection.Text = String.Empty;

            // TODO: allow cut when on the "command" line, otherwise copy
            ((ConsoleRichTextBox)sender).Copy();
            e.Handled = true;
        }
		
		private static void OnDecreaseZoom(object sender, ExecutedRoutedEventArgs e)
        {
            ConsoleRichTextBox box = ((ConsoleRichTextBox)(sender));

            if (box.FontSize > 1)
            {
                box.FontSize -= 1.0;
            }

            e.Handled = true;
        }
		
		private static void OnIncreaseZoom(object sender, ExecutedRoutedEventArgs e)
        {
            ((ConsoleRichTextBox)(sender)).FontSize += 1.0;
        }
		
		private static void OnPaste(object sender, ExecutedRoutedEventArgs e)
        {
            ConsoleRichTextBox box = (ConsoleRichTextBox)sender;

            if (Clipboard.ContainsText())
            {
                // TODO: check if focus is in the "command" line already, if so, insert text at cursor
                if (box.CaretInCommand)
                {
                    // get a pointer with forward gravity so it will stick to the end of the paste. I don't know why you have to use the Offset instead of insertion...
                    TextPointer insert = box.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);// GetInsertionPosition(LogicalDirection.Forward);
                    // now insert the text and make sure the caret is where it belongs.
                    insert.InsertTextInRun(Clipboard.GetText(TextDataFormat.UnicodeText));
                    box.CaretPosition = insert;
                }
                else
                {
                    box.Document.ContentEnd.InsertTextInRun(Clipboard.GetText(TextDataFormat.UnicodeText));
                    box.CaretPosition = box.Document.ContentEnd;
                }
            }

            box.ScrollToEnd();
            e.Handled = true;
        }
		
		private static void OnZoom(object sender, ExecutedRoutedEventArgs e)
        {
            ConsoleRichTextBox control = (ConsoleRichTextBox)sender;

            if (e.Parameter is double)
            {
                control.SetZoomFactor((double)(e.Parameter));
                return;
            }

            string parameter = e.Parameter as string;
            double zoom = 1.0;

            if (parameter != null && double.TryParse(parameter, out zoom))
            {
                control.SetZoomFactor(zoom);
                return;
            }
        }
		
		private void SetZoomFactor(double zoom)
        {            
            FontSize = Math.Max(1.0, zoom * Properties.Settings.Default.FontSize);
        }
		
		#endregion [rgn]

    }
}