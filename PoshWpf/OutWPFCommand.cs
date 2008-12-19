using System;
using System.IO;
using System.Management.Automation;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Management.Automation.Host;
using System.Windows.Threading;

namespace PoshWpf
{
	[Cmdlet(VerbsData.Out, "WPF", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
	public class OutWPFCommand : PSCmdlet, IDynamicParameters
	{
        #region Parameters
        [Parameter(
            Position = 0,
            //           ParameterSetName = "Input",
            Mandatory = true,
            ValueFromPipeline = true,
            HelpMessage = "Data to bind to the wpf control")]
        public PSObject InputObject { get; set; }

        [Parameter(
            Position = 1,
            ParameterSetName = "FileTemplate",
            Mandatory = true,
            ValueFromPipeline = false,
            HelpMessage = "XAML template file")]
        public FileInfo FileTemplate { get; set; }

        [Parameter(
            Position = 1,
            ParameterSetName = "SourceTemplate",
            Mandatory = true,
            ValueFromPipeline = false,
            HelpMessage = "XAML template XmlDocument")]
        [Alias("Template")]
        public XmlDocument SourceTemplate { get; set; }
        #endregion

        private WPFParameters _dynamicProviders = null;
        #region IDynamicParameters
		internal class WPFParameters
		{
			#region Parameters
			[Parameter(
				Position = 2,
				Mandatory = false,
				ValueFromPipeline = false,
				HelpMessage = "Show in popup Window")]
			public SwitchParameter Popup { get; set; }
			#endregion
		}

		public object GetDynamicParameters()
		{
			if (Host is IPSWpfHost)
			{
                _dynamicProviders = new WPFParameters();
            }
			return _dynamicProviders;
		}
		#endregion IDynamicParameters

		#region Methods

		// private Huddled.WPF.Controls.Interfaces.IPSXamlConsole xamlUI;
		// private FlowDocument _document = null;
		private FrameworkElement _element = null;
		private IPSWpfConsole _xamlUI = null;
		private ItemsControl _host = null;
		private Window _window = null;
		private Dispatcher _dispatcher = null;


		protected override void BeginProcessing()
		{
			XmlDocument document = GetXaml();

			if (Host is IPSWpfHost)
			{
				_xamlUI = ((IPSWpfHost)Host).GetWpfConsole();
                if (_dynamicProviders != null  && _dynamicProviders.Popup.ToBool())
				{
					_host = XamlHelper.NewItemsControl();
					_window = new Window
					{
						WindowStyle = WindowStyle.ToolWindow,
						Content = _host
					};
					_xamlUI.PopoutWindows.Add(_window);
					_window.Show();
					_dispatcher = _window.Dispatcher;
				}
				else
				{
					_window = _xamlUI.RootWindow;
					_dispatcher = _xamlUI.Dispatcher;
				}
			}
			else
			{
				var result = Presentation.Start(document);
				_window = result.Window;
				_dispatcher = result.Dispatcher;
			}


			ErrorRecord error = (ErrorRecord)_dispatcher.Invoke((Func<ErrorRecord>)(() =>
			{
				ErrorRecord err = null;
				document.TryLoadXaml(out _element, out err);

				if (_element is Window)
				{
					_window = (Window)_element;
					_window.Show();
					_dispatcher = _window.Dispatcher;
				}
				return err;
			}));
			if (error != null) { WriteError(error); }

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
						} else {
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
				default:
					{
						throw new InvalidOperationException("Bad Parameter Set");
					}
			}		
			return document;
		}


		protected override void ProcessRecord()
		{
			object output = InputObject.BaseObject;
			_dispatcher.BeginInvoke((Action)(() =>
			{
				// If the output is something that goes in a document ....
				// Then we need to ditch our NewItemsControl and use a FlowDocumentScrollViewer
				if (output is Block || output is Inline)
				{
					Paragraph par;
					FlowDocument doc;
					// if we're dong a XamlUI Popup window (or are not in XamlUI) ...
					if (_window != null)
					{
						par = new Paragraph();
						doc = new FlowDocument(par);
						if( _xamlUI != null ) {
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


					if (output is Block)
					{
						doc.Blocks.InsertAfter(par, (Block)output);
					}
					else
					{
						par.Inlines.Add((Inline)output);
					}
				}
				else
				{
					if (_host == null)
					{
						_host = XamlHelper.NewItemsControl();
					}
					if (_window == null)
					{
						_xamlUI.CurrentBlock.Inlines.Add(new InlineUIContainer(_host));
					}
					else
					{
						// TODO: use some template-based magic 
						// to FIND a predefined ItemsControl, 
						// or the place where they want the ItemsControl
                        _window.Content = _host.Items.Add( _window.Content );
					}

					if (_element != null)
					{
						var e = _element;
						e.DataContext = output;
						_host.Items.Add(e);
					}
					else
					{
						_host.Items.Add(output);
					}
				}
			}));
		}

		#endregion


	}
}
