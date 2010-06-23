using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace PoshWpf.Utility
{
   public abstract class ScriptBlockBase : PSCmdlet
   {
      //private Object _module;
      private string _moduleBase;

      protected bool? RunspaceAvailable { get; set; }

      protected string ScriptBase
      {
         get
         {
            if (String.IsNullOrEmpty(_moduleBase))
            {
               if (Invoker.Module != null)
               {
                  _moduleBase = Invoker.Module.ModuleBase;
               }
               if (String.IsNullOrEmpty(_moduleBase))
               {
                  _moduleBase = Environment.GetEnvironmentVariable("PSModulePath");
                  if (!String.IsNullOrEmpty(_moduleBase))
                     _moduleBase = _moduleBase.Split(new char[] { ';' }, 2).FirstOrDefault();
               }
               if (String.IsNullOrEmpty(_moduleBase))
               {
                  var name = MyInvocation.MyCommand.Name;
                  _moduleBase = (from cmdlet in Runspace.DefaultRunspace.RunspaceConfiguration.Cmdlets
                                 where cmdlet.Name.Equals(name)
                                 select cmdlet.PSSnapIn.ApplicationBase).FirstOrDefault();
               }
            }
            return _moduleBase;
         }
      }

      protected override void BeginProcessing()
      {
         RunspaceAvailable = Runspace.DefaultRunspace.RunspaceAvailability == RunspaceAvailability.Available;
         if (Invoker.Module == null)
         {
            // For those times when 2.0 is available, we need to grab the method...
            PropertyInfo moduleProp = typeof(CommandInfo).GetProperty("Module");
            if (moduleProp != null)
            {
               var _module = moduleProp.GetValue(MyInvocation.MyCommand, null) as PSModuleInfo;
               if( _module != null && (((ModuleType)_module.GetType().GetProperty("ModuleType").GetValue(_module, null)) == ModuleType.Script)) {
                  Invoker.Module = _module;
               }
            }
         }
         base.BeginProcessing();
      }

      /// <summary>
      /// Set a variable in the scope that our script blocks execute in
      /// </summary>
      /// <param name="name">The name of the variable</param>
      /// <param name="value">The value of the variable</param>
      /// <param name="scope">The scope of the variable (either a named scope: global, local, script ... or a number).</param>
      protected void ExportVariable(string name, object value, string scope)
      {
         int attempts = 0;
         while (attempts < 2)
         {
            using (var pipe = attempts == 0 ? Runspace.DefaultRunspace.CreatePipeline() : Runspace.DefaultRunspace.CreateNestedPipeline())
            {
               attempts = attempts + 1;
               var cmd = new Command("Set-Variable", false, false);
               cmd.Parameters.Add("Name", name);
               cmd.Parameters.Add("Value", value);
               cmd.Parameters.Add("Scope", scope);
               cmd.Parameters.Add("Option", "AllScope");
               pipe.Commands.Add(cmd);
               pipe.Invoke();
               pipe.Stop();
               return;
            }
         }
         WriteError(new ErrorRecord(new InvalidOperationException("Can't export variables here"),
                                    "Cannot create pipeline to set variable", ErrorCategory.DeadlockDetected, Runspace.DefaultRunspace));
      }

      // Enumerate all the descendants of the visual object.
      protected void ExportVisual(Visual myVisual, string scope)
      {
         for (int i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++)
         {
            // Retrieve child visual at specified index value.
            Visual childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);
            if (childVisual is FrameworkElement)
            {
               // Do processing of the child visual object.
               string name = childVisual.GetValue(FrameworkElement.NameProperty) as string;
               if (!string.IsNullOrEmpty(name) && Dispatcher.CurrentDispatcher.CheckAccess())
               {
                  ExportVariable(name, childVisual, scope);
               }
            }
            // Enumerate children of the child visual object.
            ExportVisual(childVisual, scope);
         }
      }

      protected ICollection<PSObject> InvokeNested(ScriptBlock sb, PSVariable[] variables, params object[] args)
      {
         return Invoker.Invoke(sb, variables, args);
      }
      
      protected ICollection<PSObject> Invoke(ScriptBlock sb, PSVariable[] variables, params object[] args)
      {
         return Invoker.Invoke(sb, variables, args);
      }

      protected ICollection<PSObject> Invoke(ScriptBlock sb, params object[] args)
      {
         return Invoker.Invoke(sb, args);
      }
   }
}
