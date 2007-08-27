using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using System.IO;
using System.Windows.Controls;
using System.Management.Automation;

namespace Huddled.PoshConsole
{
    partial class ConsoleRichTextBox : IPSXamlConsole
    {
        #region IPSXamlConsole Members

        void IPSXamlConsole.OutXaml(XmlDocument source)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (BeginInvoke)delegate
            {
                try {
                    OutXamlObject(XamlReader.Load(new XmlNodeReader(source)));
                }
                catch(Exception ex)
                {
                    ((IPSConsole)this).WriteErrorRecord( new ErrorRecord( ex, "Loading Xaml", ErrorCategory.SyntaxError, source));
                }
            });
        }

        void IPSXamlConsole.OutXaml(FileInfo source)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (BeginInvoke)delegate
            {
                try
                {
                    OutXamlObject(XamlReader.Load(source.OpenRead()));
                }
                catch (Exception ex)
                {
                    ((IPSConsole)this).WriteErrorRecord(new ErrorRecord(ex, "Loading Xaml", ErrorCategory.SyntaxError, source));
                }
            });
        }

        void IPSXamlConsole.OutXaml(XmlDocument source, PSObject data)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (BeginInvoke)delegate
            {
                try
                {
                    object loaded = XamlReader.Load(new XmlNodeReader(source));
                    if (loaded is FrameworkElement)
                    {
                        InlineUIContainer iui = new InlineUIContainer();// ((UIElement)fromXaml);
                        iui.DataContext = data.BaseObject;
                        iui.Child = (FrameworkElement)loaded;
                        _currentParagraph.Inlines.Add(iui);
                        //_currentParagraph.Inlines.Add(iui);
                        //((FrameworkElement)loaded).DataContext
                        //OutXamlObject(((FrameworkElement)loaded));
                    } else {
                        ((IPSConsole)this).WriteErrorRecord(new ErrorRecord(new ArgumentException("XmlDocument doesn't yield FrameworkElement","source"), "Can't DataBind", ErrorCategory.MetadataError, loaded));
                    }
                }
                catch (Exception ex)
                {
                    ((IPSConsole)this).WriteErrorRecord(new ErrorRecord(ex, "Loading Xaml", ErrorCategory.SyntaxError, source));
                }
            });
        }

        void IPSXamlConsole.OutXaml(FileInfo source, PSObject data)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (BeginInvoke)delegate
            {
                try
                {
                    object loaded = XamlReader.Load(source.OpenRead());
                    if (loaded is FrameworkElement)
                    {
                        InlineUIContainer iui = new InlineUIContainer();// ((UIElement)fromXaml);
                        iui.DataContext = data;
                        iui.Child = (FrameworkElement)loaded;
                        _currentParagraph.Inlines.Add(iui);
                        //((FrameworkElement)loaded).DataContext = data;
                        //OutXamlObject(((FrameworkElement)loaded));
                    } else {
                        ((IPSConsole)this).WriteErrorRecord(new ErrorRecord(new ArgumentException("XmlDocument doesn't yield FrameworkElement", "source"), "Can't DataBind", ErrorCategory.MetadataError, loaded));
                    }
                }
                catch (Exception ex)
                {
                    ((IPSConsole)this).WriteErrorRecord(new ErrorRecord(ex, "Loading Xaml", ErrorCategory.SyntaxError, source));
                }
            });
        }

        
        private void OutXamlObject(object fromXaml) {
            if (fromXaml is ContentElement)
            { 
                OutXamlElement((ContentElement)fromXaml);
            }
            else if (fromXaml is FrameworkElement)
            {
                OutXamlElement((FrameworkElement)fromXaml); 
            }
        }

        private void OutXamlElement(ContentElement fromXaml)
        {
            if( fromXaml is Inline )
            {
                _currentParagraph.Inlines.Add((Inline)fromXaml);
            }
            else if (fromXaml is Paragraph)
            {
                _currentParagraph.Inlines.AddRange(((Paragraph)fromXaml).Inlines);
            }
            else
            {
                ((IPSConsole)this).WriteErrorLine("I'm not sure how to load the ContentElement");
                ((IPSConsole)this).WriteErrorLine(fromXaml.GetType().FullName + ", " + fromXaml.GetType().BaseType.FullName);
            }
        }

        private void OutXamlElement(FrameworkElement fromXaml)
        {
            if (fromXaml is UIElement)
            {
                //InlineUIContainer iui = new InlineUIContainer((UIElement)fromXaml);
                //_currentParagraph.Inlines.Add(iui);
                _currentParagraph.Inlines.Add((UIElement)fromXaml);
                //System.Windows.Documents.Run r = new System.Windows.Documents.Run( new String((char)160,2), _currentParagraph.ContentEnd);
                //Write(null, null, " \n");
            }
            else
            {
                ((IPSConsole)this).WriteErrorLine("Couldn't figure out how to load the element");
                ((IPSConsole)this).WriteErrorLine(fromXaml.GetType().FullName + ", " + fromXaml.GetType().BaseType.FullName);
            }
        }

        #endregion
    }
}