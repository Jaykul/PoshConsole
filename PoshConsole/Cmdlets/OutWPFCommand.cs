using System;
using System.IO;
using System.Management.Automation;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using Huddled.WPF.Controls.Interfaces;
using System.Windows.Media;

namespace PoshConsole.Cmdlets
{
   [Cmdlet(VerbsData.Out, "WPF", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "DataTemplate")]
   public class OutWPFCommand : PSCmdlet
   {
      #region Parameters
      [Parameter(
         Position = 0,
         //           ParameterSetName = "Input",
         Mandatory = true,
         ValueFromPipeline = true,
         HelpMessage = "Data to bind to the wpf control")]
      public PSObject InputObject { get; set; }

      [Parameter(
         Position = 1,
         ParameterSetName = "FileTemplate",
         Mandatory = true,
         ValueFromPipeline = false,
         HelpMessage = "XAML template file")]
      public FileInfo FileTemplate { get; set; }

      [Parameter(
         Position = 1,
         ParameterSetName = "SourceTemplate",
         Mandatory = true,
         ValueFromPipeline = false,
         HelpMessage = "XAML template file")]
      [Alias("Template")]
      public XmlDocument SourceTemplate { get; set; }

      [Parameter(
         Position = 2,
         Mandatory = false,
         ValueFromPipeline = false,
         HelpMessage = "Show in popup window")]
      public SwitchParameter Popup { get; set; }
      #endregion

      #region Methods

      // private Huddled.WPF.Controls.Interfaces.IPSXamlConsole xamlUI;
      // private FlowDocument _document = null;
      private FrameworkElement _element = null;
      private IPSXamlConsole _xamlUI = null;
      private ItemsControl _host = null;
      private Window _window = null;


      protected override void BeginProcessing()
      {


         _xamlUI = ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI;
         ErrorRecord error = (ErrorRecord)_xamlUI.Dispatcher.Invoke((Func<ErrorRecord>)(() =>
         {
            ErrorRecord err = null;
            if (Popup.ToBool())
            {
               _host = NewContainer();
               _window = new Window
               {
                  WindowStyle = WindowStyle.ToolWindow,
                  Content = _host
               };
               _xamlUI.PopoutWindows.Add(_window);
               _window.Show();
            }

            #region templates
            switch (ParameterSetName)
            {
               case "FileTemplate":
                  {
                     string templates = Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "XamlTemplates");
                     string template = null;

                     if (!FileTemplate.Exists)
                     {
                        // try to magically resolve the file
                        template = System.IO.Path.Combine(base.CurrentProviderLocation("FileSystem").Path, FileTemplate.Name);
                        if (File.Exists(template))
                        {
                           FileTemplate = new FileInfo(template);
                        }
                        else
                        {
                           template = Path.Combine(templates, FileTemplate.Name);
                           if (File.Exists(template))
                           {
                              FileTemplate = new FileInfo(template);
                           }
                           else
                           {
                              throw new FileNotFoundException("Can't find the template file.  There is currently no default template location, so you must specify the path to the template file.", template);
                           }
                        }
                     }

                     FileTemplate.TryLoadXaml(out _element, out err);
                  }
                  break;
               case "SourceTemplate":
                  {
                     SourceTemplate.TryLoadXaml(out _element, out err);
                  }
                  break;
            }
            #endregion templates

            return err;
         }));
         if (error != null) { WriteError(error); }
         base.BeginProcessing();
      }

      private ItemsControl NewContainer()
      {
         FrameworkElementFactory factoryPanel = new FrameworkElementFactory(typeof(WrapPanel));
         factoryPanel.SetValue(WrapPanel.IsItemsHostProperty, true);
         factoryPanel.SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal);
         ItemsPanelTemplate template = new ItemsPanelTemplate() { VisualTree = factoryPanel };

         return new ItemsControl()
         {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsPanel = template
         };
      }

      protected override void ProcessRecord()
      {
         object output = InputObject.BaseObject;
         _xamlUI.Dispatcher.BeginInvoke((Action)(() =>
         {
            if (output is Block || output is Inline)
            {
               Paragraph par;
               FlowDocument doc;
               if (_window != null)
               {
                  par = new Paragraph()
                  {
                     Background = _xamlUI.CurrentBlock.Background,
                     Foreground = _xamlUI.CurrentBlock.Foreground
                  };
                  doc = new FlowDocument(par)
                  {
                     Background = _xamlUI.Document.Background,
                     Foreground = _xamlUI.Document.Foreground
                  };
                  _window.Content = new FlowDocumentScrollViewer
                  {
                     Document = doc,
                     Width = _window.Width,
                     Height = _window.Height,
                     Margin = new Thickness(0),
                     Padding = new Thickness(0),
                     IsToolBarVisible = true
                  };
               }
               else
               {
                  par = _xamlUI.CurrentBlock;
                  doc = _xamlUI.Document;
               }
               if (InputObject.BaseObject is Block)
               {
                  doc.Blocks.InsertAfter(par, (Block)output);
               }
               else
               {
                  par.Inlines.Add((Inline)output);
               }
            }
            else
            {
               if (_window == null)
               {
                  _host = NewContainer();
                  _xamlUI.CurrentBlock.Inlines.Add(new InlineUIContainer(_host));
               }

               if (_element != null)
               {
                  var e = _element;
                  e.DataContext = output;
                  _host.Items.Add(e);
               }
               else
               {
                  _host.Items.Add(output);
               }
            }
         }));
      }

      #endregion
   }
}
