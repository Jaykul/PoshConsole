using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("PoshWpf")]
[assembly: AssemblyDescription("A WPF GUI Module for PowerShell to support threading")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HuddledMasses.org")]
[assembly: AssemblyProduct("PoshWpf")]
[assembly: AssemblyCopyright("Copyright ©  2008-2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Assigning an XML Namespace Deffinition exposes the namespace to WPF
[assembly: XmlnsDefinition("http://schemas.huddledmasses.org/wpf/powershell", "PoshWpf")]
[assembly: XmlnsDefinition("http://schemas.huddledmasses.org/wpf/powershell", "PoshWpf.Converters")]
[assembly: XmlnsDefinition("http://schemas.huddledmasses.org/wpf/powershell", "PoshWpf.Utility")]
[assembly: XmlnsDefinition("http://schemas.huddledmasses.org/wpf/powershell", "PoshWpf.Data")]
[assembly: XmlnsDefinition("http://schemas.huddledmasses.org/wpf/powershell", "PoshWpf.Commands")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("271dbea6-4872-4f6d-b947-aa0a1645714e")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]
