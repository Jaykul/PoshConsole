using System.IO;
using System.Management.Automation;
using PoshWpf.Utility;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsCommon.Add, "BootsTemplate", SupportsShouldProcess = true, DefaultParameterSetName="Path")]
   public class AddBootsTemplateCommand : HuddledFileSystemBaseCommand
   {
		protected override void ProcessRecord()
		{
			base.ProcessRecord();
			// at this point, we have a list of paths on the filesystem.
			foreach (string filePath in ProviderPaths)
			{
				// If -whatif was supplied, do not perform the actions
				// inside this "if" statement; only show the message.
				//
				// This block also supports the -confirm switch, where
				// you will be asked if you want to perform the action
				// "get metadata" on target: foo.txt
				if (ShouldProcess(filePath, "Add Boots Template"))
				{
					if (File.Exists(filePath))
					{
						XamlHelper.AddDataTemplate(filePath);
					}
				}
			}
		}
   }
}
