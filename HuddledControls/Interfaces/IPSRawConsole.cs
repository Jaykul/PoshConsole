using System;
using System.Management.Automation.Host;
using Huddled.WPF.Controls.Interfaces;

namespace System.Management.Automation.Host
{
   /// <summary>
   /// An interface for <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> to allow 
   /// implementation along with <see cref="System.Management.Automation.Host.PSHostUserInterface"/> in 
   /// the same class ... since there's no multiple inheritance in C#.
   /// </summary>
   /// <remarks>The need for this interface appears to be an oversight in the PowerShell hosting API.
   /// Since most implementations of <see cref="System.Management.Automation.Host.PSHostUserInterface"/>
   /// and <see cref="System.Management.Automation.Host.PSHostRawUserInterface"/> will be within the same 
   /// control, it seems problematic at best to have them as separate base classes.
   /// </remarks>
   /// <seealso cref="IPSXamlConsole"/>
   /// <seealso cref="IPoshConsoleControl"/>
   /// <seealso cref="IPSConsole"/>
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

      KeyInfo ReadKey(ReadKeyOptions options);
      bool KeyAvailable { get; }
      BufferCell[,] GetBufferContents(Rectangle rectangle);
      void SetBufferContents(Rectangle rectangle, BufferCell fill);
      void SetBufferContents(Coordinates origin, BufferCell[,] contents);

      void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill);
      string WindowTitle { get; set; }
   }
}