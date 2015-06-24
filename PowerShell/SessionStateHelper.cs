namespace PoshCode.PowerShell
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;

    /// <summary>
    /// The SessionState Helper contains extension methods for working with SessionState or InitialSessionState objects
    /// </summary>
    public static class SessionStateHelper
    {
        /// <summary>
        /// Loads any cmdlets defined in the specified types
        /// </summary>
        /// <param name="iss">The InitialSessionState.</param>
        /// <param name="types">The types which might be cmdlets.</param>
        /// <returns>The InitialSessionState with the commands added.</returns>
        public static InitialSessionState LoadCmdlets(this InitialSessionState iss, IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                var cmdlets = t.GetCustomAttributes(typeof(CmdletAttribute), false) as CmdletAttribute[];
                if (cmdlets != null)
                {
                    foreach (CmdletAttribute cmdlet in cmdlets)
                    {
                        iss.Commands.Add(
                            new SessionStateCmdletEntry(
                                string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t, string.Format("{0}.xml", t.Name)));
                    }
                }
            }

            return iss;
        }

        /// <summary>
        /// Loads any cmdlets defined in the specified assembly
        /// </summary>
        /// <param name="iss">The InitialSessionState.</param>
        /// <param name="assembly">The assembly which contains cmdlets.</param>
        /// <returns>The InitialSessionState with the commands added.</returns>
        public static InitialSessionState LoadCmdlets(this InitialSessionState iss, Assembly assembly)
        {
            return LoadCmdlets(iss, assembly.GetTypes());
        }
    }
}