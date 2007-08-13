using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Huddled.PoshConsole
{
    internal class PoshOptions : DependencyObject
    {
        PoshHost myHost;
        IPSConsoleControl myConsole;
        public PoshOptions(PoshHost host, IPSConsoleControl console)
        {
            myHost = host;
            myConsole = console;
        }

        public Properties.Settings Settings
        {
            get { return Properties.Settings.Default; }
        }
        public Properties.Colors Colors
        {
            get { return Properties.Colors.Default; }
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
                return myConsole.History;
            }
        }
    }
}
