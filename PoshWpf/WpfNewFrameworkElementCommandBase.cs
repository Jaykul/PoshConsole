using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace PoshWpf
{
    public class WpfNewFrameworkElementCommandBase : WpfCommandBase
    {
		  protected FrameworkElement element;

        [Parameter]public Dock   Dock                                       { get; set; }
        [Parameter]public Double Height                                     { get; set; }
        [Parameter]public HorizontalAlignment HorizontalAlignment           { get; set; }
        [Parameter]public Thickness Margin                                  { get; set; }
        [Parameter]public Double MaxHeight                                  { get; set; }
        [Parameter]public Double MaxWidth                                   { get; set; }
        [Parameter]public Double MinHeight                                  { get; set; }
        [Parameter]public Double MinWidth                                   { get; set; }
        [Parameter]public String Name                                       { get; set; }
        [Parameter]public object ToolTip                                    { get; set; }
        [Parameter]public Double Width                                      { get; set; }
        [Parameter]public VerticalAlignment   VerticalAlignment             { get; set; }

		  protected override void ProcessRecord()
        {
            _dispatcher.Invoke((Action)(() =>
            {
                if(Dock != default(Dock))
                    element.SetValue(DockPanel.DockProperty, Dock);
                if (Height > 0)
                    element.Height = Height;
                if (HorizontalAlignment != default(HorizontalAlignment))
                    element.HorizontalAlignment = HorizontalAlignment;
                if (Margin != default(Thickness))
                    element.Margin = Margin;
                if (MaxHeight > 0)
                    element.MaxHeight = MaxHeight;
                if (MaxWidth > 0)
                    element.MaxWidth = MaxWidth;
                if (MinHeight > 0)
                    element.MinHeight = MinHeight;
                if (MinWidth > 0)
                    element.MinWidth = MinWidth;
                if (!string.IsNullOrEmpty(Name))
                    element.Name = Name;
                if (ToolTip != null)
                    element.ToolTip = ToolTip;
					 if (VerticalAlignment != default(VerticalAlignment))
						 element.VerticalAlignment = VerticalAlignment;
                if (Width > 0)
                    element.Width = Width;
            }));

            base.ProcessRecord();
        }
    }
}
