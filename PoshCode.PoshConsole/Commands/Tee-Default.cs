using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Xml.Schema;
using Microsoft.PowerShell.Commands;

namespace PoshCode.Commands
{
    [Cmdlet("Tee", "Default")]

    public class TeeDefault : OutDefaultCommand
    {
        private static bool _handled = false;
        private bool _isFirst = false;
        private ulong _count;
        private bool _broken = false;
        private static ulong danger = ulong.MaxValue - 1;

        [Parameter()]
        [ValidateRange(0, ulong.MaxValue)]
        public ulong Limit { get; set; }

        protected override void BeginProcessing()
        {
            if (Limit == ulong.MaxValue)
            {
                Limit = 0;
            }

            _count = 0;
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            if (!_handled)
                _isFirst = _handled = true;

            // All we do is passthru the input, and call the base Out-Default
            WriteObject(InputObject, false);
            if(_isFirst && (Limit == 0 || Limit > _count++))
            {
                base.ProcessRecord();
            }

            if (_count == danger)
            {
                _count = Limit + 1;
                _broken = true;
            }
        }

        protected override void EndProcessing()
        {
            if (_isFirst && Limit < _count)
            {
                
                var summary = _broken
                    ? new PSObject($"And many more... ")
                    : new PSObject($"And {_count - Limit} more... ");
                summary.Properties.Add(new PSNoteProperty("Limit", Limit));
                summary.Properties.Add(new PSNoteProperty("Total", _count));
                summary.Properties.Add(new PSNoteProperty("Skipped", _count - Limit));
                InputObject = summary;
                base.ProcessRecord();
            }
            base.EndProcessing();

            _isFirst = _handled = false;
        }
    }
}