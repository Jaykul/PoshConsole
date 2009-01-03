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
    [Cmdlet(VerbsCommon.New, "Button", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
    public class NewButtonCommand : WpfCommandBase
    {
        [Parameter( Position = 1 )]
        public RoutedEventHandler Click { get; set; }
        [Parameter]
        public Thickness Margin { get; set; }


        private Button button;
        protected override void ProcessRecord()
		{
            _dispatcher.Invoke((Action)(() =>
            {
                if (Content != null)
                {
                    object output = Content.BaseObject;

                    button = new Button();
                    button.Margin = Margin;

                    if (Click != null)
                    {
                        button.Click += Click;
                    }

                    if (_element != null)
                    {
                        ErrorRecord err;
                        FrameworkElement el;
                        _template.TryLoadXaml(out el, out err);
                        el.DataContext = output;
                        button.Content = el;
                    }
                    else
                    {
                        button.Content = output;
                    }
                }
            }));
            WriteObject(button);
        }
    }
}
