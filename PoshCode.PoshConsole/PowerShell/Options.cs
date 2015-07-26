using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Controls;
using PoshCode.Properties;

namespace PoshCode.PowerShell
{
    internal class Options : DependencyObject, IPSWpfOptions
    {
        public Options(PoshConsole wpfConsole)
        {
            WpfConsole = wpfConsole;
        }
        public IRichConsole WpfConsole { get; set; }

        public ContentControl ContentControl { get; set;  }

        public Colors Colors
        {
            get { return Colors.Default; }
        }
    }
}
