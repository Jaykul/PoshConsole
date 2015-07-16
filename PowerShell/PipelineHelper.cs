using System.Management.Automation.Runspaces;

namespace PoshCode.PowerShell
{

    internal static class PipelineHelper
    {

        #region [rgn] Methods (2)

        // [rgn] Public Methods (2)

        public static bool IsDone(this PipelineStateInfo psi)
        {
            return
                psi.State == PipelineState.Completed ||
                psi.State == PipelineState.Stopped ||
                psi.State == PipelineState.Failed;
        }

        public static bool IsFailed(this PipelineStateInfo info)
        {
            return info.State == PipelineState.Failed;
        }

        #endregion [rgn]

    }
}
