using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Controls;

namespace PoshWpf
{
    [Cmdlet(VerbsCommon.New, "Image", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
	public class NewImageCommand : WpfNewFrameworkElementCommandBase
    {

        protected override void ProcessRecord()
		  {
            _dispatcher.Invoke((Action)(() =>
            {
                if (Content != null)
                {
                    object output = Content.BaseObject;

                    element = new Image();
						  element.Margin = Margin;

                    if (_element != null)
                    {
                        ErrorRecord err;
                        FrameworkElement el;
                        _template.TryLoadXaml(out el, out err);
                        el.DataContext = output;
								element = el;
                    }
                    else
                    {
							  ((Image)element).Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(Content.ToString()));
                    }
                }
            }));
				base.ProcessRecord();
				WriteObject(element);
        }
    }
}
