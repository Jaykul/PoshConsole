using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace PoshCode.Controls
{
    public class PromptForObjectEventArgs : EventArgs
    {
        public string Caption { get; set; }
        public string Message { get; set; }
        public Collection<FieldDescription> Descriptions { get; set; }

        public Dictionary<string, PSObject> Results { get; set; }

        public PromptForObjectEventArgs(string caption, string message, Collection<FieldDescription> descriptions)
        {
            Caption = caption;
            Message = message;
            Descriptions = descriptions;
            Results = new Dictionary<string, PSObject>();
        }
    }
}