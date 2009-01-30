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
   [Cmdlet("Invoke", "BootsWindow", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByIndex")]
   public class InvokeBootsWindowCommand : PSCmdlet
   {
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByIndex")]
      public int[] Index { get; set; }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByTitle")]
      public string[] Name { get; set; }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(Position = 0, Mandatory = true, ParameterSetName = "ByWindow")]
      public Window[] Window { get; set; }

      [Parameter(Position = 1, Mandatory = true)]
      public ScriptBlock Script { get; set; }

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
                     WriteObject(BootsWindowDictionary.Instance[i].Dispatcher.Invoke(((Func<Collection<PSObject>>)Invoker)));
                  } break;
               case "ByTitle":
                  foreach (var window in BootsWindowDictionary.Instance.Values)
                  {
                     foreach (var title in patterns)
	                  {
                        if(title.IsMatch( window.Title )) {
                           WriteObject(window.Dispatcher.Invoke(((Func<Collection<PSObject>>)Invoker)));
                        }
                     }
                  } break;
         		case "ByWindow":
                  foreach (var window in Window)
                  {
                     WriteObject(window.Dispatcher.Invoke(((Func<Collection<PSObject>>)Invoker)));
                  } break;
	         }

            if(_error != null) {
               WriteError(_error);
            }
         }

         base.ProcessRecord();
      }

      ErrorRecord _error;
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      private Collection<PSObject> Invoker()
      {
         Collection<PSObject> result = null;
         try
         {
            result = Script.Invoke();
         }
         catch (Exception ex)
         {
            _error = new ErrorRecord(ex, "Error during invoke", ErrorCategory.OperationStopped, Script);
         }
         return result;
      }

   }
}
