using System;
using System.Management.Automation;
using System.Threading;
using PoshWpf.Utility;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsData.Export, "NamedElement", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = ByElement)]
   public class ExportNamedElementCommand : ScriptBlockBase
   {
      private const string ByTitle = "ByTitle";
      private const string ByIndex = "ByIndex";
      private const string ByElement = "ByElement";

      //[Parameter(Position = 0, Mandatory = true, ParameterSetName = ByElement, ValueFromPipeline = true)]
      //[ValidateNotNull]
      //[Alias("Window")]
      //public UIElement Element { get; set; }

      [Parameter(Position = 1, Mandatory = false)]
      [ValidateNotNullOrEmpty]
      public string Prefix { get; set; }

      private string _scope = "0";
      [Parameter(Mandatory = false)]
      [ValidateNotNullOrEmpty]
      [ValidatePattern("^(?:\\d+|Local|Script|Global|Private)$")]
      public string Scope
      {
         get { return _scope; }
         set { _scope = value; }
      }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      protected override void ProcessRecord()
      {
         try
         {
            // var active = Thread.CurrentThread;
            if (UIWindowDictionary.Instance.Count > 0)
            {
               foreach (var window in UIWindowDictionary.Instance.Values)
               {
                  // if (window.Dispatcher.Thread == active)
                  if (System.Windows.Threading.Dispatcher.CurrentDispatcher == window.Dispatcher)
                  {
                     ExportVisual(window, Scope);
                  }
               }
            }
         }
         catch (Exception ex)
         {
            WriteError(new ErrorRecord(ex, "TrappedException", ErrorCategory.NotSpecified, Thread.CurrentThread));
         }
         base.ProcessRecord();
      }



   }
}
