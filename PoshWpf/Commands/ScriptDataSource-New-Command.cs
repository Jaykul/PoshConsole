using System;
using System.Collections.Generic;
using System.Management.Automation;
using PoshWpf.Data;

namespace PoshWpf.Commands {
    [Cmdlet(VerbsCommon.New, "ScriptDataSource")]
    public class NewScriptDataSourceCommand : Cmdlet {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "A scriptblock to execute")]
        public ScriptBlock Script { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Interval", HelpMessage = "Delay between re-running the script")]
        [Alias("TimeSpan")]
        public TimeSpan Each { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), Parameter(Mandatory = true, Position = 10, HelpMessage = "Input parameters to the ScriptBlock", ValueFromRemainingArguments = true, ValueFromPipeline = true)]
        [Alias("IO")]
        public PSObject[] InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter RunFirst { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter AccumulateOutput { get; set; }

        private List<PSObject> _input;
        protected override void BeginProcessing() {
            _input = new List<PSObject>();
            base.BeginProcessing();
        }
        protected override void ProcessRecord() {
            _input.AddRange(InputObject);
            base.ProcessRecord();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override void EndProcessing() {
            WriteObject(new PSDataSource(Script, _input, Each, RunFirst.ToBool()));

            base.EndProcessing();
        }
    }
}
