using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Windows;
using System.IO;
using System.Xml;
using System.Windows.Threading;
using System.Reflection;

namespace PoshWpf
{
   public abstract class ScriptBlockBase : PSCmdlet
   {
      //private Object _module;
      private string _moduleBase;

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
