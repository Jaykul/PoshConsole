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
	[Cmdlet(VerbsCommon.Get, "BootsWindow", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ShowAll")]
	public class GetBootsWindowCommand : PSCmdlet
	{
      [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByIndex")]
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
			if (BootsWindowDictionary.Instance.Count > 0)
         {
            switch (ParameterSetName)
	         {
               case "ByIndex":
                  foreach (var i in Index)
                  {
                     WriteObject(BootsWindowDictionary.Instance[i]);
                  } break;
               case "ByTitle":
                  foreach (var window in BootsWindowDictionary.Instance.Values)
                  {
                     foreach (var title in patterns)
	                  {
                        if(title.IsMatch( window.Title )) {
                           WriteObject(window);
                        }
                     }
                  } break;
         		default:
                  foreach (var window in BootsWindowDictionary.Instance.Values)
                  {
                     WriteObject(window);
                  } break;
	         }
         }

         base.ProcessRecord();
      }

   }
}
