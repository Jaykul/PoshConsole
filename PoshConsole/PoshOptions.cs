using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Huddled.PoshConsole
{
    internal class PoshOptions : DependencyObject
    {
        PoshHost _host;
        IPoshConsoleControl _console;
        XamlConsole _xamlUI;
        public PoshOptions(PoshHost host, IPoshConsoleControl console)
        {
            _host = host;
            _console = console;
            _xamlUI = new XamlConsole(console);
        }

        public Properties.Settings Settings
        {
            get { return Properties.Settings.Default; }
        }

        public Properties.Colors Colors
        {
            get { return Properties.Colors.Default; }
        }

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

            public void WriteXaml(string xamlSource)
            {
                _console.WriteXaml(xamlSource);
            }

            public void LoadXaml(string sourceFile)
            {
                _console.LoadXaml(sourceFile);
            }

            #endregion
        }

        public IPSXamlConsole XamlUI
        {
            get
            {
                return _xamlUI;
            }
        }

        private delegate string GetStringDelegate();
        private delegate void SetStringDelegate( /* string value */ );

        public static DependencyProperty StatusTextProperty = DependencyProperty.Register("StatusText", typeof(string), typeof(PoshOptions));

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

        public double FullPrimaryScreenWidth
        {
            get { return System.Windows.SystemParameters.FullPrimaryScreenWidth; }
        }

        public double FullPrimaryScreenHeight
        {
            get { return System.Windows.SystemParameters.FullPrimaryScreenHeight; }
        }

        public CommandHistory History
        {
            get
            {
                return _console.History;
            }
        }
    }
}
