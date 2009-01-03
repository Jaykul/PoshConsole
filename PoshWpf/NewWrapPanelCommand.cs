using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Management.Automation;
using System.Windows.Media;

namespace PoshWpf
{
    [Cmdlet(VerbsCommon.New, "WrapPanel", DefaultParameterSetName = "DataTemplate", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
    public class NewWrapPanelCommand : NewPanelCommand
    {
        [Parameter]
        public Orientation Orientation { get; set; }


        protected override Panel CreatePanel()
        {
            Panel panel = null;
            _dispatcher.Invoke((Action)(() =>
            {
                panel = new WrapPanel();

                panel.SetValue(WrapPanel.OrientationProperty, Orientation);
                if (Background != null)
                {
                    panel.SetValue(WrapPanel.BackgroundProperty, Background);
                }

                if (Width > 0.0)
                {
                    panel.SetValue(WrapPanel.WidthProperty, Width);
                }

                if (Height > 0.0)
                {
                    panel.SetValue(WrapPanel.HeightProperty, Height);
                }
            }));

            return panel;
        }
    }
}
