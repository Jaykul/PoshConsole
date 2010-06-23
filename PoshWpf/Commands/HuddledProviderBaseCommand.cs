using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Microsoft.PowerShell.Commands;
namespace PoshWpf.Commands
{
   public abstract class HuddledProviderBaseCommand : PSCmdlet
   {
      protected const string ParameterSetLiteral = "Literal";
      protected const string ParameterSetPath = "Path";
		protected List<string> ProviderPaths;

      private string[] _paths;
      private bool _shouldExpandWildcards;

      [Parameter(
          Position = 0,
          Mandatory = true,
          ValueFromPipeline = false,
          ValueFromPipelineByPropertyName = true,
          ParameterSetName = ParameterSetLiteral,
			 HelpMessage = "Specifies the literal path of the file to process. Wildcard syntax is not handled.")
      ]
      [Alias("PSPath")]
      [ValidateNotNullOrEmpty]
      public string[] LiteralPath
      {
         get { return _paths; }
         set { _paths = value; }
      }


      [Parameter(
          Position = 0,
          Mandatory = true,
          ValueFromPipeline = true,
          ValueFromPipelineByPropertyName = true,
          ParameterSetName = ParameterSetPath,
			 HelpMessage = "Specifies the path of the file to process. Wildcard syntax is allowed.")
      ]
      [ValidateNotNullOrEmpty]
      public string[] Path
      {
         get { return _paths; }
         set
         {
            _shouldExpandWildcards = true;
            _paths = value;
         }
      }

		[Parameter()]
		public SwitchParameter Exists { get; set; }


		protected override void ProcessRecord()
		{
			if (ProviderPaths == null || ProviderPaths.Count == 0)
			{
				ProviderPaths = ResolveProviderPaths(null);
			}
			base.ProcessRecord();
		}


		protected List<string> ResolveProviderPaths()
		{
			return ResolveProviderPaths(null);
		}

   	protected List<string> ResolveProviderPaths(Func<ProviderInfo, string, bool> providerConstraint)
   	{
			var resolvedPaths = new List<string>();
			foreach (string path in _paths)
   		{
   			// This will hold information about the provider containing
   			// the items that this path string might resolve to.                
   			ProviderInfo provider;
   			// this contains the paths to process for this iteration of the
   			// loop to resolve and optionally expand wildcards.
   			if (_shouldExpandWildcards)
   			{
   				// Turn *.txt into foo.txt,foo2.txt etc.
   				// if path is just "foo.txt," it will return unchanged.
					try
					{
						var providerPaths = SessionState.Path.GetResolvedPSPathFromPSPath(path);
						// ensure that this path or set of paths is on the filesystem. 
						// FYI: A wildcard can never expand to span multiple providers.
						if (providerConstraint == null || providerConstraint(providerPaths.First().Provider, path))
						{
							resolvedPaths.AddRange(from p in providerPaths select p.Path);
						}
						else continue;
					}
					catch (ItemNotFoundException ex)
					{
						// does the input contain wildcards? should it -Exist?
						if (!WildcardPattern.ContainsWildcardCharacters(ex.ItemName) && !Exists)
						{
							// no wildcards, so don't try to expand any * or ? symbols.                    
							PSDriveInfo drive;
							var literal = SessionState.Path.GetUnresolvedProviderPathFromPSPath(ex.ItemName, out provider, out drive);
							// ensure that this path is on an acceptable provider. 
							if (providerConstraint != null && !providerConstraint(provider, path))
								continue;

							// Ensure we have a provider-qualified path
							if (!SessionState.Path.IsProviderQualified(literal))
							{
								literal = provider.Name + "::" + literal;
							}
							resolvedPaths.Add(literal);
						}
					}
   			}
   			else
   			{
   				// no wildcards, so don't try to expand any * or ? symbols.                    
   				PSDriveInfo drive;
					
					var literal = SessionState.Path.GetUnresolvedProviderPathFromPSPath(path, out provider, out drive);
					// ensure that this path is on an acceptable provider. 
					if (providerConstraint != null && !providerConstraint(provider, path))
						continue;

					// Ensure we have a provider-qualified path
					if (!SessionState.Path.IsProviderQualified(literal))
					{
						literal = provider.Name + "::" + literal;
					}
					resolvedPaths.Add(literal);
				}
   		}
			return resolvedPaths;
		}

		/// <summary>
		/// Determine if the provider is the FileSystemProvider.
		/// If it isn't, write an error using the specified path
		/// </summary>
		/// <param name="provider">Provider to test</param>
		/// <param name="path">Path to write in the error</param>
		/// <returns>True if the provider is the FileSystemProvider, false otherwise.</returns>
      protected bool IsFileSystemProvider(ProviderInfo provider, string path)
      {
         // check that this provider is the filesystem
			if (!typeof(FileSystemProvider).IsAssignableFrom(provider.ImplementingType))
         {
            // create a .NET exception wrapping our error text
            var ex = new ArgumentException(path + " does not resolve to a path on the FileSystem provider.");
            // wrap this in a powershell errorrecord
            var error = new ErrorRecord(ex, "InvalidProvider", ErrorCategory.InvalidArgument, path);
            // write a non-terminating error to pipeline
            WriteError(error);
            // tell our caller that the item was not on the filesystem
            return false;
         }
         return true;
      }


		/// <summary>
		/// Determine if the provider is a ContentProvider.
		/// If it isn't, write an error using the specified path
		/// </summary>
		/// <param name="provider">Provider to test</param>
		/// <param name="path">Path to write in the error</param>
		/// <returns>True if the provider is the FileSystemProvider, false otherwise.</returns>
		protected bool IsContentProvider(ProviderInfo provider, string path)
		{
			// check that this provider is the filesystem
			if (!typeof(IContentCmdletProvider).IsAssignableFrom(provider.ImplementingType))
			{
				// create a .NET exception wrapping our error text
				var ex = new ArgumentException(path + " does not resolve to a path on a Content provider.");
				// wrap this in a powershell errorrecord
				var error = new ErrorRecord(ex, "ContentAccessNotSupported", ErrorCategory.InvalidArgument, path);
				// write a non-terminating error to pipeline
				WriteError(error);
				// tell our caller that the item was not on the filesystem
				return false;
			}
			return true;
		}
   }
}
