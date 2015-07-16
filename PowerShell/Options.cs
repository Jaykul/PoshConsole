using System.Management.Automation.Host;
using System.Windows;
using PoshCode.Properties;

namespace PoshCode.PowerShell
{
    internal class Options : DependencyObject, IPSWpfOptions
    {
        public Options(PoshConsole wpfConsole)
        {
            WpfConsole = wpfConsole;
        }
        public IPSWpfConsole WpfConsole { get; set; }


        public Colors Colors
        {
            get { return Colors.Default; }
        }
    }
}
