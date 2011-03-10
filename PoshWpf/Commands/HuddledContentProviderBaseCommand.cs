using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
namespace PoshWpf.Commands
{
	public abstract class HuddledContentProviderBaseCommand : HuddledProviderBaseCommand
   {
		protected override void ProcessRecord()
		{
			if (ProviderPaths == null || ProviderPaths.Count == 0)
			{
				ProviderPaths = ResolveProviderPaths(IsContentProvider);
			}
			base.ProcessRecord();
		}

		protected Dictionary<string,IContentReader> GetReaders()
		{
			return ProviderPaths.ToDictionary(path => path, path => TryGetReader(path));
		}


		protected Dictionary<string, IContentWriter> GetWriters()
		{
			return ProviderPaths.ToDictionary(path => path, path => TryGetWriter(path));
		}

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Not suppressing the exception, just moving it into an ErrorRecord for PowerShell")]
        protected IContentReader TryGetReader(string path)
		{
			try {
				return InvokeProvider.Content.GetReader(WildcardPattern.Escape(path)).Single();
			} 
			catch(Exception ex)
			{
				WriteError( new ErrorRecord(ex,"CantGetReader",ErrorCategory.ReadError,path));
			}
			return null;
		}

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Not suppressing the exception, just moving it into an ErrorRecord for PowerShell")]
        protected IContentWriter TryGetWriter(string path)
		{
			try
			{
				return InvokeProvider.Content.GetWriter(WildcardPattern.Escape(path)).Single();
				// return InvokeProvider.Content.GetWriter(new[]{WildcardPattern.Escape(path)}, true, true ).Single();
			} 
			catch(Exception ex)
			{
				WriteError( new ErrorRecord(ex,"CantGetWriter",ErrorCategory.ReadError,path));
			}
			return null;
		}
   }
}
