using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace PoshCode.Wpf
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
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool TryLoadXaml<TElement>(this FileInfo source, out TElement element, out ErrorRecord error) //where T1 : FrameworkElement
        {
            error = default(ErrorRecord);
            element = default(TElement);
            try
            {
                object loaded = XamlReader.Load(source.OpenRead());
                if (loaded is TElement)
                {
                    element = (TElement)loaded;
                    return true;
                }
                error = new ErrorRecord(new ArgumentException("Template file doesn't yield FrameworkElement", "source"), "Can't DataBind", ErrorCategory.MetadataError, loaded);
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
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool TryLoadXaml<TElement>(this XmlDocument source, out TElement element, out ErrorRecord error) //where T1 : FrameworkElement
        {
            error = default(ErrorRecord);
            element = default(TElement);
            try
            {
                using (var reader = new XmlNodeReader(source))
                {
                    var loaded = XamlReader.Load(reader);
                    if (loaded is TElement)
                    {
                        element = (TElement)loaded;
                        return true;
                    }
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
            ItemsPanelTemplate template = new ItemsPanelTemplate { VisualTree = factoryPanel };

            return new ItemsControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsPanel = template
            };
        }

        private static readonly string defaultTemplate = Path.Combine(
                                                      Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                                      Path.Combine("XamlTemplates", "default.xaml"));
        private static List<string> __dataTemplates = new List<string>(new[] { defaultTemplate });

        internal static string[] GetDataTemplates()
        {
            // we're copying to a new array, so you can't use this to add things where the file doesn't exist
            return __dataTemplates.ToArray();
        }

        internal static void AddDataTemplate(string templatePath)
        {
            if (File.Exists(templatePath) && !__dataTemplates.Contains(templatePath))
            {
                __dataTemplates.Add(templatePath);
            }
        }

        internal static void RemoveDataTemplate(string templatePath)
        {
            if (__dataTemplates.Contains(templatePath))
            {
                __dataTemplates.Remove(templatePath);
            }
        }

        internal static void LoadTemplates(this Window window)
        {
            window.Dispatcher.Invoke(() =>
            {
                window.Resources.MergedDictionaries.Clear();

                foreach (var templatePath in __dataTemplates)
                {
                    if (File.Exists(templatePath))
                    {
                        ResourceDictionary resources;
                        ErrorRecord error;
                        FileInfo startup = new FileInfo(templatePath);
                        // Application.ResourceAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                        if (startup.TryLoadXaml(out resources, out error))
                        {
                            //Application.Current.Resources.MergedDictionaries.Add(resources);
                            window.Resources.MergedDictionaries.Add(resources);
                        }
                        else
                        {
                            throw error.Exception;
                        }
                    }
                }
            });

        }

        static XamlHelper()
        {
            //TypeDescriptor.AddProvider(new BindingTypeDescriptionProvider(), typeof(System.Windows.Data.Binding));
            // this one is absolutely vital to the functioning of ConvertToXaml
            TypeDescriptor.AddAttributes(typeof(BindingExpression), new TypeConverterAttribute(typeof(BindingConverter)));
        }

        public static string RoundtripXaml(string xaml)
        {
            return XamlWriter.Save(XamlReader.Parse(xaml));
        }

        public static string ConvertToXaml(object ui)
        {
            return XamlWriter.Save(ui);
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(this DependencyObject parent, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }
        //public static string FormatXaml(object ui)
        //{
        //   TypeDescriptor.AddAttributes(typeof(System.Windows.Data.BindingExpression), new Attribute[] { new TypeConverterAttribute(typeof(BindingConverter)) });

        //   var outstr = new System.Text.StringBuilder();
        //   // fix up the formatting a little
        //   var settings = new System.Xml.XmlWriterSettings();
        //   settings.Indent = true;
        //   settings.OmitXmlDeclaration = true;

        //   var writer = System.Xml.XmlWriter.Create(outstr, settings);
        //   var dsm = new System.Windows.Markup.XamlDesignerSerializationManager(writer);
        //   // turn on expression saving mode 
        //   dsm.XamlWriterMode = System.Windows.Markup.XamlWriterMode.Expression;

        //   System.Windows.Markup.XamlWriter.Save(ui, dsm);
        //   return outstr.ToString();
        //}
    }
}
