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
	[Cmdlet("Register", "BootsEvent", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = ByElement)]
   public class RegisterBootsEventCommand : ScriptBlockBase//, IDynamicParameters
	{
		private const string ByTitle = "ByTitle";
		private const string ByIndex = "ByIndex";
		private const string ByElement = "ByElement";

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByIndex, ValueFromPipeline = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public int[] Index { get; set; }

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByTitle, ValueFromPipelineByPropertyName = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public string[] Title { get; set; }
		private List<WildcardPattern> windowTitlePatterns;

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByElement, ValueFromPipeline = true), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		[Alias("Window")]
		public UIElement[] Element { get; set; }


      [Parameter(Position = 2, Mandatory = false), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		[ValidateNotNullOrEmpty]
		public string[] Name { get; set; }
		private List<WildcardPattern> namePatterns;


		[Parameter(Position = 4, Mandatory = true)]
		[ValidateNotNullOrEmpty]
		public string EventName { get; set; }

		[Parameter(Position = 6, Mandatory = true)]
		[ValidateNotNullOrEmpty]
		public RoutedEventHandler Action { get; set; }

      [Parameter(), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Passthru")]
		public SwitchParameter Passthru{ get; set;}
		//public object GetDynamicParameters()
		//{
		//   var paramDictionary = new RuntimeDefinedParameterDictionary();
		//   var param = new RuntimeDefinedParameter();
		//   param.Name = "Action"

		//   var prop = new ParameterAttribute();
		//   prop.Position = 10;
		//   prop.Mandatory = true;

		//   param.Attributes.Add( prop );
		//   param.ParameterType = 

		//   paramDictionary.Add("Action", param)

		//   return paramDictionary
		//}


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
			if (Name != null) // Name.Length > 0
			{
				namePatterns = new List<WildcardPattern>(Name.Length);
				foreach (var name in Name)
				{
					namePatterns.Add(new WildcardPattern(name, WildcardOptions.IgnoreCase | WildcardOptions.Compiled));
				}
			}
			base.BeginProcessing();
		}

		public RoutedEvent GetEvent(Type type, String eventName)
		{
			RoutedEvent[] re = EventManager.GetRoutedEventsForOwner(type);
			if (re != null)
			{
				foreach (var e in re)
				{
               if (string.Equals(e.Name, EventName, StringComparison.OrdinalIgnoreCase))
					{
						return e;
					}
				}
			}
			re = EventManager.GetRoutedEvents();
			if (re != null)
			{
				foreach (var e in re)
				{
               if (string.Equals(e.Name, EventName, StringComparison.OrdinalIgnoreCase))
					{
						return e;
					}
				}
			}
			ThrowTerminatingError(new ErrorRecord(new NotSupportedException("'" + eventName + "' event not found."), "EventNotFound", ErrorCategory.InvalidArgument, EventName));
			return null;
		}

		protected override void ProcessRecord()
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
							{
								window.Dispatcher.Invoke((Action)(() =>
									{
										try
										{
											List<UIElement> controls = (Name != null) ? FindByName(window, namePatterns) : new List<UIElement>(new[] { window });
											foreach (var el in controls)
											{
												el.AddHandler(GetEvent(el.GetType(), EventName), Action);
											}
										}
										catch (System.Reflection.TargetInvocationException ex)
										{
											WriteError(new ErrorRecord(ex.InnerException, "TargetInvocationException", ErrorCategory.NotSpecified, Element));
										}
									}
								));
								if(Passthru.ToBool()) { WriteObject( window ); }
							}
						} break;
					case ByTitle:
						foreach (var window in BootsWindowDictionary.Instance.Values)
						{
							if (window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
							{
								bool w = false;
								foreach (var title in windowTitlePatterns)
								{
									w |= (bool)window.Dispatcher.Invoke((Func<bool>)(() =>
									{
										try
										{
											if (title.IsMatch(window.Title))
											{
												List<UIElement> controls = (Name != null) ? FindByName(window, namePatterns) : new List<UIElement>(new[] { window });
												foreach (var el in controls)
												{
													el.AddHandler(GetEvent(el.GetType(), EventName), Action);
												}
												return true;
											} 
										}
										catch (System.Reflection.TargetInvocationException ex)
										{
											WriteError(new ErrorRecord(ex.InnerException, "TargetInvocationException", ErrorCategory.NotSpecified, Element));
										}
										return false;
									}));
								}
								if(w  && Passthru.ToBool()) { WriteObject( window ); }
							}
						} break;
					case ByElement:
						foreach (var window in Element)
						{
							if (window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
								window.Dispatcher.Invoke((Action)(() =>
							{
								try
								{
									List<UIElement> controls = (Name != null) ? FindByName(window, namePatterns) : new List<UIElement>(new[] { window });
									foreach (var el in controls)
									{
										el.AddHandler(GetEvent(el.GetType(), EventName), Action);
									}
								}
								catch (System.Reflection.TargetInvocationException ex)
								{
									WriteError(new ErrorRecord(ex.InnerException, "TargetInvocationException", ErrorCategory.NotSpecified, Element));
								}
							}
							));
							if(Passthru.ToBool()) { WriteObject( window ); }
						} break;
				}
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
