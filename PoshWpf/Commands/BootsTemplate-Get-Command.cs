using System.IO;
using System.Management.Automation;
using PoshWpf.Utility;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsCommon.Get, "BootsTemplate", SupportsShouldProcess = true, DefaultParameterSetName="Path")]
   public class GetBootsTemplateCommand : PSCmdlet
   {

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(Position = 0, Mandatory = false)]
      public string[] Filter
      {
         get { return _filter; } 
         set{ _filter = value; }
      }

      private string[] _filter = new[]{"*"};

      protected override void ProcessRecord()
      {
         string[] templateFiles = XamlHelper.GetDataTemplates();
         foreach (string path in Filter)
         {
            var pat = new WildcardPattern(path);
            foreach (var file in templateFiles)
            {
               if (pat.IsMatch(file) || pat.IsMatch( Path.GetDirectoryName(file) ))
               {
                  WriteObject(new FileInfo(file));
               }
            }
         }
      }
   }
}
