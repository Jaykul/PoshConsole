using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Huddled.PoshConsole
{
    partial class ConsoleRichTextBox : IPSXamlConsole
    {
        #region IPSXamlConsole Members

        void IPSXamlConsole.WriteXaml(string xamlSource)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (BeginInvoke)delegate
            {
                object wpf;
                string error;
                if (XamlHelper.TryLoadFromSource<object>(xamlSource, out wpf, out error))
                {
                    LoadXamlObject(wpf);
                }
                else
                {
                    ((IPSConsole)this).WriteErrorLine(error);
                }
            });
        }

        void IPSXamlConsole.LoadXaml(string sourceUri)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (BeginInvoke)delegate
            {
                object wpf;
                string error;
                if (XamlHelper.TryLoadFromFile<object>(sourceUri, out wpf, out error))
                {
                    LoadXamlObject(wpf);
                }
                else
                {
                    ((IPSConsole)this).WriteErrorLine(error);
                }
            });
        }

        private void LoadXamlObject(object fromXaml)
        {
            if (fromXaml is Inline)
            {
                _currentParagraph.Inlines.Add((Inline)fromXaml);
            }
            else if (fromXaml is UIElement)
            {
                //Floater f = new Floater(new Paragraph(new InlineUIContainer((UIElement)fromXaml)));
                _currentParagraph.Inlines.Add((UIElement)fromXaml);
                //System.Windows.Documents.Run r = new System.Windows.Documents.Run( new String((char)160,2), _currentParagraph.ContentEnd);
                //Write(null, null, " \n");
            }
            else if (fromXaml is string)
            {
                _currentParagraph.Inlines.Add((string)fromXaml);
            }
            else if (fromXaml is Paragraph)
            {
                _currentParagraph.Inlines.AddRange(((Paragraph)fromXaml).Inlines);
            }
        }

        #endregion
    }
}