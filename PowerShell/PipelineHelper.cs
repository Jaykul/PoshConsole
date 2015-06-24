using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoshCode.PowerShell
{

    internal static class PipelineHelper
    {

        #region [rgn] Methods (2)

        // [rgn] Public Methods (2)

        public static bool IsDone(this System.Management.Automation.Runspaces.PipelineStateInfo psi)
        {
            return
                psi.State == System.Management.Automation.Runspaces.PipelineState.Completed ||
                psi.State == System.Management.Automation.Runspaces.PipelineState.Stopped ||
                psi.State == System.Management.Automation.Runspaces.PipelineState.Failed;
        }

        public static bool IsFailed(this System.Management.Automation.Runspaces.PipelineStateInfo info)
        {
            return info.State == System.Management.Automation.Runspaces.PipelineState.Failed;
        }

        #endregion [rgn]

    }
}
