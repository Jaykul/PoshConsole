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
    public class WpfNewControlCommandBase : WpfCommandBase
    {
        protected Control control;

        [Parameter]public Brush Background                                  { get; set; }
        [Parameter]public Brush BorderBrush                                 { get; set; }
        [Parameter]public Thickness BorderThickness                         { get; set; }
        [Parameter]public Dock   Dock                                       { get; set; }
        [Parameter]public FontFamily FontFamily                             { get; set; }
        [Parameter]public Double FontSize                                   { get; set; }
        [Parameter]public FontStretch FontStretch                           { get; set; }
        [Parameter]public FontWeight FontWeight                             { get; set; }
        [Parameter]public Brush Foreground                                  { get; set; }
        [Parameter]public Double Height                                     { get; set; }
        [Parameter]public HorizontalAlignment HorizontalAlignment           { get; set; }
        [Parameter]public HorizontalAlignment HorizontalContentAlignment    { get; set; }
        [Parameter]public Thickness Padding                                 { get; set; }
        [Parameter]public Thickness Margin                                  { get; set; }
        [Parameter]public Double MaxHeight                                  { get; set; }
        [Parameter]public Double MaxWidth                                   { get; set; }
        [Parameter]public Double MinHeight                                  { get; set; }
        [Parameter]public Double MinWidth                                   { get; set; }
        [Parameter]public String Name                                       { get; set; }
        [Parameter]public object ToolTip                                    { get; set; }
        [Parameter]public VerticalAlignment VerticalAlignment               { get; set; }
		  [Parameter]public VerticalAlignment VerticalContentAlignment        { get; set; }
        [Parameter]public Double Width                                      { get; set; }

        protected override void ProcessRecord()
        {
            _dispatcher.Invoke((Action)(() =>
            {
                if (Background != null) 
                    control.Background = Background;
                if (BorderBrush != null)
                    control.BorderBrush = BorderBrush;
                if (BorderThickness != default(Thickness))
                    control.BorderThickness = BorderThickness;
					 if (Dock != default(Dock))
						 control.SetValue(DockPanel.DockProperty, Dock);
					 if (FontFamily != null) 
                    control.FontFamily = FontFamily;
                if (FontSize > 0)
                    control.FontSize = FontSize;
                if (FontStretch != default(FontStretch))
                    control.FontStretch = FontStretch;
                if (FontWeight != default(FontWeight))
                    control.FontWeight = FontWeight;
                if (BorderBrush != null)
                    control.Foreground = Foreground;
                if (Height > 0)
                    control.Height = Height;
                if (HorizontalAlignment != default(HorizontalAlignment))
                    control.HorizontalAlignment = HorizontalAlignment;
                if (HorizontalContentAlignment != default(HorizontalAlignment))
                    control.HorizontalContentAlignment = HorizontalContentAlignment;
                if (Margin != default(Thickness))
                    control.Margin = Margin;
                if (MaxHeight > 0)
                    control.MaxHeight = MaxHeight;
                if (MaxWidth > 0)
                    control.MaxWidth = MaxWidth;
                if (MinHeight > 0)
                    control.MinHeight = MinHeight;
                if (MinWidth > 0)
                    control.MinWidth = MinWidth;
                if (!string.IsNullOrEmpty(Name))
                    control.Name = Name;
					 if (Padding != default(Thickness))
						 control.Padding = Padding;
					 if (ToolTip != null)
                    control.ToolTip = ToolTip;
					 if (VerticalAlignment != default(VerticalAlignment))
						 control.VerticalAlignment = VerticalAlignment;
					 if (VerticalContentAlignment != default(VerticalAlignment))
						 control.VerticalContentAlignment = VerticalContentAlignment;
					 if (Width > 0)
						 control.Width = Width;
            }));

            base.ProcessRecord();
        }
    }
}
