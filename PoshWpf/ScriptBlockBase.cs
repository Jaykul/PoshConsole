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
      private Object _module;
      //private static PSModuleInfo _module;
      private MethodInfo _newBoundScriptBlockMethod;
      private SessionState _sessionState;
      private string _moduleBase;

      protected ICollection<PSObject> Invoke(ScriptBlock sb, PSVariable[] variables, params object[] args)
      {
         if(variables == null) 
            throw new ArgumentNullException("variables");
            
         foreach (var v in variables) SetScriptVariable(v);

         if (_module != null)
         {
            sb = (ScriptBlock)_newBoundScriptBlockMethod.Invoke(_module, new[] { sb });
         }
         return sb.Invoke(args);
      }

      protected ICollection<PSObject> Invoke(ScriptBlock sb, params object[] args)
      {
         if (_module != null)
         {
            sb = (ScriptBlock)_newBoundScriptBlockMethod.Invoke(_module, new[] { sb });
         }
         return sb.Invoke(args);
      }

      protected string ScriptBase
      {
         get
         {
            if (String.IsNullOrEmpty(_moduleBase))
            {
               if (_module != null)
               {
                  _moduleBase = _module.GetType().GetProperty("ModuleBase").GetValue(_module, null) as string;
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
      /// <summary>
      /// Set a variable in the scope that our script blocks execute in
      /// </summary>
      /// <param name="name"></param>
      /// <param name="value"></param>
      protected void SetScriptVariableValue(string name, object value)
      {
         _sessionState.PSVariable.Set(name, value);
      }

      /// <summary>
      /// Set a variable in the scope that our script blocks execute in
      /// </summary>
      /// <param name="variable"></param>
      protected void SetScriptVariable(PSVariable variable)
      {
         _sessionState.PSVariable.Set(variable);
      }
      /// <summary>
      /// Get the value of a variable from the scope that our script blocks execute in
      /// </summary>
      protected object GetScriptVariableValue(string name, object defaultValue)
      {
         return _sessionState.PSVariable.GetValue(name, defaultValue);
      }


      protected override void BeginProcessing()
      {
         if (_module == null)
         {
            // For those times when 2.0 is available, we need to grab the method...
            PropertyInfo moduleProp = typeof(CommandInfo).GetProperty("Module");
            if (moduleProp != null)
            {
               _module = moduleProp.GetValue(MyInvocation.MyCommand, null);
               if( _module == null || (((ModuleType)_module.GetType().GetProperty("ModuleType").GetValue(_module, null)) != ModuleType.Script)) {
                  _module = null;
                  _sessionState = SessionState;
               } else {
                  _newBoundScriptBlockMethod = _module.GetType().GetMethod("NewBoundScriptBlock");
                  _sessionState = _module.GetType().GetProperty("SessionState").GetValue(_module, null) as SessionState;
               }
            }
         }

         base.BeginProcessing();
      }


   }
}
