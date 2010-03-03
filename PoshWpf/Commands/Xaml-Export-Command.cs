using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System.Xaml;
using System.Xml;

namespace PoshWpf.Commands
{
#if CLR4
   [Cmdlet(VerbsData.Export, "Xaml")]
	public class XamlExportCommand : HuddledContentProviderBaseCommand
   {
      [Parameter(Mandatory = true, Position = 10, ValueFromPipeline = true)]
      public PSObject[] InputObject { get; set; }

      private List<object> inputs = new List<object>();

      protected override void ProcessRecord()
      {
         inputs.AddRange(from obj in InputObject select obj.BaseObject);
         base.ProcessRecord();
      }

      protected override void EndProcessing()
      {
      	foreach (var path in ProviderPaths)
      	{
      		using(var writer = TryGetWriter(path))
      		{
      			if (writer != null)
      			{
      				writer.Write( new List<string>(new[]{XamlServices.Save(inputs.ToArray())} ) );
      			}
      		}
      	}
         base.EndProcessing();
      }
   }
#endif
}


