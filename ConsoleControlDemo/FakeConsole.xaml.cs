using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Huddled.WPF.Controls;
using Huddled.WPF.Controls.Interfaces;

namespace ConsoleControlDemo
{
   /// <summary>
   /// Interaction logic for FakeConsole.xaml
   /// </summary>
   public partial class FakeConsole : Window
   {
      public FakeConsole()
      {
         InitializeComponent();

         WpfConsole.Command += new Huddled.WPF.Controls.CommmandDelegate(WpfConsole_Command);

      }

      void WpfConsole_Command(object source, CommandEventArgs command)
      {
         var worker = new BackgroundWorker();
         worker.DoWork += new DoWorkEventHandler(worker_DoWork);
         worker.RunWorkerAsync(command);
      }

      private int i = 0;
      void worker_DoWork(object sender, DoWorkEventArgs e)
      {
         Random r = new Random();
         int max = r.Next(3, 6);

         var cea = e.Argument as CommandEventArgs;
         for (int j = 0; j < max; j++)
         {
            ((IPSConsole)WpfConsole).WriteLine("This is a test (" + cea.Command + ") of the emergency broadcast system",cea.OutputBlock);
            System.Threading.Thread.Sleep(r.Next(200, 800));
         }

         WpfConsole.Prompt("[" + (++i) + "]: ");
      }
   }
}
