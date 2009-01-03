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
    [Cmdlet(VerbsCommon.New, "StackPanel", DefaultParameterSetName = "DataTemplate", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
    public class NewStackPanelCommand : WpfNewPanelCommandBase
    {
        public NewStackPanelCommand()
        {
            Orientation = Orientation.Vertical;
        }


        [Parameter]
        public Orientation Orientation { get; set; }

        protected override Panel CreatePanel()
        {
            StackPanel panel = null;
            _dispatcher.Invoke((Action)(() =>
            {
                panel = new StackPanel();
                panel.Orientation = Orientation;
            }));
            return panel;
        }
    }
}
