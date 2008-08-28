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
using Huddled.WPF.Controls.Interfaces;
using Huddled.WPF.Controls.Utility;

namespace Huddled.WPF.Controls
{
    partial class ConsoleControl : IPSXamlConsole
    {
        #region IPSXamlConsole Members
        void IPSXamlConsole.OutXaml(XmlDocument source)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
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
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
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
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
            {
                try
                {
                    object loaded = XamlReader.Load(new XmlNodeReader(source));
                    if (loaded is FrameworkElement)
                    {
                        InlineUIContainer iui = new InlineUIContainer
                                                   {
                                                      Child = ((FrameworkElement) loaded),
                                                      DataContext = data.BaseObject
                                                   };
                       _current.Inlines.Add(iui);

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
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
            {
                ErrorRecord error;
                FrameworkElement element;
                if (source.TryLoadXaml( out element, out error))
                {
                    InlineUIContainer iui = new InlineUIContainer {Child = element, DataContext = data.BaseObject};// ((UIElement)fromXaml);
                   _current.Inlines.Add(iui);
                }
                else
                {
                    ((IPSConsole)this).WriteErrorRecord(error);
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
                _current.Inlines.Add((Inline)fromXaml);
            }
            else if (fromXaml is Paragraph)
            {
                _current.Inlines.AddRange(((Paragraph)fromXaml).Inlines);
            }
            else
            {
                ((IPSConsole)this).WriteErrorLine("I'm not sure how to load the ContentElement");
                ((IPSConsole)this).WriteErrorLine(fromXaml.GetType().FullName + ", " + fromXaml.GetType().BaseType.FullName);
            }
        }
        private void OutXamlElement(FrameworkElement fromXaml)
        {
           _current.Inlines.Add(fromXaml);
        }

        #endregion
    }
}