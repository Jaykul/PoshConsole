// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPSWpfConsole.cs" company="Huddled Masses">
//   Copyright (c) 2010-2012 Joel Bennett
// </copyright>
// <summary>
//   <para>Provides an interface which extends the existing PowerShell PrivateData class with a
//   <see cref="IPSWpfConsole" />, with access to the WPF Window and Dispatcher</para>
// </summary>
// --------------------------------------------------------------------------------------------------------------------
// ReSharper disable CheckNamespace
namespace System.Management.Automation.Host
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Threading;

    /// <summary>
    /// <para>Provides an interface which extends the existing PowerShell interfaces with a Xaml
    /// based user interface which allows loading of arbitrary bits of Xaml source.  This
    /// is peculiar to the <see cref="PoshConsole"/> implementation.</para>
    /// <para>The implemenation of these methods must be done on the UI Delegate thread, because
    /// typically Xaml can only be loaded on the UI thread, since no other thread is allowed to 
    /// create instances of the visual controls (the likely contents of the <paramref name="template"/>).
    /// </para>
    /// </summary>
    public interface IPSWpfConsole
    {
        /// <summary>
        /// Gets the root window.
        /// </summary>
        Window RootWindow { get; }

        /// <summary>
        /// Gets the dispatcher.
        /// </summary>
        Dispatcher Dispatcher { get; }

        /// <summary>
        /// Gets the list of current popout windows.
        /// </summary>
        IList<Window> PopoutWindows { get; }

        /// <summary>
        /// Gets the document.
        /// </summary>
        FlowDocument Document { get; }

        /// <summary>
        /// Gets the current paragraph.
        /// </summary>
        Paragraph CurrentBlock { get; }

        bool IsInputFocused { get; }

        /// <summary>
        /// Creates a new paragraph.
        /// </summary>
        void NewParagraph();

        /// <summary>
        /// Focuses the input line.
        /// </summary>
        void FocusInput();      /// <summary>

        /// Clears the input line.
        /// </summary>
        void ClearInput();

        //Runspace Runspace { get; }

        //void OutXaml(bool popup, System.Xml.XmlDocument template );
        //void OutXaml(bool popup, System.IO.FileInfo template );
        //void OutXaml(bool popup, System.Xml.XmlDocument template, params PSObject[] data);
        //void OutXaml(bool popup, System.IO.FileInfo template, params PSObject[] data);
        //void OutXaml(bool popup, params PSObject[] data);
        // Block GetOutputBlock(int id);
    }


    /// <summary>
    /// <para>Provides an interface which extends the existing PowerShell PrivateData class with a
    /// <see cref="IPSWpfConsole" />, with access to the WPF Window and Dispatcher</para>
    /// </summary>
    public interface IPSWpfOptions
    {
        /// <summary>
        /// Gets WpfConsole.
        /// </summary>
        IPSWpfConsole WpfConsole { get; }
    }
}
// ReSharper restore CheckNamespace
