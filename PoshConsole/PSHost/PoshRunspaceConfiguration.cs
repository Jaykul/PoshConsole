using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PoshConsole.Cmdlets;

namespace PoshConsole.PSHost
{
   internal sealed class PoshRunspaceConfiguration : RunspaceConfiguration
   {
      public static string ShellIdConstant = "PoshConsole";
      private RunspaceConfiguration _baseConfig;
      /// <summary>
      /// Default Constructor copies all the data from the "default" RunspaceConfiguration
      /// </summary>
      public PoshRunspaceConfiguration()
      {
         _baseConfig = RunspaceConfiguration.Create();
         // AuthorizationManager = baseConfig.AuthorizationManager;
         // ToDo: Add the HuddledControls assembly to the Assemeblies 
         Assemblies.Append(_baseConfig.Assemblies);
         Cmdlets.Append(_baseConfig.Cmdlets);
         Formats.Append(_baseConfig.Formats);
         InitializationScripts.Append(_baseConfig.InitializationScripts);
         Providers.Append(_baseConfig.Providers);
         Scripts.Append(_baseConfig.Scripts);
         Types.Append(_baseConfig.Types);

         // TODO: uncomment this if we start really using it
         //foreach (var t in System.Reflection.Assembly.GetEntryAssembly().GetTypes())
         //{
         //   var cmdlets = t.GetCustomAttributes(typeof(System.Management.Automation.CmdletAttribute), false) as System.Management.Automation.CmdletAttribute[];
         //   if (cmdlets != null)
         //   {
         //      foreach (var cmdlet in cmdlets)
         //      {
         //         Cmdlets.Append(new CmdletConfigurationEntry(
         //                           string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t,
         //                           string.Format("{0}.xml", t.Name)));
         //      }
         //   }
         //}

      }
      public override System.Management.Automation.AuthorizationManager AuthorizationManager
      {
         get
         {
            return base.AuthorizationManager;
         }
      }

      public new System.Management.Automation.PSSnapInInfo AddPSSnapIn(string name, out System.Management.Automation.Runspaces.PSSnapInException warning)
      {
         return _baseConfig.AddPSSnapIn(name, out warning);
      }

      public override string ShellId
      {
         get { return ShellIdConstant; }
      }
   }
}
