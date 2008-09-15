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
         Assemblies.Append(_baseConfig.Assemblies);

         Cmdlets.Append(_baseConfig.Cmdlets);

         //var assembly = System.Reflection.Assembly.GetCallingAssembly();
         //var cmdlets = assembly.GetCustomAttributes(typeof(System.Management.Automation.CmdletAttribute), false);

         foreach (var t in System.Reflection.Assembly.GetEntryAssembly().GetTypes())
         {
            var cmdlets = t.GetCustomAttributes(typeof(System.Management.Automation.CmdletAttribute), false) as System.Management.Automation.CmdletAttribute[];

            if (cmdlets != null)
            {
               foreach (var cmdlet in cmdlets)
               {
                  Cmdlets.Append(new CmdletConfigurationEntry(
                                    string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t,
                                    string.Format("{0}.xml", t.Name)));
               }
            }
         }

         //Cmdlets.Append(new CmdletConfigurationEntry("Out-WPF", typeof(OutWPFCommand), "OutWPFCommand.xml"));
         //Cmdlets.Append(new CmdletConfigurationEntry("Add-PoshSnapin", typeof(AddPoshSnapinCommand), "AddPoshSnapinCommand.xml"));
         //Cmdlets.Append(new CmdletConfigurationEntry("Add-Hotkey", typeof(AddHotkeyCommand), "AddHotkeyCommand.xml"));
         //Cmdlets.Append(new CmdletConfigurationEntry("New-Paragraph", typeof(NewParagraphCommand), "NewParagraphCommand.xml"));
         //Cmdlets.Append(new CmdletConfigurationEntry("Get-PoshOutput", typeof(GetPoshOutputCommand), "GetPoshOutputCommand.xml"));
         // ToDo: Add the HoddledControls assembly to the Assemeblies 

         Formats.Append(_baseConfig.Formats);
         InitializationScripts.Append(_baseConfig.InitializationScripts);
         Providers.Append(_baseConfig.Providers);
         Scripts.Append(_baseConfig.Scripts);
         Types.Append(_baseConfig.Types);


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
