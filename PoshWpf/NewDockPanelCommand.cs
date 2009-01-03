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
    [Cmdlet(VerbsCommon.New, "DockPanel", DefaultParameterSetName = "DataTemplate", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
    public class NewDockPanelCommand : NewPanelCommand
    {

        [Parameter][Alias("Fill")]
        public Boolean LastChildFill { get; set; }

        protected override Panel CreatePanel()
        {
            Panel panel = null;
            _dispatcher.Invoke((Action)(() =>
            {
                panel = new DockPanel();
                panel.SetValue(DockPanel.LastChildFillProperty, LastChildFill);
                panel.SetValue(DockPanel.DockProperty, Dock);


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
