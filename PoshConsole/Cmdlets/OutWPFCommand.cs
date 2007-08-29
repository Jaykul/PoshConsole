using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.Collections;
using System.Xml;
using System.IO;
using System.Windows.Controls;
using System.Windows.Markup;

namespace PoshConsole.Cmdlets
{
    [Cmdlet(VerbsData.Out, "WPF", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
    public class OutWPFCommand : PSCmdlet
    {
        
		#region [rgn] Methods (1)

		// [rgn] Protected Methods (1)

		protected override void ProcessRecord()
        {
            try
            {
                if (ParameterSetName == "FileTemplate")
                {
                    if (!_templateFile.Exists)
                    {
                        string newPath = System.IO.Path.Combine(base.CurrentProviderLocation("FileSystem").Path,_templateFile.Name);
                        if (!File.Exists(newPath))
                        {
                            throw new FileNotFoundException("Can't find the template file.  There is currently no default template location, so you must specify the path to the template file.", _templateFile.FullName);
                        }
                        else
                        {
                            _templateFile = new FileInfo(newPath);
                        }
                    }

                    ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.OutXaml(_templateFile, _inputObject);
                }
                else if (ParameterSetName == "SourceTemplate")
                {
                    ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI.OutXaml(_templateSource, _inputObject);
                }
                else 
                    WriteError(new ErrorRecord(
                        new ArgumentNullException("Template", "Automatic template choosing is not implemented yet."), 
                        "Must Specify a Template", 
                        ErrorCategory.NotImplemented, 
                        _inputObject));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord( ex, "NoIdeaWhy!", ErrorCategory.InvalidData, _inputObject));
            }
        }
		
		#endregion [rgn]
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
    }
}
