using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Microsoft.PowerShell.Commands;
namespace PoshWpf
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
