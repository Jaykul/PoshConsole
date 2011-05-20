using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Windows.Interactivity;
using System.Windows.Media;
using Huddled.Wpf;
using Huddled.Interop;
using Huddled.Interop.Windows;
using PoshWpf;

namespace PoshConsole.Cmdlets
{
   [Cmdlet(VerbsCommon.Switch, "QuakeMode", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
   public class QuakeModeCommand : PSCmdlet
   {
      protected override void BeginProcessing()
      {
         ((PoshConsole.Host.PoshOptions)Host.PrivateData.BaseObject).WpfConsole.Dispatcher.BeginInvoke((Action)(() =>
         {
            var options = (PoshConsole.Host.PoshOptions)Host.PrivateData.BaseObject;
            var snapTo = Interaction.GetBehaviors(options.WpfConsole.RootWindow).OfType<SnapToBehavior>().Single();
            if (options.Settings.QuakeMode == DockingEdge.None) {
               snapTo.WindowState = AdvancedWindowState.DockedTop;
               snapTo.DockAgainst = options.Settings.QuakeMode = DockingEdge.Top;
            }
            else {
               snapTo.WindowState = AdvancedWindowState.Normal;
               snapTo.DockAgainst = options.Settings.QuakeMode = DockingEdge.None;
            }

            options.Settings.Save();
         }));
      }
   }
}