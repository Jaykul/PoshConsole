using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PoshWpf.Commands
{
	[Cmdlet(VerbsCommon.Get, "UIWindow", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ShowAll")]
	public class GetUIWindowCommand : PSCmdlet
	{
      [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByIndex")]
      public int[] Index { get; set; }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByTitle")]
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
			if (UIWindowDictionary.Instance.Count > 0)
         {
            switch (ParameterSetName)
	         {
               case "ByIndex":
                  foreach (var i in Index)
                  {
                     WriteObject(UIWindowDictionary.Instance[i]);
                  } break;
               case "ByTitle":
                  {
                     var windows = UIWindowDictionary.Instance;
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
                     var windows = UIWindowDictionary.Instance;
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
