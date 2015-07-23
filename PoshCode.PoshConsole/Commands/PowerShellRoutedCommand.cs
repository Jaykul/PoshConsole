using System.Management.Automation;
using System.Windows.Input;

namespace PoshCode.Commands
{
    public class PowerShellRoutedCommand<TCmdlet> : RoutedCommand
        where TCmdlet : Cmdlet
    {
         
    }
}