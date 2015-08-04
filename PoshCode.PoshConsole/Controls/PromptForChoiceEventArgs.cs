using System;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;

namespace PoshCode.Controls
{
    public class PromptForChoiceEventArgs : EventArgs
    {
        public string Caption { get; set; }
        public string Message { get; set; }
        public Collection<ChoiceDescription> Choices { get; set; }
        public int SelectedIndex { get; set; }

        public bool Handled { get; set; }

        public PromptForChoiceEventArgs(string caption, 
            string message, 
            Collection<ChoiceDescription> choices,
            int selectedIndex)
        {
            Caption = caption;
            Message = message;
            Choices = choices;
            SelectedIndex = selectedIndex;
        }
    }
}