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
    public class NewWrapPanelCommand : WpfNewPanelCommandBase
    {
        [Parameter]
        public Orientation Orientation { get; set; }


		  /// <summary>
		  /// Create the Panel object
		  /// </summary>
		  /// <remarks>Must be invoked on the dispatcher thread.</remarks>
		  /// <returns>The panel for the PanelCommand</returns>
		  protected override Panel CreatePanel()
        {
            Panel panel = new WrapPanel();
            panel.SetValue(WrapPanel.OrientationProperty, Orientation);
            return panel;
        }
    }
}
