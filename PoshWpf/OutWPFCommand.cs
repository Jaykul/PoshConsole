using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml;

namespace PoshWpf
{
    [Cmdlet(VerbsData.Out, "WPF", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
    public class OutWPFCommand : WpfCommandBase
	{
        private ItemsControl _host = null;
        // private Huddled.WPF.Controls.Interfaces.IPSXamlConsole xamlUI;
        // private FlowDocument _document = null;

		#region Methods
		protected override void BeginProcessing()
		{
			base.BeginProcessing();
		}

		protected override void ProcessRecord()
		{
			_dispatcher.Invoke((Action)(() =>
			{
                if (Content != null)
                {
                    object output = Content.BaseObject;

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


                        if (output is Block)
                        {
                            doc.Blocks.InsertAfter(par, (Block)output);
                        }
                        else
                        {
                            par.Inlines.Add((Inline)output);
                        }
                    }
                    else if(output is Panel)
                    {
                         _window.Content = output;
                    } 
                    else 
                    {
                        if (_host == null) { _host = XamlHelper.NewItemsControl(); }
                        if (_window == null && _host.Parent == null)
                        {
                            _xamlUI.CurrentBlock.Inlines.Add(new InlineUIContainer(_host));
                        }
                        else if(_host.Items.Count == 0 )
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
			}));
		}

		#endregion

		protected override void EndProcessing()
		{
			// release only in OutWPFCommand
			//if (_windowCount == 0)
			//{
				_window = null;
				_dispatcher = null;
				_xamlUI = null;
			//}
			base.EndProcessing();
		}
	}
}
