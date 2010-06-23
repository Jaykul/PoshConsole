using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Windows;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsCommon.Remove, "BootsWindow", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = "Cleanup")]
   public class RemoveBootsWindowCommand : PSCmdlet
   {
      [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByIndex")]
      public int[] Index { get; set; }

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByTitle", HelpMessage = "The window title to remove (accepts wildcards)")]
      public string[] Name { get; set; }

      [Parameter(Mandatory = true, ParameterSetName = "ByWindow", ValueFromPipeline = true)]
      public Window[] Window { get; set; }

      private List<WildcardPattern> patterns;
      protected override void BeginProcessing()
      {
         if (ParameterSetName == "ByTitle")
         {
            patterns = new List<WildcardPattern>(Name.Length);
            foreach (var title in Name)
            {
               patterns.Add(new WildcardPattern(title));
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
                     BootsWindowDictionary.Instance.Remove(i);
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
                                 windows.Remove(window);
                              }
                           }
                        }
                     }
                  } break;
               case "ByWindow":
                  {
                     for(int w =0;w < Window.Length;w++)
                     {
                        BootsWindowDictionary.Instance.Remove(Window[w]);
                     }
                  } break;
               default:
                  {
                     var windows = BootsWindowDictionary.Instance;
                     int[] keys = new int[windows.Count];
                     windows.Keys.CopyTo(keys, 0);
                     foreach (var k in keys)
                     {
                        var window = windows[k];
                        if (!window.Dispatcher.Thread.IsAlive || window.Dispatcher.HasShutdownStarted)
                        {
                           windows.Remove(window);
                        }
                     }
                  } break;
            }
         }
         base.ProcessRecord();
      }
   }
}
