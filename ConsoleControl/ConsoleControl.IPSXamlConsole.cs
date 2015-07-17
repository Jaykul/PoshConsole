using System;
using System.Collections.Generic;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using PoshCode.Wpf.Controls.Utility;

namespace PoshCode.Wpf.Controls
{
   partial class ConsoleControl
   {
      #region IPSXamlConsole Members

      /// <summary>
      /// News the paragraph.
      /// </summary>
      public void NewParagraph()
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
      public IList<Window> PopoutWindows
      {
         get 
         {
            return this.children;
         }
      }

      ///// <summary>
      ///// Gets a <see cref="T:System.Windows.Documents.FlowDocument"/> that hosts the content to be displayed by the <see cref="T:System.Windows.Controls.FlowDocumentScrollViewer"/>.
      ///// </summary>
      ///// <returns>A <see cref="T:System.Windows.Documents.FlowDocument"/> that hosts the content to be displayed by the <see cref="T:System.Windows.Controls.FlowDocumentScrollViewer"/>. The default is null.</returns>
      //public FlowDocument Document
      //{
      //   get
      //   {
      //      return (FlowDocument)Dispatcher.Invoke(
      //         (Func<FlowDocument>)(() => this.Document),
      //         DispatcherPriority.Normal);
      //   }
      //}

      /// <summary>
      /// Gets the root window.
      /// </summary>
      public Window RootWindow
      {
         get
         {
            return (Window)Dispatcher.Invoke(
               (Func<Window>)this.TryFindParent<Window>,
               DispatcherPriority.Normal);
         }
      }

      public bool IsInputFocused
      {
         get
         {
            return _commandContainer.Child.IsFocused;
         }
      }

      public void ClearInput()
      {
         CurrentCommand = "";
      }

      public void FocusInput()
      {
         _commandContainer.Child.Focus();
      }

      public Paragraph CurrentBlock
      {
         get
         {
            return Current;
         }
      }
      #endregion
   }
}