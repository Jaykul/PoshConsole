using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
namespace PoshWpf
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
			var readers = new Dictionary<string, IContentReader>();
			foreach (var path in ProviderPaths)
			{
				readers.Add(path, TryGetReader(path));
			}
			return readers;
		}


		protected Dictionary<string, IContentWriter> GetWriters()
		{
			var writers = new Dictionary<string, IContentWriter>();
			foreach (var path in ProviderPaths)
			{
				writers.Add(path, TryGetWriter(path));
			}
			return writers;
		}

		protected IContentReader TryGetReader(string path)
		{
			try {
				return InvokeProvider.Content.GetReader(WildcardPattern.Escape(path)).Single();
			} 
			catch(Exception ex)
			{
				WriteError( new ErrorRecord(ex,"CantGetContent",ErrorCategory.ReadError,path));
			}
			return null;
		}

		protected IContentWriter TryGetWriter(string path)
		{
			try
			{
				return InvokeProvider.Content.GetWriter(WildcardPattern.Escape(path)).Single();
			} 
			catch(Exception ex)
			{
				WriteError( new ErrorRecord(ex,"CantGetContent",ErrorCategory.ReadError,path));
			}
			return null;
		}
   }
}
