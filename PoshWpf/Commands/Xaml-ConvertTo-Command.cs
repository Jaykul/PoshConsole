using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System.Xml;

namespace PoshWpf.Commands
{
#if CLR4
   public enum XamlOutput { String, XmlDocument, XDocument }

   [Cmdlet(VerbsData.ConvertTo, "Xaml")]
   public class ConvertToXamlCommand : Cmdlet
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
            //var builder = new StringBuilder();
            //var xmlWriter = XmlWriter.Create(builder);
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
#endif
}


