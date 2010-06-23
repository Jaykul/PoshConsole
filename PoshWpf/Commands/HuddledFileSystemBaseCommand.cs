namespace PoshWpf.Commands
{
   public abstract class HuddledFileSystemBaseCommand : HuddledProviderBaseCommand
   {

		protected override void ProcessRecord()
		{
			if(ProviderPaths == null || ProviderPaths.Count == 0)
			{
				ProviderPaths = ResolveProviderPaths(IsFileSystemProvider);
			}
			base.ProcessRecord();
		}
   }
}
