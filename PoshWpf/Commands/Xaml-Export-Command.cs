using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System.Xaml;
using System.Xml;

namespace PoshWpf.Commands
{
#if CLR4
	[Cmdlet(VerbsData.Export, "Xaml", DefaultParameterSetName = ParamSetPath)]
	public class XamlExportCommand : HuddledContentProviderBaseCommand
   {
      [Parameter(Mandatory = true, Position = 10, ValueFromPipeline = true)]
      public PSObject[] InputObject { get; set; }

		private List<object> inputs = new List<object>();

      protected override void ProcessRecord()
      {
         inputs.AddRange(from obj in InputObject select obj.BaseObject);
      	//inputs.AddRange(InputObject);
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
						try
						{
							object arr = inputs.ToArray();
							var xaml = XamlServices.Save(arr);
							writer.Write(new List<string>(new[]{xaml}));
						} 
						catch (Exception ex)
						{
							WriteError(new ErrorRecord(ex, "CantWriteContent", ErrorCategory.ReadError, path));
						}
      			}
					writer.Close();
      		}
      	}
         base.EndProcessing();
      }
   }
#endif
}


