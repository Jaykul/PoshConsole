using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PoshWpf
{
   public class Invoker
   {
      private static PSModuleInfo _module;
      internal static PSModuleInfo Module
      {
         get { return _module;  }
         set { _module = value; }
      }

      internal static bool? Nested;

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
      /// Set a variable in the module scope (for script blocks called through <see cref="Invoker"/>)
      /// </summary>
      /// <param name="name"></param>
      /// <param name="value"></param>
      internal static void SetScriptVariableValue(string name, object value)
      {
         if (_module != null)
            _module.SessionState.PSVariable.Set(name, value);
      }


      /// <summary>
      /// Set a variable in the module scope (for script blocks called through <see cref="Invoker"/>)
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
      /// Get the value of a variable from the module scope (after script blocks called through <see cref="Invoker"/>)
      /// </summary>
      internal static object GetScriptVariableValue(string name, object defaultValue)
      {
         return _module != null ? _module.SessionState.PSVariable.GetValue(name, defaultValue) : null;
      }
   }
}