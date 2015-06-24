using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PoshCode.PowerShell
{
    internal class Options : DependencyObject
    {
        public Options(PoshConsole console)
        {
            Console = console;
        }
        public PoshConsole Console { get; set; }


        public Properties.Colors Colors
        {
            get { return Properties.Colors.Default; }
        }
    }
}
