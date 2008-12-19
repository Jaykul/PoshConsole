using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Windows;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using System.Windows.Controls;

namespace PoshWpf
{
	public static class XamlHelper
	{

		/// <summary>
		/// An exception-free wrapper for loading XAML from files. Loads a XAML
		/// file if it exists, and puts it in the out element parameter, or else
		/// writes a string to the error variable.
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <param name="source"></param>
		/// <param name="data"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		public static bool TryLoadXaml<T1>(this FileInfo source, out T1 element, out ErrorRecord error) //where T1 : FrameworkElement
		{
			error = default(System.Management.Automation.ErrorRecord);
			element = default(T1);
			try
			{
				object loaded = XamlReader.Load(source.OpenRead());
				if (loaded is T1)
				{
					element = (T1)loaded;
					return true;
				}
				else
				{
					error = new ErrorRecord(new ArgumentException("Template file doesn't yield FrameworkElement", "source"), "Can't DataBind", ErrorCategory.MetadataError, loaded);
				}
			}
			catch (Exception ex)
			{
				error = new ErrorRecord(ex, "Loading Xaml", ErrorCategory.SyntaxError, source);
			}
			return false;
		}


		/// <summary>
		/// An exception-free wrapper for loading XAML from files. Loads a XAML
		/// file if it exists, and puts it in the out element parameter, or else
		/// writes a string to the error variable.
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <param name="source"></param>
		/// <param name="data"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		public static bool TryLoadXaml<T1>(this XmlDocument source, out T1 element, out ErrorRecord error) //where T1 : FrameworkElement
		{
			error = default(System.Management.Automation.ErrorRecord);
			element = default(T1);
			try
			{
				var loaded = XamlReader.Load(new XmlNodeReader(source));
				if (loaded is T1)
				{
					element = (T1)loaded;
					return true;
				}
				else
				{
					error = new ErrorRecord(new ArgumentException("Template file doesn't yield FrameworkElement", "source"), "Can't DataBind", ErrorCategory.MetadataError, loaded);
				}
			}
			catch (Exception ex)
			{
				error = new ErrorRecord(ex, "Loading Xaml", ErrorCategory.SyntaxError, source);
			}
			return false;
		}


		/// <summary>
		/// Create a new ItemsControl to contain objects....
		/// </summary>
		/// <returns>An <see cref="System.Windows.Controls.ItemsControl"/></returns>
		public static ItemsControl NewItemsControl()
		{
			FrameworkElementFactory factoryPanel = new FrameworkElementFactory(typeof(WrapPanel));
			factoryPanel.SetValue(WrapPanel.IsItemsHostProperty, true);
			factoryPanel.SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal);
			ItemsPanelTemplate template = new ItemsPanelTemplate() { VisualTree = factoryPanel };

			return new ItemsControl()
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				ItemsPanel = template
			};
		}
	}
}
