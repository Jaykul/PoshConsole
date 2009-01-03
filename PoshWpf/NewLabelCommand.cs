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
    [Cmdlet(VerbsCommon.New, "Label", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
	public class NewLabelCommand : WpfNewControlCommandBase
    {

        protected override void ProcessRecord()
		  {
            _dispatcher.Invoke((Action)(() =>
            {
                if (Content != null)
                {
                    object output = Content.BaseObject;

                    control = new Label();

                    if (_element != null)
                    {
                        ErrorRecord err;
                        FrameworkElement el;
                        _template.TryLoadXaml(out el, out err);
                        el.DataContext = output;
								((Label)control).Content = el;
                    }
                    else
                    {
							  ((Label)control).Content = output;
                    }
                }
            }));
				base.ProcessRecord();
				WriteObject(control);
        }
    }
}
