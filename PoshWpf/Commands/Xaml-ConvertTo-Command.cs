#if CLR_V2
#if CLR_V4
#error You can't define CLR_V2 and CLR_V4 at the same time
#endif
// code for clr 2
#elif CLR_V4
// code for clr 4

using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Xml;

namespace PoshWpf.Commands
{

   public enum XamlOutput { String, XmlDocument, XDocument }

   [Cmdlet(VerbsData.ConvertTo, "Xaml")]
	public class XamlConvertToCommand : Cmdlet
   {
      [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
      public PSObject[] InputObject { get; set; }

      [Parameter(Mandatory = false)]
      public SwitchParameter AsDocument { get; set; }

      private List<object> inputs = new List<object>();

      protected override void ProcessRecord()
      {
			inputs.AddRange(from obj in InputObject select obj.BaseObject);
         base.ProcessRecord();
      }

      protected override void EndProcessing()
      {
         if (AsDocument.ToBool())
         {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(System.Xaml.XamlServices.Save(inputs.ToArray()));
            WriteObject(xmlDocument);
         }
         else
         {
            WriteObject(System.Xaml.XamlServices.Save(inputs.ToArray()));
         }
         base.EndProcessing();
      }
   }

}

#else
#error Define either CLR_V2 or CLR_V4 to compile
#endif
