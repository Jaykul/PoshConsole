using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using System.IO;
using System.Windows.Controls;
using System.Management.Automation;
using System.Management.Automation.Host;
using Huddled.WPF.Controls.Interfaces;
using Huddled.WPF.Controls.Utility;
using System.Windows.Media;

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
      List<Window> IPSWpfConsole.PopoutWindows
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
            return Document;
         }
      }

      Window IPSWpfConsole.RootWindow
      {
         get
         {
            return this.TryFindParent<Window>();
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