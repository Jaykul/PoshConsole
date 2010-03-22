#if CLR_V2
#if CLR_V4
#error You can't define CLR_V2 and CLR_V4 at the same time
#endif
// code for clr 2
#elif CLR_V4
// code for clr 4

using System;
using System.Management.Automation;
using System.Text;
using System.Xml;
using System.Linq;

namespace PoshWpf.Commands
{

	[Cmdlet(VerbsData.Import, "Xaml", DefaultParameterSetName = ParamSetPath)]
   public class XamlImportCommand : HuddledContentProviderBaseCommand
   {
      protected override void ProcessRecord()
      {
			// pre-call base to get ProviderPaths populated
			base.ProcessRecord();

      	foreach (var path in ProviderPaths)
      	{
      		using (var reader = TryGetReader(path))
      		{
      			var builder = new StringBuilder();
					// read everything into a list of ... stuff
					try
					{
						var lines = reader.Read(0);
						foreach (var line in lines)
						{
							builder.Append(line.ToString());
						}
					} 
					catch(Exception ex)
					{
						WriteError(new ErrorRecord(ex, "CantReadContent", ErrorCategory.ReadError, path));
					}

					// Any errors here will just propagate out and crash.
					// I'm ok with that.
					WriteObject(System.Xaml.XamlServices.Parse(builder.ToString()));
      		}
      	}
      }
   }

}


#else
#error Define either CLR_V2 or CLR_V4 to compile
#endif