using System;
using System.IO;
using System.Management.Automation;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace PoshCode.Controls.Utility
{
    public static class WpfHelper
    {

        /// <summary>
        /// An exception-free wrapper for loading XAML from files. Loads a XAML
        /// file if it exists, and puts it in the out element parameter, or else
        /// writes a string to the error variable.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="source"></param>
        /// <param name="element"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool TryLoadXaml<T1>(this FileInfo source, out T1 element, out ErrorRecord error) //where T1 : FrameworkElement
        {
            error = default(ErrorRecord);
            element = default(T1);
            try
            {
                object loaded = XamlReader.Load(source.OpenRead());
                if (loaded is T1)
                {
                    element = (T1)loaded;
                    return true;
                }
                error = new ErrorRecord(new ArgumentException("Template file doesn't yield FrameworkElement", nameof(source)), "Can't DataBind", ErrorCategory.MetadataError, loaded);
            }
            catch (Exception ex)
            {
                error = new ErrorRecord(ex, "Loading Xaml", ErrorCategory.SyntaxError, source);
            }
            return false;
        }


        /// <summary>
        /// Tries to locate an item of the specified type within the visual tree,
        /// starting from a specific position. 
        /// </summary>
        /// <typeparam name="T">The type of element to find.</typeparam>
        /// <param name="reference">The main element to do hit testing from.</param>
        /// <param name="point">The position to start hit testing at.</param>
        public static T TryFindFromPoint<T>(this UIElement reference, Point point)
          where T : DependencyObject
        {
            var element = reference.InputHitTest(point) as DependencyObject;
            if (element == null) return null;
            return element as T ?? TryFindParent<T>(element);
        }

        /// <summary>
        /// Finds a parent of the specified type of a given item in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of element to find.</typeparam>
        /// <param name="child">A direct or indirect child of the element you're trying to find.</param>
        /// <returns>The first parent item of the correct type, or null.
        /// </returns>
        public static T TryFindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            //child.Dispatcher.Invoke(()=>{
            // get parent item
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {  // If the parent is the right type, return it.
                if (parent is T) break;
                parent = VisualTreeHelper.GetParent(parent);
            }
            // we reached the root of the tree without finding it
            return parent as T;
        }

        public static IntPtr GetHandle(this DependencyObject control)
        {
            return control.Dispatcher.Invoke(() => { 
                var window = Window.GetWindow(control);
                return window == null
                    ? IntPtr.Zero
                    : new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
                });
        }
    }
}