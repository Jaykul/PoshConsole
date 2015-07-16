using System;
using System.Collections.Generic;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using PoshCode.Wpf.Controls.Utility;

namespace PoshCode.Wpf.Controls
{
   partial class ConsoleControl : IPSWpfConsole
   {
      #region IPSXamlConsole Members

      /// <summary>
      /// News the paragraph.
      /// </summary>
      void IPSWpfConsole.NewParagraph()
      {
         Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
             {
                Current = Next;
             }));
      }

      /// <summary>
      /// A collection of popout windows
      /// </summary>
      private readonly List<Window> children = new List<Window>();

      /// <summary>
      /// Gets the popout windows.
      /// </summary>
      IList<Window> IPSWpfConsole.PopoutWindows
      {
         get 
         {
            return this.children;
         }
      }

      /// <summary>
      /// Gets a <see cref="T:System.Windows.Documents.FlowDocument"/> that hosts the content to be displayed by the <see cref="T:System.Windows.Controls.FlowDocumentScrollViewer"/>.
      /// </summary>
      /// <returns>A <see cref="T:System.Windows.Documents.FlowDocument"/> that hosts the content to be displayed by the <see cref="T:System.Windows.Controls.FlowDocumentScrollViewer"/>. The default is null.</returns>
      FlowDocument IPSWpfConsole.Document
      {
         get
         {
            return (FlowDocument)Dispatcher.Invoke(
               (Func<FlowDocument>)(() => this.Document),
               DispatcherPriority.Normal);
         }
      }

      /// <summary>
      /// Gets the root window.
      /// </summary>
      Window IPSWpfConsole.RootWindow
      {
         get
         {
            return (Window)Dispatcher.Invoke(
               (Func<Window>)this.TryFindParent<Window>,
               DispatcherPriority.Normal);
         }
      }

      bool IPSWpfConsole.IsInputFocused
      {
         get
         {
            return _commandContainer.Child.IsFocused;
         }
      }

      void IPSWpfConsole.ClearInput()
      {
         CurrentCommand = "";
      }
      
      void IPSWpfConsole.FocusInput()
      {
         _commandContainer.Child.Focus();
      }

      Paragraph IPSWpfConsole.CurrentBlock
      {
         get
         {
            return Current;
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