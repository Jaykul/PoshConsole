using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PoshCode.PowerShell
{
    internal class Options : DependencyObject, IPSWpfOptions
    {
        public Options(PoshConsole wpfConsole)
        {
            WpfConsole = wpfConsole;
        }
        public IPSWpfConsole WpfConsole { get; set; }


        public Properties.Colors Colors
        {
            get { return Properties.Colors.Default; }
        }
    }
}
