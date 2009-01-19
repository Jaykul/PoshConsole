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
	[Cmdlet(VerbsCommon.New, "TextBox", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
	public class NewTextBoxCommand : WpfNewControlCommandBase
	{
		[Parameter()]
		public SwitchParameter ReadOnly { get; set; }


		protected override void ProcessRecord()
		{
			_dispatcher.Invoke((Action)(() =>
			{
				control = new TextBox();
				((TextBox)control).IsReadOnly = ReadOnly.ToBool();
				((TextBox)control).VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
				((TextBox)control).HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

				if (Content != null)
				{
					object output = Content.BaseObject;
					((TextBox)control).Text += output.ToString();
				}
			}));

			base.ProcessRecord();
			WriteObject(control);
		}
	}
}
