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
using Huddled.WPF.Controls.Interfaces;
using Huddled.WPF.Controls.Utility;
using System.Windows.Media;

namespace Huddled.WPF.Controls
{
   partial class ConsoleControl : IPSXamlConsole
   {
      #region IPSXamlConsole Members
      void IPSXamlConsole.NewParagraph()
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
             {
                _current = _next;
             }));
      }

      private List<Window> _children = new List<Window>();
      List<Window> IPSXamlConsole.PopoutWindows
      {
         get 
         {
            return _children;
         }
      }

      FlowDocument IPSXamlConsole.Document
      {
         get
         {
            return Document;
         }
      }

      Window IPSXamlConsole.RootWindow
      {
         get
         {
            return this.TryFindParent<Window>();
         }
      }

      Paragraph IPSXamlConsole.CurrentBlock
      {
         get
         {
            return _current;
         }
      }

      Dispatcher IPSXamlConsole.Dispatcher {
         get
         {
            return Dispatcher;
         }
      }
      #endregion
   }
}