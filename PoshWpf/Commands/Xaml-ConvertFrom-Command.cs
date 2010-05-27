#if CLR_V2
#if CLR_V4
#error You can't define CLR_V2 and CLR_V4 at the same time
#endif
// code for clr 2
#elif CLR_V4
// code for clr 4

using System.Management.Automation;
using System.Xml;

namespace PoshWpf.Commands
{

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

}


#else
#error Define either CLR_V2 or CLR_V4 to compile
#endif