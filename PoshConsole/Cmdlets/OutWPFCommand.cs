using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.Collections;
using System.Xml;
using System.IO;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Huddled.PoshConsole
{
    [Cmdlet(VerbsData.Out, "WPF", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
    public class OutWPFCommand : PSCmdlet
    {

        #region Parameters
        [Parameter(
           Position = 1,
//           ParameterSetName = "Input",
           Mandatory = true,
           ValueFromPipeline = true,
           HelpMessage = "Data to bind to the wpf control")]
        public PSObject InputObject
        {
            get { return _inputObject; }
            set { _inputObject = value; }
        }

        [Parameter(
           Position = 0,
           ParameterSetName = "FileTemplate",
           Mandatory = true,
           ValueFromPipeline = false,
           HelpMessage = "XAML template file")]
        public FileInfo FileTemplate
        {
            get { return _templateFile; } 
            set { _templateFile = value; }
        }

        [Parameter(
           Position = 0,
           ParameterSetName = "SourceTemplate",
           Mandatory = true,
           ValueFromPipeline = false,
           HelpMessage = "XAML template file")]
        [Alias("Template")]
        public XmlDocument SourceTemplate
        {
            get { return _templateSource; }
            set { _templateSource = value; }
        }
	


        private PSObject _inputObject;
        private FileInfo _templateFile;
        private XmlDocument _templateSource;
        #endregion

        protected override void ProcessRecord()
        {
            try
            {
                ((PoshOptions)Host.PrivateData.BaseObject).XamlUI.OutXaml(_templateSource, _inputObject);
                //WriteError(new ErrorRecord(
                //    new ArgumentNullException("Template", "Automatic template choosing is not implemented yet."), 
                //    "Must Specify a Template", 
                //    ErrorCategory.NotImplemented, 
                //    _inputObject));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord( ex, "NoIdeaWhy!", ErrorCategory.InvalidData, _inputObject));
            }
        }
    }
}
