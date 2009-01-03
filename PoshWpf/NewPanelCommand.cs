using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Management.Automation;

namespace PoshWpf
{
    public abstract class NewPanelCommand : NewWpfControlCommandBase
    {
        protected Panel hostPanel = null;
        protected ItemsControl items = null;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            hostPanel = CreatePanel();
        }
        /// <summary>
        /// This is the method that must be implemented by the specific implementations
        /// </summary>
        /// <returns></returns>
        protected abstract Panel CreatePanel();
        
        protected override void ProcessRecord()
        {
            _dispatcher.Invoke((Action)(() =>
            {
                if (Content != null)
                {
                    object output = Content.BaseObject;
                    // If the output is something that goes in a document ....
                    // Then we need to ditch our NewItemsControl and use a FlowDocumentScrollViewer
                    if (output is UIElement)
                    {
                        hostPanel.Children.Add(output as UIElement); 
                    }
                    else if (_element != null)
                    {
                        ErrorRecord err;
                        FrameworkElement el;
                        _template.TryLoadXaml(out el, out err);
                        el.DataContext = output;
                        hostPanel.Children.Add(el);
                    }
                    else
                    {
                        if(items == null) { 
                            items = XamlHelper.NewItemsControl();
                            hostPanel.Children.Add(items);
                        }
                        items.Items.Add( output );
                    }
                }
                else if (_element != null)
                {
                    hostPanel.Children.Add(_element);
                }
            }));
            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            WriteObject(hostPanel);
            base.EndProcessing();
        }
    }
}
