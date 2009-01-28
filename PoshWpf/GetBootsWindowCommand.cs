using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace PoshWpf
{
	[Cmdlet(VerbsCommon.Get, "BootsWindow", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByIndex")]
	public class GetBootsWindowCommand : PSCmdlet
	{
      [Parameter(Position = 0, Mandatory = false, ParameterSetName = "ByIndex")]
      public int[] Index { get; set; }
      
      [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByTitle")]
      public string[] Name { get; set; }

      private List<WildcardPattern> patterns;
      protected override void BeginProcessing()
      {
         if(ParameterSetName == "ByTitle") {
            patterns = new List<WildcardPattern>(Name.Length);
            foreach (var title in Name)
	         {
               patterns.Add( new WildcardPattern( title ) );
         	}
         }
 	      base.BeginProcessing();
      }

      protected override void ProcessRecord()
      {
         var windows = SessionState.PSVariable.Get("BootsWindows");

         if (windows != null && windows.Value != null && (windows.Value is BootsWindowDictionary))
         {
            switch (ParameterSetName)
	         {
               case "ByIndex":
                  foreach (var i in Index)
                  {
                     WriteObject(((BootsWindowDictionary)windows.Value)[i]);
                  } break;
               case "ByTitle":
                  foreach (var window in ((BootsWindowDictionary)windows.Value).Values)
                  {
                     foreach (var title in patterns)
	                  {
                        if(title.IsMatch( window.Title )) {
                           WriteObject(window);
                        }
                     }
                  } break;
         		default:
                  foreach (var window in ((BootsWindowDictionary)windows.Value).Values)
                  {
                     WriteObject(window);
                  } break;
	         }
         }

         base.ProcessRecord();
      }

   }
}
