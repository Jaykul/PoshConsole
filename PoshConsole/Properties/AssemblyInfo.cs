#region Using directives

using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("PoshConsole")]
[assembly: AssemblyDescription("A sleek PowerShell console in WPF")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Huddled Masses")]
[assembly: AssemblyProduct("Posh Console")]
[assembly: AssemblyCopyright("© Copyright 2008-2011 Joel Bennett")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.
[assembly: NeutralResourcesLanguage("en-US")]

// This will expose a couple of pieces of our code as xaml/wpf controls
[assembly: XmlnsDefinition("http://schemas.poshconsole.org/controls", "PoshConsole.Controls")]
[assembly: XmlnsDefinition("http://schemas.poshconsole.org/properties", "PoshConsole.Properties")]

// Specifies the location in which theme dictionaries are stored for types in an assembly.
[assembly: ThemeInfo(
    // Specifies the location of system theme-specific resource dictionaries for this project.
    // The default setting in this project is "None" since this default project does not
    // include these user-defined theme files:
    //     Themes\Aero.NormalColor.xaml
    //     Themes\Classic.xaml
    //     Themes\Luna.Homestead.xaml
    //     Themes\Luna.Metallic.xaml
    //     Themes\Luna.NormalColor.xaml
    //     Themes\Royale.NormalColor.xaml
    ResourceDictionaryLocation.None,

    // Specifies the location of the system non-theme specific resource dictionary:
    //     Themes\generic.xaml
    ResourceDictionaryLocation.SourceAssembly)]


// Version information
[assembly: AssemblyVersion("2.0.2011.309")]
[assembly: AssemblyFileVersion("2.0.2011.309")]
[assembly: AssemblyInformationalVersion("2.0.2011.309")]
[assembly: GuidAttribute("0d77437b-6892-4c83-a41c-d135133e758a")]
