using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PoshWpf.Commands
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
                  {
                     var windows = BootsWindowDictionary.Instance;
                     int[] keys = new int[windows.Count];
                     windows.Keys.CopyTo(keys, 0);
                     foreach (var k in keys)
                     {
                        var window = windows[k];
                        if (window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
                        {
                           foreach (var title in patterns)
                           {
                              if ((bool)window.Dispatcher.Invoke((Func<bool>)(() => title.IsMatch(window.Title))))
                              {
                                 WriteObject(window);
                              }
                           }
                        }
                     }
                  } break;
               default:
                  {
                     var windows = BootsWindowDictionary.Instance;
                     int[] keys = new int[windows.Count];
                     windows.Keys.CopyTo(keys, 0);
                     foreach (var k in keys)
                     {
                        WriteObject(windows[k]);
                     }
                  } break;
	         }
         }

         base.ProcessRecord();
      }

   }
}
