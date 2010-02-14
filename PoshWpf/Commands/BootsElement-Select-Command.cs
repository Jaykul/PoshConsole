using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace PoshWpf
{
	[Cmdlet(VerbsCommon.Select, "BootsElement", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = ByElement)]
   public class SelectBootsElementCommand : ScriptBlockBase
	{
		private const string ByTitle = "ByTitle";
		private const string ByIndex = "ByIndex";
		private const string ByElement = "ByElement";

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByIndex, ValueFromPipeline = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public int[] Index { get; set; }

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByTitle, ValueFromPipelineByPropertyName = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public string[] Title { get; set; }

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByElement, ValueFromPipeline = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		[Alias("Window")]
		public UIElement[] Element { get; set; }

      [Parameter(Position = 1, Mandatory = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		[ValidateNotNullOrEmpty]
		public string[] Name { get; set; }
		private List<WildcardPattern> namePatterns;

		private List<WildcardPattern> windowTitlePatterns;
		protected override void BeginProcessing()
		{
			if (ParameterSetName == ByTitle)
			{
				windowTitlePatterns = new List<WildcardPattern>(Title.Length);
				foreach (var title in Title)
				{
					windowTitlePatterns.Add(new WildcardPattern(title, WildcardOptions.IgnoreCase | WildcardOptions.Compiled));
				}
			}

			namePatterns = new List<WildcardPattern>(Name.Length);
			foreach (var name in Name)
			{
				namePatterns.Add(new WildcardPattern(name, WildcardOptions.IgnoreCase | WildcardOptions.Compiled));
			}
			base.BeginProcessing();
		}

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      protected override void ProcessRecord()
		{
			try
			{
				if (BootsWindowDictionary.Instance.Count > 0)
				{
					switch (ParameterSetName)
					{
						case ByIndex:
							foreach (var i in Index)
							{
								var window = BootsWindowDictionary.Instance[i];
								if (window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
									WriteObject(window.Dispatcher.Invoke(((Func<UIElement, List<WildcardPattern>, List<UIElement>>)FindByName), window, namePatterns), true);
							} break;
						case ByTitle:
							foreach (var window in BootsWindowDictionary.Instance.Values)
							{
								if (window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
								{
									foreach (var title in windowTitlePatterns)
									{
										WriteObject(
											window.Dispatcher.Invoke((Func<List<UIElement>>)(() =>
											{
												if (title.IsMatch(window.Title))
												{
													return FindByName(window, namePatterns);
												}
												else return null;
											})), true);
									}
								}
							} break;
						case ByElement:
							foreach (var window in Element)
							{
								if (window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
									WriteObject(window.Dispatcher.Invoke(((Func<UIElement, List<WildcardPattern>, List<UIElement>>)FindByName), window, namePatterns), true);
							} break;
					}
				}
			}
			catch (Exception ex)
			{
				WriteError(new ErrorRecord(ex, "TrappedException", ErrorCategory.NotSpecified, Element));
			}
			base.ProcessRecord();
		}

		List<string> _contentProperties = new List<string>();

		private List<UIElement> FindByName(UIElement element, List<WildcardPattern> patterns)
		{
			var sb = InvokeCommand.NewScriptBlock("Get-BootsContentProperty");
			foreach (var cont in Invoke(sb))
			{
				_contentProperties.Add(cont.BaseObject.ToString());
			}

			var results = new List<UIElement>();
			FindByName(element, patterns, ref results);
			return results;
		}

		private void FindByName(UIElement element, List<WildcardPattern> patterns, ref List<UIElement> results)
		{
			foreach (string content in _contentProperties)
			{
				Type type = element.GetType();
				var prop = type.GetProperty(content);
				if (prop != null)
				{
					var enumerable = prop.GetValue(element, null) as System.Collections.IEnumerable;
					if (enumerable != null)
					{
						foreach (object el in enumerable)
						{
							var fel = el as FrameworkElement;
							if (fel != null)
							{
								foreach (var pattern in patterns)
								{
									if (pattern.IsMatch(fel.Name))
									{
										results.Add(fel);
									}
								}
							}
						}
						foreach (object el in enumerable)
						{
							if (el is UIElement)
							{
								FindByName((UIElement)el, patterns, ref results);
							}
						}
					}
					else
					{
						var el = prop.GetValue(element, null) as UIElement;
						if (el != null)
						{
							var fel = el as FrameworkElement;
							if (fel != null)
							{
								foreach (var pattern in patterns)
								{
									if (pattern.IsMatch(fel.Name))
									{
										results.Add(fel);
									}
								}
								FindByName(fel, patterns, ref results);
							}
							else
							{
								FindByName(el, patterns, ref results);
							}
						}
					}
				}
				// if we didn't find it by now, just give up...
			}
		}
	}
}
