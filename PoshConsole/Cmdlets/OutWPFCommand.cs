using System;
using System.IO;
using System.Management.Automation;
using System.Xml;

namespace PoshConsole.Cmdlets
{
   [Cmdlet(VerbsData.Out, "WPF", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
   public class OutWPFCommand : PSCmdlet
   {
      #region Parameters
      [Parameter(
         Position = 0,
         //           ParameterSetName = "Input",
         Mandatory = true,
         ValueFromPipeline = true,
         HelpMessage = "Data to bind to the wpf control")]
      public PSObject InputObject { get; set; }

      [Parameter(
         Position = 1,
         ParameterSetName = "FileTemplate",
         Mandatory = true,
         ValueFromPipeline = false,
         HelpMessage = "XAML template file")]
      public FileInfo FileTemplate { get; set; }

      [Parameter(
         Position = 1,
         ParameterSetName = "SourceTemplate",
         Mandatory = true,
         ValueFromPipeline = false,
         HelpMessage = "XAML template file")]
      [Alias("Template")]
      public XmlDocument SourceTemplate { get; set; }
      #endregion

      #region [rgn] Methods (1)

      // [rgn] Protected Methods (1)

      protected override void ProcessRecord()
      {
         try
         {
            switch (ParameterSetName)
            {
               case "FileTemplate":
                  {
                     string templates = Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "XamlTemplates");
                     string template = null;
                     if (FileTemplate == null)
                     {
                        // try to magically pick a file based on type and name....
                        foreach (string typeName in InputObject.TypeNames)
                        {
                           template = Path.Combine(templates, typeName + ".psxaml");
                           if (File.Exists(template))
                           {
                              FileTemplate = new FileInfo(template);
                              break;
                           }
                        }
                     }

                     if (!FileTemplate.Exists)
                     {
                        // try to magically resolve the file
                        template = System.IO.Path.Combine(base.CurrentProviderLocation("FileSystem").Path, FileTemplate.Name);
                        if (File.Exists(template))
                        {
                           FileTemplate = new FileInfo(template);
                        }
                        else
                        {
                           template = Path.Combine(templates, FileTemplate.Name);
                           if (File.Exists(template))
                           {
                              FileTemplate = new FileInfo(template);
                           }
                           else
                           {
                              throw new FileNotFoundException("Can't find the template file.  There is currently no default template location, so you must specify the path to the template file.", template);
                           }
                        }
                     }

                     ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.OutXaml(FileTemplate, InputObject);
                  }
                  break;
               case "SourceTemplate":
                  {
                     ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.OutXaml(SourceTemplate, InputObject);
                  }
                  break;
               case "DataTemplate":
                  {
                     ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.OutXaml(InputObject);
                     //WriteError(new ErrorRecord(
                     //    new ArgumentNullException("Template", "Automatic template choosing is not implemented yet."),
                     //    "Must Specify a Template",
                     //    ErrorCategory.NotImplemented,
                     //    InputObject));
                  }
                  break;
            }
         }
         catch (Exception ex)
         {
            WriteError(new ErrorRecord(ex, "NoIdeaWhy!", ErrorCategory.InvalidData, InputObject));
         }
      }

      #endregion [rgn]
   }
}
