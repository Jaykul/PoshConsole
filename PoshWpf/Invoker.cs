using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PoshWpf
{
   public class Invoker
   {
      private static PSModuleInfo _module;
      private static PSModuleInfo _coModule;
      internal static PSModuleInfo Module
      {
         get { return _module;  }
         set { _module = value; }
      }
      internal static PSModuleInfo CoModule
      {
         get { return _coModule;  }
         set { _coModule = value; }
      }

      /// <summary>
      /// Invoke a <see cref="ScriptBlock"/>, binding it to the module, if possible.
      /// </summary>
      /// <param name="sb">The <see cref="ScriptBlock"/></param>
      /// <param name="variables">Variables to set before invoking</param>
      /// <param name="args">Arguments to the <see cref="ScriptBlock"/></param>
      /// <returns>A collection of <see cref="PSObject"/></returns>
      internal static ICollection<PSObject> Invoke(ScriptBlock sb, PSVariable[] variables, params object[] args)
      {
         if (variables == null)
            throw new ArgumentNullException("variables");

         foreach (var v in variables) SetScriptVariable(v);

         if (_module != null)
         {
            sb = _module.NewBoundScriptBlock(sb);
         }
         return sb.Invoke(args);
      }

      /// <summary>
      /// Invoke a <see cref="ScriptBlock"/>, binding it to the module, if possible.
      /// </summary>
      /// <param name="sb">The <see cref="ScriptBlock"/></param>
      /// <param name="args">Arguments to the <see cref="ScriptBlock"/></param>
      /// <returns>A collection of <see cref="PSObject"/></returns>
      internal static ICollection<PSObject> Invoke(ScriptBlock sb, params object[] args)
      {
         if (_module != null)
         {
            sb = _module.NewBoundScriptBlock(sb);
         }
         return sb.Invoke(args);
      }

      /// <summary>
      /// Invoke a <see cref="ScriptBlock"/>, binding it to the module, if possible.
      /// </summary>
      /// <param name="script">The <see cref="ScriptBlock"/> as a string</param>
      /// <param name="variables">Variables to set before invoking</param>
      /// <param name="args">Arguments to the <see cref="ScriptBlock"/></param>
      /// <returns>A collection of <see cref="PSObject"/></returns>
      internal static ICollection<PSObject> Invoke(string script, PSVariable[] variables, params object[] args)
      {
         return Invoke(ScriptBlock.Create(script), variables, args);
      }

      /// <summary>
      /// Invoke a <see cref="ScriptBlock"/>, binding it to the module, if possible.
      /// </summary>
      /// <param name="script">The <see cref="ScriptBlock"/> as a string</param>
      /// <param name="args">Arguments to the <see cref="ScriptBlock"/></param>
      /// <returns>A collection of <see cref="PSObject"/></returns>
      internal static ICollection<PSObject> Invoke(String script, params object[] args)
      {
         return Invoke(ScriptBlock.Create(script), args);
      }

      /// <summary>
      /// Set a variable in the scope that our script blocks execute in
      /// </summary>
      /// <param name="name">The name of the variable</param>
      /// <param name="value">The value of the variable</param>
      /// <param name="scope">The scope of the variable (either a named scope: global, local, script ... or a number).</param>
      internal static void SetScriptVariableValue(string name, object value, string scope)
      {
         if (_module != null)
         {
            Pipeline pipe;
            switch (Runspace.DefaultRunspace.RunspaceAvailability)
            {
               case RunspaceAvailability.Available:
                  pipe = Runspace.DefaultRunspace.CreatePipeline();
                  break;
               case RunspaceAvailability.AvailableForNestedCommand:
               case RunspaceAvailability.Busy:
                  pipe = Runspace.DefaultRunspace.CreateNestedPipeline();
                  break;
               default:
                  throw new InvalidPipelineStateException();
            }
            
            var cmd = new Command("Set-Variable",false,false);
            cmd.Parameters.Add("Name", name);
            cmd.Parameters.Add("Value", value);
            cmd.Parameters.Add("Scope", scope);
            cmd.Parameters.Add("Option", "AllScope");
            pipe.Commands.Add(cmd);
            var results = pipe.Invoke();
            pipe.Dispose();
            //var v = new PSVariable(name, value, ScopedItemOptions.AllScope);
            //_module.SessionState.PSVariable.Set(v);
            //_module.ExportedVariables.Add(name,v);
         }
      }

      /// <summary>
      /// Set a variable in the scope that our script blocks execute in
      /// </summary>
      /// <param name="variable"></param>
      internal static void SetScriptVariable(PSVariable variable)
      {
         if (_module != null)
         {
            variable.Options = ScopedItemOptions.AllScope;
            _module.SessionState.PSVariable.Set(variable);
         }
      }
      /// <summary>
      /// Get the value of a variable from the scope that our script blocks execute in
      /// </summary>
      internal static object GetScriptVariableValue(string name, object defaultValue)
      {
         return _module != null ? _module.SessionState.PSVariable.GetValue(name, defaultValue) : null;
      }
   }
}