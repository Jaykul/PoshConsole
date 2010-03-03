using System.Management.Automation;
using System.Xml;

namespace PoshWpf.Commands
{
#if CLR4
   [Cmdlet(VerbsData.ConvertFrom, "Xaml", DefaultParameterSetName = "Xaml")]
   public class XamlConvertFromCommand : Cmdlet
   {
      [Parameter(ParameterSetName = "Document", Mandatory = true, ValueFromPipeline = true)]
      public XmlDocument Document { get; set; }

      [Parameter(ParameterSetName = "Xaml", Mandatory = true, Position = 0, ValueFromPipeline = true)]
      [Alias("Source")]
      public string Xaml { get; set; }

      protected override void ProcessRecord()
      {
         if (Document != null && string.IsNullOrEmpty(Xaml))
         {
            Xaml = Document.OuterXml;
         }
         // Any errors here will just propagate out and crash.
         // I'm ok with that.
         WriteObject(System.Xaml.XamlServices.Parse(Xaml));

         base.ProcessRecord();
      }
   }
#endif
}


