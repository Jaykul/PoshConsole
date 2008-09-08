using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;

namespace PoshConsole.Cmdlets
{
   [Cmdlet(VerbsCommon.Get, "PoshOutput", SupportsShouldProcess = false, ConfirmImpact = ConfirmImpact.None)]
   public class GetPoshOutputCommand : PSCmdlet
   {
      [Parameter(
         Position = 0,
         Mandatory = false,
         HelpMessage = "The id of the Paragraph to return")]
      public int id { get; set; }

      private int _id = 0;
      private Window _window;
      private Huddled.WPF.Controls.Interfaces.IPSXamlConsole _xamlUI;
      private Block _numbered;

      protected override void  BeginProcessing()
      {
         _xamlUI = ((PoshConsole.PSHost.PoshOptions)Host.PrivateData.BaseObject).XamlUI;
         _xamlUI.Dispatcher.BeginInvoke((Action)(()=> {
            _window = _xamlUI.RootWindow;
            Block b = _xamlUI.CurrentBlock;
            while(b.Tag == null && b.PreviousBlock != null) {
               b = b.PreviousBlock;
            }
            if (b.Tag is int)
            {
               _id = (int)b.Tag;
               _numbered = b;
            }
         }));

 	      base.BeginProcessing();
      }

      protected override void ProcessRecord()
      {
          WriteObject(
            (Block)_xamlUI.Dispatcher.Invoke((Func<Block>)(()=> {
               Block source = null;
               if (id == 0)
               {
                  _xamlUI.NewParagraph();
                  source = _xamlUI.CurrentBlock;
               }
               else
               {

                  bool reverse = id < 0;
                  id = Math.Abs(id);
                  if (id > _id)
                  {
                     id = 1;
                  }
                  else if (reverse)
                  {
                     id = _id - (_id - id - 2); 
                  }
                  else
                  {
                     id = _id - id;
                  }


                  source = _numbered;
                  while (id-- > 0 && source.PreviousBlock != null)
                  {
                     source = source.PreviousBlock;
                  }
               }
               // clone it by serializing and deserializing
               MemoryStream stream = new MemoryStream();
               (new TextRange(source.ContentStart, source.ContentEnd)).Save(stream, DataFormats.XamlPackage, true);

               var result = new Paragraph();
               var copy = new TextRange(result.ContentStart, result.ContentEnd);

               copy.Load(stream, DataFormats.XamlPackage);
               return result;
         })));
      }
   }
}
