using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PoshWpf.Commands
{
   [Cmdlet(VerbsCommon.New, "ScriptDataSource")]
   public class NewScriptDataSourceCommand : Cmdlet
   {
      [Parameter(Mandatory = true, Position = 0, HelpMessage = "A scriptblock to execute")]
      public ScriptBlock Script { get; set; }

      [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Interval", HelpMessage = "Delay between re-running the script")]
      [Alias("TimeSpan")]
      public TimeSpan Each { get; set; }

      [Parameter(Mandatory = true, Position = 10, HelpMessage = "Input parameters to the ScriptBlock", ValueFromRemainingArguments = true, ValueFromPipeline = true)]
      [Alias("IO")]
      public PSObject[] InputObject { get; set; }


      [Parameter(Mandatory = false)]
      public SwitchParameter RunFirst { get; set; }

      public List<PSObject> _input;
      protected override void BeginProcessing()
      {
         _input = new List<PSObject>();
         base.BeginProcessing();
      }
      protected override void ProcessRecord()
      {
         _input.AddRange(InputObject);
         base.ProcessRecord();
      }

      protected override void EndProcessing()
      {
         WriteObject( new PsDataSource( Script, new PSDataCollection<PSObject>(_input), Each, RunFirst.ToBool()) );

         base.EndProcessing();
      }
   }
}
