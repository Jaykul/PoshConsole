using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Windows;
using System.Windows.Threading;
using PoshWpf.Utility;

namespace PoshWpf.Commands
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
            IEnumerable<ICollection<PSObject>> output = null;
            switch (ParameterSetName)
            {
               case ByIndex:
                  output = ProcessIndexRecord(); break;
               case ByTitle:
                  output = ProcessTitleRecord(); break;
               case ByElement:
                  output = ProcessElementRecord(); break;
            }
            if (output != null)
               foreach(var o in output)
                  WriteObject(o,true);

            if (_error != null) WriteError(_error);
         }

         base.ProcessRecord();
      }

      private IEnumerable<ICollection<PSObject>> ProcessElementRecord()
      {
        return from element in Element
               where element.Dispatcher.Thread.IsAlive && !element.Dispatcher.HasShutdownStarted
               let uie = element
               let window = element.Dispatcher.Invoke((Func<Window>) (() => Window.GetWindow(uie))) as Window
               let vars = new[]{ new PSVariable("this", window), new PSVariable("window", element) }
               select Invoke(window, vars, Parameters)
               into result where result != null && result.Count > 0
               select result;

      }

      private IEnumerable<ICollection<PSObject>> ProcessTitleRecord()
      {
         return from win in BootsWindowDictionary.Instance.Values
                where win.Dispatcher.Thread.IsAlive && !win.Dispatcher.HasShutdownStarted
                from title in _patterns
                select (ICollection<PSObject>)win.Dispatcher.Invoke((Func<ICollection<PSObject>>) (() =>
                     {
                        if (!title.IsMatch(win.Title))
                           return null;

                        // don't need to do Window.GetWindow(window1) if they're using ByTitle, because it MUST be a root window
                        var vars = new[] { new PSVariable("this", win), new PSVariable("window", win) };
                        return Invoke(Script, vars, Parameters);
                     }))
               into result where result != null && result.Count > 0
               select result;
      }

      private IEnumerable<ICollection<PSObject>> ProcessIndexRecord()
      {
         return from i in Index
                select BootsWindowDictionary.Instance[i]
                into window
                where window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted
                let vars = new[]
                              {
                                 new PSVariable("this", window), new PSVariable("window", window)
                              }
                select Invoke(window, vars, Parameters)
                into result
                where result != null && result.Count > 0
                select result;
      }


      private ICollection<PSObject> Invoke(DispatcherObject uie, PSVariable[] variables, params object[] arguments)
      {
         return uie.Dispatcher.Invoke((InvokeDelegate)Invoker, variables, arguments) as ICollection<PSObject>;
      }

      delegate ICollection<PSObject> InvokeDelegate(PSVariable[] variables, params object[] arguments);


      ErrorRecord _error;
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      private ICollection<PSObject> Invoker(PSVariable[] variables, params object[] arguments)
      {
         return InvokeNested(Script, variables, arguments);
      }

   }
}
