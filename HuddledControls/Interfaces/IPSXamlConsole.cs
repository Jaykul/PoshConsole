using System.Management.Automation;

namespace Huddled.WPF.Controls.Interfaces
{
   /// <summary>
   /// <para>Provides an interface which extends the existing PowerShell interfaces with a Xaml
   /// based user interface which allows loading of arbitrary bits of Xaml source.  This
   /// is peculiar to the <see cref="PoshConsole"/> implementation.</para>
   /// <para>The implemenation of these methods must be done on the UI Delegate thread, because
   /// typically Xaml can only be loaded on the UI thread, since no other thread is allowed to 
   /// create instances of the visual controls (the likely contents of the <paramref name="template"/>).
   /// </para>
   /// </summary>
   public interface IPSXamlConsole
   {
      void OutXaml(System.Xml.XmlDocument template);
      void OutXaml(System.IO.FileInfo template);
      void OutXaml(System.Xml.XmlDocument template, PSObject data);
      void OutXaml(System.IO.FileInfo template, PSObject data);
      void OutXaml(PSObject data);
      void NewParagraph();
   }
}