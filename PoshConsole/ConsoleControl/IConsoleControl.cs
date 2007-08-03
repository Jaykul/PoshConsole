using System;
using System.Management.Automation.Host;
using System.Management.Automation;

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

    public delegate void WriteProgressDelegate(long sourceId, ProgressRecord record);

    public interface IConsoleControl
    {
        //void SetCallback(IConsoleControlCallback callback);
        //void InitiateShutdown(int exitCode);

        // TODO: This needs to go somewhere else...
        event WriteProgressDelegate GotProgressUpdate;
        void SendProgressUpdate(long sourceId, ProgressRecord record);

        void EndOutput();
        void Prompt(ConsoleColor foreground, ConsoleColor background, string text );
        void Write(ConsoleColor foreground, ConsoleColor background, string text);
        void WriteLine(ConsoleColor foreground, ConsoleColor background, string text);
        void Write(string text);
        void WriteLine(string text);

        // we don't allow you to set your own colors if you use the "special" write methods
        // and there's no non-"line" methods for it either: since it might not be shown
        // in the console, we don't want you to think you can get all fancy.
        void WriteErrorLine(string value);
        void WriteDebugLine(string value);
        void WriteVerboseLine(string value);
        void WriteWarningLine(string value);

        string CurrentCommand { get; set; }
        string Title { get; set; }
        //bool IsBusy { get; set; }

        ConsoleColor ConsoleForeground { get; set; }
        ConsoleColor ConsoleBackground { get; set; }

        //ConsoleScrollBarVisibility VerticalScrollBarVisibility { get; set; }
        //ConsoleScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
    }

    public interface IConsoleControlBuffered : IConsoleControl
    {
        Size BufferSize { get; set; }
        Size MaxWindowSize { get;  }
        Size WindowSize { get; set; }

        Coordinates CursorPosition { get; set; }
        Coordinates WindowPosition { get; set; }

        BufferCell[,] GetBufferContents(Rectangle rectangle);
        void SetBufferContents(Rectangle rectangle, BufferCell fill);
        void SetBufferContents(Coordinates origin, BufferCell[,] contents);
        void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill);
    }
}
