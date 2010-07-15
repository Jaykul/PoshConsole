using System;
using System.Collections.Generic;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;
using PoshWpf.Utility;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsCommon.Remove, "BootsTemplate", SupportsShouldProcess = true, DefaultParameterSetName = "Path")]
   public class RemoveBootsTemplateCommand : PSCmdlet
   {
      private const string Noun = "BootsTemplate";
      private const string ParamSetLiteral = "Literal";
      private const string ParmamSetPath = "Path";

      private string[] _paths;
      private bool _shouldExpandWildcards;
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(
          Position = 0,
          Mandatory = true,
          ValueFromPipeline = false,
          ValueFromPipelineByPropertyName = true,
          ParameterSetName = ParamSetLiteral)
      ]
      [Alias("PSPath")]
      [ValidateNotNullOrEmpty]
      public string[] LiteralPath
      {
         get { return _paths; }
         set { _paths = value; }
      }


      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(
          Position = 0,
          Mandatory = true,
          ValueFromPipeline = true,
          ValueFromPipelineByPropertyName = true,
          ParameterSetName = ParmamSetPath)
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
      protected override void ProcessRecord()
      {
         foreach (string path in _paths)
         {
            // This will hold information about the provider containing
            // the items that this path string might resolve to.                
            ProviderInfo provider;
            // This will be used by the method that processes literal paths
            PSDriveInfo drive;
            // this contains the paths to process for this iteration of the
            // loop to resolve and optionally expand wildcards.
            List<string> filePaths = new List<string>();
            if (_shouldExpandWildcards)
            {
               // Turn *.txt into foo.txt,foo2.txt etc.
               // if path is just "foo.txt," it will return unchanged.
               filePaths.AddRange(this.GetResolvedProviderPathFromPSPath(path, out provider));
            }
            else
            {
               // no wildcards, so don't try to expand any * or ? symbols.                    
               filePaths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                   path, out provider, out drive));
            }
            // ensure that this path (or set of paths after wildcard expansion)
            // is on the filesystem. A wildcard can never expand to span multiple
            // providers.
            if (IsFileSystemPath(provider, path) == false)
            {
               // no, so skip to next path in _paths.
               continue;
            }
            // at this point, we have a list of paths on the filesystem.
            foreach (string filePath in filePaths)
            {
               // If -whatif was supplied, do not perform the actions
               // inside this "if" statement; only show the message.
               //
               // This block also supports the -confirm switch, where
               // you will be asked if you want to perform the action
               // "get metadata" on target: foo.txt
               if (ShouldProcess(filePath, "Add Boots Template"))
               {
                  XamlHelper.RemoveDataTemplate(filePath);
               }
            }
         }
      }

      //private PSObject GetFileCustomObject(FileInfo file)
      //{
      //   // this message will be shown if the -verbose switch is given
      //   WriteVerbose("GetFileCustomObject " + file);
      //   // create a custom object with a few properties
      //   PSObject custom = new PSObject();
      //   custom.Properties.Add(new PSNoteProperty("Size", file.Length));
      //   custom.Properties.Add(new PSNoteProperty("Name", file.Name));
      //   custom.Properties.Add(new PSNoteProperty("Extension", file.Extension));
      //   return custom;
      //}

      //private PSObject GetDirectoryCustomObject(DirectoryInfo dir)
      //{
      //   // this message will be shown if the -verbose switch is given
      //   WriteVerbose("GetDirectoryCustomObject " + dir);
      //   // create a custom object with a few properties
      //   PSObject custom = new PSObject();
      //   int files = dir.GetFiles().Length;
      //   int subdirs = dir.GetDirectories().Length;
      //   custom.Properties.Add(new PSNoteProperty("Files", files));
      //   custom.Properties.Add(new PSNoteProperty("Subdirectories", subdirs));
      //   custom.Properties.Add(new PSNoteProperty("Name", dir.Name));
      //   return custom;
      //}

      private bool IsFileSystemPath(ProviderInfo provider, string path)
      {
         bool isFileSystem = true;
         // check that this provider is the filesystem
         if (provider.ImplementingType != typeof(FileSystemProvider))
         {
            // create a .NET exception wrapping our error text
            ArgumentException ex = new ArgumentException(path +
                " does not resolve to a path on the FileSystem provider.");
            // wrap this in a powershell errorrecord
            ErrorRecord error = new ErrorRecord(ex, "InvalidProvider",
                ErrorCategory.InvalidArgument, path);
            // write a non-terminating error to pipeline
            this.WriteError(error);
            // tell our caller that the item was not on the filesystem
            isFileSystem = false;
         }
         return isFileSystem;
      }
   }
}
