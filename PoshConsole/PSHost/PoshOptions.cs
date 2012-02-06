using System.Collections.Generic;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Huddled.Wpf.Controls;
using IPoshConsoleControl = System.Management.Automation.Host.IPoshConsoleControl;

namespace PoshConsole.Host
{
    internal class PoshOptions : DependencyObject, IPSWpfOptions
   {

      #region [rgn] Fields (4)

      IPoshConsoleControl _console;
      PoshHost _host;
      PoshWpfConsole _xamlUI;

      #endregion [rgn]

      #region [rgn] Constructors (1)

      public PoshOptions(PoshHost host, IPoshConsoleControl console)
      {
         _host = host;
         _console = console;
         _xamlUI = new PoshWpfConsole(console);
      }

      #endregion [rgn]

      #region [rgn] Properties (7)

      public Properties.Colors Colors
      {
         get { return Properties.Colors.Default; }
      }

      //public double FullPrimaryScreenHeight
      //{
      //   get { return System.Windows.SystemParameters.FullPrimaryScreenHeight; }
      //}

      //public double FullPrimaryScreenWidth
      //{
      //   get { return System.Windows.SystemParameters.FullPrimaryScreenWidth; }
      //}

      public CommandHistory History
      {
         get
         {
            return _console.History;
         }
      }

      public Properties.Settings Settings
      {
         get { return Properties.Settings.Default; }
      }


      public IPSWpfConsole WpfConsole
      {
         get
         {
            return _xamlUI;
         }
      }
      public IPSBackgroundHost BgHost
      {
         get
         {
            return _host;
         }
      }
      
      #endregion [rgn]

      #region [rgn] Delegates and Events (2)

      // [rgn] Delegates (2)

      private delegate string GetStringDelegate();
      private delegate void SetStringDelegate( /* string value */ );

      #endregion [rgn]

      #region [rgn] Nested Classes (1)

      public class PoshWpfConsole : IPSWpfConsole
      {
         private IPSWpfConsole _console;

         /// <summary>
         /// Initializes a new instance of the <see cref="XamlConsole"/> class.
         /// </summary>
         /// <param name="console">The console.</param>
         public PoshWpfConsole(IPSWpfConsole console)
         {
            _console = console;
         }


         #region IPSWpfConsole Members
         public void NewParagraph(){ _console.NewParagraph(); }
         public FlowDocument Document { get { return _console.Document;  } }
         public Window RootWindow { get { return _console.RootWindow; } }
         public IList<Window> PopoutWindows { get { return _console.PopoutWindows; } }
         public Paragraph CurrentBlock { get { return _console.CurrentBlock; } }
         public Dispatcher Dispatcher { get { return _console.Dispatcher; } }
         //public Runspace Runspace { get { return _console.Runspace; } }

         #endregion
      }

      #endregion [rgn]

   }
}
