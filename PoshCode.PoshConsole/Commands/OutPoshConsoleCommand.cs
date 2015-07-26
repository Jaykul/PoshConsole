using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml;
using PoshCode;
using PoshCode.PowerShell;
using PoshCode.Wpf;
using PoshWpf.Utility;

namespace PoshWpf.Commands
{
    [Cmdlet(VerbsData.Out, "PoshConsole", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
    public class OutPoshConsoleCommand : ScriptBlockBase
    {
        #region Parameters

        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = true,
            HelpMessage = "Data to bind to the wpf control")]
        public PSObject InputObject { get; set; }

        // TODO!
        [Parameter(
            Position = 10,
            ParameterSetName = "FileTemplate",
            Mandatory = true,
            ValueFromPipeline = false,
            HelpMessage = "XAML template file")]
        public FileInfo FileTemplate { get; set; }


        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        [Parameter(
            Position = 10,
            ParameterSetName = "SourceTemplate",
            Mandatory = true,
            ValueFromPipeline = false,
            HelpMessage = "XAML template XmlDocument")]
        [Alias("Template")]
        public XmlDocument SourceTemplate { get; set; }

        [Parameter(
            Position = 20,
            Mandatory = false,
            ValueFromPipeline = false,
            HelpMessage = "Show in popup Window")]
        public SwitchParameter Popup { get; set; }

        private static Window _window;
        private static Dispatcher _dispatcher;
        private static IRichConsole _xamlUI;
        private static int _windowCount;
        private XmlDocument _template;
        private FrameworkElement _element;
        #endregion
        private ItemsControl _host;
        // private Huddled.WPF.Controls.Interfaces.IPSXamlConsole xamlUI;
        // private FlowDocument _document = null;

        #region Methods
        protected override void BeginProcessing()
        {
            if (_window == null)
            {
                _template = GetXaml();

                if (Host.PrivateData != null && Host.PrivateData.BaseObject is Options)
                {
                    //((IPSWpfHost)).GetWpfConsole();
                    _xamlUI = ((Options)Host.PrivateData.BaseObject).WpfConsole as PoshConsole;
                    if (Popup.ToBool())
                    {
                        //_window = new Window
                        //{
                        //    WindowStyle = WindowStyle.ToolWindow,
                        //    // Content = XamlHelper.NewItemsControl()
                        //};
                        //_xamlUI.PopoutWindows.Add(_window);
                        //_window.Show();
                        //_dispatcher = _window.Dispatcher;

                        //////////////////////////////////////////////////////////////
                        // If they ask for a popup from a IPSWpfOptions, we still use the Presentation
                        // That way the threading "issues" are the same for both Wpf and non-Wpf hosts
                        // Otherwise we'd have to deal with two different realities
                        var result = Presentation.Start(_template, null, null);
                        _window = result.Window;
                        _dispatcher = result.Dispatcher;
                    }
                    else
                    {
                        //_window = _xamlUI.RootWindow;
                        _dispatcher = _xamlUI.Dispatcher;
                        _xamlUI.RootWindow.LoadTemplates();
                    }
                }
                else
                {
                    var result = Presentation.Start(_template, null, null);
                    _window = result.Window;
                    _dispatcher = result.Dispatcher;
                }

                if (_template != null)
                {
                    ErrorRecord error = _dispatcher.Invoke(() =>
                    {
                        ErrorRecord err;
                        _template.TryLoadXaml(out _element, out err);

                        var window = _element as Window;
                        if (window != null)
                        {
                            if (Host.PrivateData != null && Host.PrivateData.BaseObject is IPSWpfOptions)
                            {
                                _window = window;
                                _window.Show();
                                _dispatcher = _window.Dispatcher;
                            }
                            _element = null;
                        }
                        return err;
                    });
                    if (error != null) { WriteError(error); }
                }
            }
            // internal reference count
            _windowCount++;
            base.BeginProcessing();
        }
        protected override void ProcessRecord()
        {
            _dispatcher.Invoke(() =>
            {
                if (InputObject != null)
                {
                    object output = InputObject.BaseObject;

                    // If the output is something that goes in a document ....
                    // Then we need to ditch our NewItemsControl and use a FlowDocumentScrollViewer
                    if (output is Block || output is Inline)
                    {
                        DocumentOutput(output);
                    }
                    else if (output is Panel)
                    {
                        _window.Content = output;
                    }
                    else
                    {
                        if (_host == null)
                        {
                            _host = XamlHelper.NewItemsControl(); 
                            if (_xamlUI != null)
                            {
                                _xamlUI.ContentPanel.Children.Clear();
                            }
                            
                        }
                        if (_window == null && _host.Parent == null)
                        {
                            _xamlUI.ContentPanel.Children.Add(_host);
                        }
                        else if (_host.Items.Count == 0)
                        {
                            //    // TODO: use some template-based magic 
                            //    // to FIND a predefined ItemsControl, 
                            //    // or the place where they want the ItemsControl
                            //    //_window.Content
                            _window.Content = _host;//.Items.Add();
                        }

                        if (_element != null)
                        {
                            ErrorRecord err;
                            FrameworkElement el;
                            _template.TryLoadXaml(out el, out err);
                            el.DataContext = output;
                            _host.Items.Add(el);
                        }
                        else
                        {
                            _host.Items.Add(output);
                        }
                    }
                }
                else if (_element != null)
                {
                    if (_window == null)
                    {
                        _xamlUI.CurrentBlock.Inlines.Add(new InlineUIContainer(_element));
                    }
                    else
                    {
                        _window.Content = _element;
                    }
                }
            });
            WriteObject(InputObject);
        }
        protected override void EndProcessing()
        {
            // release only in OutWPFCommand
            //if (_windowCount == 0)
            //{
            _window = null;
            _dispatcher = null;
            _xamlUI = null;
            _windowCount--;
            //}
            base.EndProcessing();
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private XmlDocument GetXaml()
        {
            XmlDocument document = null;
            switch (ParameterSetName)
            {
                case "FileTemplate":
                    {
                        if (!FileTemplate.Exists)
                        {
                            #region roll 10 for a saving throw


                            // try to magically resolve the file
                            string template = Path.Combine(CurrentProviderLocation("FileSystem").Path, FileTemplate.Name);
                            if (File.Exists(template))
                            {
                                FileTemplate = new FileInfo(template);
                            }
                            else
                            {
                                var module = Process.GetCurrentProcess().MainModule;
                                if (module != null)
                                {
                                    string templates = Path.Combine(Path.GetDirectoryName(module.FileName), "XamlTemplates");
                                    template = Path.Combine(templates, FileTemplate.Name);
                                }
                                if (File.Exists(template))
                                {
                                    document = new XmlDocument();
                                    document.Load(template);
                                }
                                else
                                {
                                    throw new FileNotFoundException(
                                       "Can't find the template file.  There is currently no default template location, so you must specify the path to the template file.",
                                       template);
                                }

                            }

                            #endregion saving throw
                        }
                        else
                        {
                            document = new XmlDocument();
                            document.Load(FileTemplate.FullName);
                        }
                    }
                    break;
                case "SourceTemplate":
                    {
                        document = SourceTemplate;
                    }
                    break;
                case "DataTemplate":
                    {

                    } break;
                default:
                    {
                        throw new InvalidOperationException("Bad Parameter Set");
                    }
            }
            return document;
        }

        private static void DocumentOutput(object output)
        {

            Paragraph par;
            FlowDocument doc;
            // if we're dong a XamlUI Popup window (or are not in XamlUI) ...
            if (_window != null)
            {
                par = new Paragraph();
                doc = new FlowDocument(par);
                if (_xamlUI != null)
                {
                    par.Background = _xamlUI.CurrentBlock.Background;
                    par.Foreground = _xamlUI.CurrentBlock.Foreground;
                    doc.Background = _xamlUI.Document.Background;
                    doc.Foreground = _xamlUI.Document.Foreground;
                }

                _window.Content = new FlowDocumentScrollViewer
                {
                    Document = doc,
                    Width = _window.Width,
                    Height = _window.Height,
                    Margin = new Thickness(0),
                    Padding = new Thickness(0),
                    IsToolBarVisible = true
                };
            }
            else
            {
                par = _xamlUI.CurrentBlock;
                doc = _xamlUI.Document;
            }


            Block blockput = output as Block;
            if (blockput == null)
            {
                doc.Blocks.InsertAfter(par, blockput);
            }
            else
            {
                par.Inlines.Add((Inline)output);
            }
        }
        #endregion

    }
}
