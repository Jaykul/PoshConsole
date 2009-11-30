using System;
using System.Collections.Generic;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Huddled.WPF.Controls.Utility;

namespace Huddled.WPF.Controls
{
   partial class ConsoleControl : IPSWpfConsole
   {
      #region IPSXamlConsole Members
      void IPSWpfConsole.NewParagraph()
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
             {
                _current = _next;
             }));
      }

      private List<Window> _children = new List<Window>();
      IList<Window> IPSWpfConsole.PopoutWindows
      {
         get 
         {
            return _children;
         }
      }

      FlowDocument IPSWpfConsole.Document
      {
         get
         {
            return (FlowDocument)Dispatcher.Invoke((Func<FlowDocument>)(() =>
            { return Document; }), DispatcherPriority.Normal);
         }
      }

      Window IPSWpfConsole.RootWindow
      {
         get
         {
             return (Window)Dispatcher.Invoke((Func <Window>)(() =>
                 { return this.TryFindParent<Window>(); }),DispatcherPriority.Normal);
         }
      }

      Paragraph IPSWpfConsole.CurrentBlock
      {
         get
         {
            return _current;
         }
      }

      Dispatcher IPSWpfConsole.Dispatcher {
         get
         {
            return Dispatcher;
         }
      }
      #endregion
   }
}