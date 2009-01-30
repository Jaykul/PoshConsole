using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Windows;
using System.IO;
using System.Xml;
using System.Windows.Threading;

namespace PoshWpf
{
    public abstract class WpfCommandBase : PSCmdlet
    {
        #region Parameters

        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = true,
            HelpMessage = "Data to bind to the wpf control")]
        public PSObject Content { get; set; }

        // TODO!
        [Parameter(
            Position = 10,
            ParameterSetName = "FileTemplate",
            Mandatory = true,
            ValueFromPipeline = false,
            HelpMessage = "XAML template file")]
        public FileInfo FileTemplate { get; set; }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        [Parameter(
            Position = 10,
            ParameterSetName = "SourceTemplate",
            Mandatory = true,
            ValueFromPipeline = false,
            HelpMessage = "XAML template XmlDocument")]
        [Alias("Template")]
        public XmlDocument SourceTemplate { get; set; }
        #endregion

        [Parameter(
            Position = 20,
            Mandatory = false,
            ValueFromPipeline = false,
            HelpMessage = "Show in popup Window")]
        public SwitchParameter Popup { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        protected static Window _window;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        protected static Dispatcher _dispatcher;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        protected static IPSWpfConsole _xamlUI;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        protected static int _windowCount;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected XmlDocument _template;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected FrameworkElement _element;

        protected override void BeginProcessing()
        {
            if (_window == null)
            {
                _template = GetXaml();

                if (Host.PrivateData.BaseObject is IPSWpfOptions)
                {
                    //((IPSWpfHost)).GetWpfConsole();
                    _xamlUI = ((IPSWpfOptions)Host.PrivateData.BaseObject).WpfConsole;
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
							  var result = Presentation.Start(_template, null);
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
						 var result = Presentation.Start(_template, null);
                    _window = result.Window;
                    _dispatcher = result.Dispatcher;
                }

                if (_template != null)
                {
                    ErrorRecord error = (ErrorRecord)_dispatcher.Invoke((Func<ErrorRecord>)(() =>
                    {
                        ErrorRecord err = null;
                        _template.TryLoadXaml(out _element, out err);

                        if (_element is Window)
                        {
                            if (Host is IPSWpfHost)
                            {
                                _window = (Window)_element;
                                _window.Show();
                                _dispatcher = _window.Dispatcher;
                            }
                            _element = null;
                        }
                        return err;
                    }));
                    if (error != null) { WriteError(error); }
                }
            }
            // internal reference count
            _windowCount++;
            base.BeginProcessing();
        }

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
                            string templates = Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "XamlTemplates");
                            string template = null;
                            // try to magically resolve the file
                            template = System.IO.Path.Combine(base.CurrentProviderLocation("FileSystem").Path, FileTemplate.Name);
                            if (File.Exists(template))
                            {
                                FileTemplate = new FileInfo(template);
                            }
                            else
                            {
                                template = Path.Combine(templates, FileTemplate.Name);
                                if (File.Exists(template))
                                {
                                    document = new XmlDocument();
                                    document.Load(template);
                                }
                                else
                                {
                                    throw new FileNotFoundException("Can't find the template file.  There is currently no default template location, so you must specify the path to the template file.", template);
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

        protected override void EndProcessing()
        {
			  // reference count
			  _windowCount--;
            base.EndProcessing();
        }

    }
}
