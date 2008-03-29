using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using PoshConsole.Controls;

namespace PoshConsole.PSHost
{
   internal class PoshOptions : DependencyObject
   {

      #region [rgn] Fields (4)

      IPoshConsoleControl _console;
      PoshHost _host;
      XamlConsole _xamlUI;
      public static DependencyProperty StatusTextProperty = DependencyProperty.Register("StatusText", typeof(string), typeof(PoshOptions));

      #endregion [rgn]

      #region [rgn] Constructors (1)

      public PoshOptions(PoshHost host, IPoshConsoleControl console)
      {
         _host = host;
         _console = console;
         _xamlUI = new XamlConsole(console);
      }

      #endregion [rgn]

      #region [rgn] Properties (7)

      public Properties.Colors Colors
      {
         get { return Properties.Colors.Default; }
      }

      public double FullPrimaryScreenHeight
      {
         get { return System.Windows.SystemParameters.FullPrimaryScreenHeight; }
      }

      public double FullPrimaryScreenWidth
      {
         get { return System.Windows.SystemParameters.FullPrimaryScreenWidth; }
      }

      public CommandHistory History
      {
         get
         {
            return _console.History;
         }
      }

      public Properties.Settings Settings
      {
         get { return Properties.Settings.Default; }
      }

      public string StatusText
      {
         get
         {
            if (!Dispatcher.CheckAccess())
            {
               return (string)Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (GetStringDelegate)delegate { return StatusText; });
            }
            else
            {
               return (string)base.GetValue(StatusTextProperty);
            }
         }
         set
         {
            if (!Dispatcher.CheckAccess())
            {
               Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (SetStringDelegate)delegate { StatusText = value; });
            }
            else
            {
               base.SetValue(StatusTextProperty, value);
            }
         }
      }

      public IPSXamlConsole XamlUI
      {
         get
         {
            return _xamlUI;
         }
      }
      public IPSBackgroundHost BgHost
      {
         get
         {
            return _host;
         }
      }

      #endregion [rgn]

      #region [rgn] Delegates and Events (2)

      // [rgn] Delegates (2)

      private delegate string GetStringDelegate();
      private delegate void SetStringDelegate( /* string value */ );

      #endregion [rgn]

      #region [rgn] Nested Classes (1)

      public class XamlConsole : IPSXamlConsole
      {
         private IPSXamlConsole _console;

         /// <summary>
         /// Initializes a new instance of the <see cref="XamlConsole"/> class.
         /// </summary>
         /// <param name="console">The console.</param>
         public XamlConsole(IPSXamlConsole console)
         {
            _console = console;
         }


         #region IPSXamlConsole Members

         public void OutXaml(System.Xml.XmlDocument template)
         {
            _console.OutXaml(template);
         }

         public void OutXaml(System.IO.FileInfo template)
         {
            _console.OutXaml(template);
         }

         public void OutXaml(System.Xml.XmlDocument template, System.Management.Automation.PSObject data)
         {
            _console.OutXaml(template, data);
         }

         public void OutXaml(System.IO.FileInfo template, System.Management.Automation.PSObject data)
         {
            _console.OutXaml(template, data);
         }

         #endregion
      }

      #endregion [rgn]

   }
}
