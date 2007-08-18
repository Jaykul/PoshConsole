using System;
using System.Management.Automation.Host;
using System.Management.Automation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security;

namespace Huddled.PoshConsole
{
    [Serializable]
    public enum ConsoleScrollBarVisibility
    {
        Disabled = 0,
        Auto = 1,
        Hidden = 2,
        Visible = 3,
    }

    // public delegate void WriteProgressDelegate(long sourceId, ProgressRecord record);

    public interface IPSUI
    {
        void WriteProgress(long sourceId, ProgressRecord record);
        PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options);
        PSCredential PromptForCredential(string caption, string message, string userName, string targetName);
        SecureString ReadLineAsSecureString();

        void SetShouldExit(int exitCode);
        IPSConsoleControl Console { get; }
    }

    /// <summary>
    /// An interface for <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> to allow 
    /// implementation of both <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> 
    /// and <see cref="System.Management.Automation.Host.PSHostUserInterface"/> in the same class 
    /// ... since there's no multiple inheritance in C#.
    /// </summary>
    public interface IPSRawConsole
    {
        int CursorSize { get; set; }
        Size BufferSize { get; set; }
        Size MaxPhysicalWindowSize { get; }
        Size MaxWindowSize { get; }
        Size WindowSize { get; set; }

        Coordinates CursorPosition { get; set; }
        Coordinates WindowPosition { get; set; }

        ConsoleColor BackgroundColor { get; set; }
        ConsoleColor ForegroundColor { get; set; }

        void FlushInputBuffer();
        // TODO: There are additional methods.
        // KeyInfo ReadKey(ReadKeyOptions options);
        // bool KeyAvailable { get; }
        BufferCell[,] GetBufferContents(Rectangle rectangle);
        void SetBufferContents(Rectangle rectangle, BufferCell fill);
        void SetBufferContents(Coordinates origin, BufferCell[,] contents);

        void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill);
        string WindowTitle { get; set; }
    }

    /// <summary>
    /// An interface for <see cref="System.Management.Automation.Host.PSHostUserInterface"/> to allow 
    /// implementation of both <see cref="System.Management.Automation.Host.PSHostUserInterface"/> 
    /// and <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> in the same class 
    /// ... since there's no multiple inheritance in C#.
    /// </summary>
    public interface IPSConsole
    {
        Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions);
        int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice);
        // If I put this in the interface, it will break the interface: 
        // the interface would need to return an IPSRawConsole
        // But obviously that wouldn't be helpful here, so we'll just skip it.
        IPSRawConsole RawUI { get; }
        //// There are additional methods, but I've un-objectively decided that these four don't belong in the console

        // void WriteProgress(long sourceId, ProgressRecord record);
        // PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options);
        // PSCredential PromptForCredential(string caption, string message, string userName, string targetName);
        // SecureString ReadLineAsSecureString();
        string ReadLine();
        void Write(string value);
        void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
        void WriteLine(string value);
        void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
        void WriteDebugLine(string message);

        void WriteErrorRecord(ErrorRecord errorRecord);
        void WriteErrorLine(string value);
        void WriteVerboseLine(string message);
        void WriteWarningLine(string message);
        // I added these so I could offer to color 'native' interactions differently
        void WriteNativeLine(string message);
        void WriteNativeErrorLine(string message);
    }

    public interface IPSXamlConsole : IPSConsole
    {
        void WriteXaml();
    }



    //public delegate string HistoryHandler(ref int index);
    public delegate void CommandHandler(string commandLine);
    
    public enum CommandResults {
        Stopped, Failed, Completed
    }

    public interface IPSConsoleControl: IPSConsole
    {
        event CommandHandler ProcessCommand;

        void CommandFinished( System.Management.Automation.Runspaces.PipelineState results );
        void Prompt(string text );

        string CurrentCommand { get; set; }
        
        CommandHistory History  { get; }
        TabExpansion   Expander { get; set; }

        ConsoleScrollBarVisibility VerticalScrollBarVisibility { get; set; }
        ConsoleScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
    }

}
