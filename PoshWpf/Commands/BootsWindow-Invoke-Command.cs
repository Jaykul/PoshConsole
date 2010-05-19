using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Windows;

namespace PoshWpf
{
   [Cmdlet("Invoke", "BootsWindow", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = ByElement)]
   public class InvokeBootsWindowCommand : ScriptBlockBase
   {
      private const string ByTitle = "ByTitle";
      private const string ByIndex = "ByIndex";
      private const string ByElement = "ByElement";

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByIndex, ValueFromPipeline = true)]
      public int[] Index { get; set; }

      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByTitle, ValueFromPipelineByPropertyName = true)]
      [Alias("Name")]
      public string[] Title { get; set; }


      [Parameter(Position = 0, Mandatory = true, ParameterSetName = ByElement, ValueFromPipeline = true)]
      [Alias("Window")]
      public UIElement[] Element { get; set; }

      [Parameter(Position = 1, Mandatory = true)]
      public ScriptBlock Script { get; set; }

      [Parameter(Position = 2, Mandatory = false, ValueFromRemainingArguments = true)]
      [Alias("Args", "Params")]
      public PSObject[] Parameters { get; set; }

      private List<WildcardPattern> _patterns;
      protected override void BeginProcessing()
      {
         if (ParameterSetName == ByTitle)
         {
            _patterns = new List<WildcardPattern>(Title.Length);
            foreach (var title in Title)
            {
               _patterns.Add(new WildcardPattern(title));
            }
         }
         base.BeginProcessing();
      }

      protected override void ProcessRecord()
      {
         WriteCommandDetail(Script.ToString());
         if (BootsWindowDictionary.Instance.Count > 0)
         {
            switch (ParameterSetName)
            {
               case ByIndex:
                  foreach (var i in Index)
                  {
                     var window = BootsWindowDictionary.Instance[i];
                     if (!window.Dispatcher.Thread.IsAlive || window.Dispatcher.HasShutdownStarted) continue;
                     // don't need to do Window.GetWindow(window) if they're using ByTitle, because it MUST be a root window
                     var vars = new[] { 
                        new PSVariable("this", window),
                        new PSVariable("window", window) 
                     };
                     var result = (ICollection<PSObject>)window.Dispatcher.Invoke(((Func<PSVariable[], Object[], ICollection<PSObject>>)Invoker), vars, Parameters);
                     if (result != null && result.Count > 0)
                        WriteObject(result, true);
                  } break;
               case ByTitle:
                  foreach (var win in BootsWindowDictionary.Instance.Values)
                  {
                     if (!win.Dispatcher.Thread.IsAlive || win.Dispatcher.HasShutdownStarted) continue;
                     Window window = win;
                     foreach (var result in from title in _patterns
                                            let title1 = title
                                            select (ICollection<PSObject>) window.Dispatcher.Invoke((Func<ICollection<PSObject>>) (() =>
                                            {
                                                if (!title1.IsMatch(window.Title))
                                                   return null;

                                                // don't need to do Window.GetWindow(window1) if they're using ByTitle, because it MUST be a root window
                                                var vars = new[]{ new PSVariable("this", window), new PSVariable("window", window) };
                                                return Invoker(vars, Parameters);
                                             }))
                                            into result where result != null && result.Count > 0 
                                            select result)
                     {
                        WriteObject(result, true);
                     }
                  }
                  break;
               case ByElement:
                  foreach (var output in from element in Element
                                         where element.Dispatcher.Thread.IsAlive && !element.Dispatcher.HasShutdownStarted
                                         let uie = element
                                         let window = element.Dispatcher.Invoke((Func<Window>) (() => Window.GetWindow(uie))) as Window
                                         let vars = new[]{ new PSVariable("this", window), new PSVariable("window", element) }
                                         select (ICollection<PSObject>) element.Dispatcher.Invoke(((Func<PSVariable[], Object[], ICollection<PSObject>>) Invoker), vars, Parameters)
                                         into result where result != null && result.Count > 0
                                         select result)
                  {
                     WriteObject(output, true);
                  } break;
            }

            if (_error != null)
            {
               WriteError(_error);
            }
         }

         base.ProcessRecord();
      }

      ErrorRecord _error;
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      private ICollection<PSObject> Invoker(PSVariable[] variables, params object[] arguments)
      {
         ICollection<PSObject> result = null;
         try
         {
            result = Invoke(Script, variables, arguments);
         }
         catch (Exception ex)
         {
            _error = new ErrorRecord(ex, "Error during invoke", ErrorCategory.OperationStopped, Script);
         }
         return result;
      }

   }
}
