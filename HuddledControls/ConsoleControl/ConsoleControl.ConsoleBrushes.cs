using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Threading;
using Huddled.WPF.Controls.Interfaces;

namespace Huddled.WPF.Controls
{
    public partial class ConsoleControl
    {
       void ColorsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
       {
          Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
          {
             switch (e.PropertyName)
             {
                case "Black":
                   ConsoleBrushes.Black = new SolidColorBrush(Properties.Colors.Default.Black);
                   goto case "ConsoleColors";
                case "Blue":
                   ConsoleBrushes.Blue = new SolidColorBrush(Properties.Colors.Default.Blue);
                   goto case "ConsoleColors";
                case "Cyan":
                   ConsoleBrushes.Cyan = new SolidColorBrush(Properties.Colors.Default.Cyan);
                   goto case "ConsoleColors";
                case "DarkBlue":
                   ConsoleBrushes.DarkBlue = new SolidColorBrush(Properties.Colors.Default.DarkBlue);
                   goto case "ConsoleColors";
                case "DarkCyan":
                   ConsoleBrushes.DarkCyan = new SolidColorBrush(Properties.Colors.Default.DarkCyan);
                   goto case "ConsoleColors";
                case "DarkGray":
                   ConsoleBrushes.DarkGray = new SolidColorBrush(Properties.Colors.Default.DarkGray);
                   goto case "ConsoleColors";
                case "DarkGreen":
                   ConsoleBrushes.DarkGreen = new SolidColorBrush(Properties.Colors.Default.DarkGreen);
                   goto case "ConsoleColors";
                case "DarkMagenta":
                   ConsoleBrushes.DarkMagenta = new SolidColorBrush(Properties.Colors.Default.DarkMagenta);
                   goto case "ConsoleColors";
                case "DarkRed":
                   ConsoleBrushes.DarkRed = new SolidColorBrush(Properties.Colors.Default.DarkRed);
                   goto case "ConsoleColors";
                case "DarkYellow":
                   ConsoleBrushes.DarkYellow = new SolidColorBrush(Properties.Colors.Default.DarkYellow);
                   goto case "ConsoleColors";
                case "Gray":
                   ConsoleBrushes.Gray = new SolidColorBrush(Properties.Colors.Default.Gray);
                   goto case "ConsoleColors";
                case "Green":
                   ConsoleBrushes.Green = new SolidColorBrush(Properties.Colors.Default.Green);
                   goto case "ConsoleColors";
                case "Magenta":
                   ConsoleBrushes.Magenta = new SolidColorBrush(Properties.Colors.Default.Magenta);
                   goto case "ConsoleColors";
                case "Red":
                   ConsoleBrushes.Red = new SolidColorBrush(Properties.Colors.Default.Red);
                   goto case "ConsoleColors";
                case "White":
                   ConsoleBrushes.White = new SolidColorBrush(Properties.Colors.Default.White);
                   goto case "ConsoleColors";
                case "Yellow":
                   ConsoleBrushes.Yellow = new SolidColorBrush(Properties.Colors.Default.Yellow);
                   goto case "ConsoleColors";

                case "ConsoleColors":
                   {
                      // These are read for each color change.
                      // If the color that was changed is *already* the default background or foreground color ...
                      // Then we need to update the brush!
                      if (Enum.GetName(typeof(ConsoleColor), ((IPSRawConsole)this).ForegroundColor).Equals(e.PropertyName))
                      {
                         Foreground = ConsoleBrushes.BrushFromConsoleColor((ConsoleColor)Enum.Parse(typeof(ConsoleColor), e.PropertyName));
                      }
                      if (Enum.GetName(typeof(ConsoleColor), ((IPSRawConsole)this).BackgroundColor).Equals(e.PropertyName))
                      {
                         Background = ConsoleBrushes.BrushFromConsoleColor((ConsoleColor)Enum.Parse(typeof(ConsoleColor), e.PropertyName));
                      }

                   } break;
                case "DefaultForeground":
                   {
                      ((IPSRawConsole)this).ForegroundColor = Properties.Colors.Default.DefaultForeground;
                   } break;
                case "DefaultBackground":
                   {
                      ((IPSRawConsole)this).BackgroundColor = Properties.Colors.Default.DefaultBackground;
                   } break;
                case "DebugBackground":
                   {
                      ConsoleBrushes.DebugBackground = new SolidColorBrush(Properties.Colors.Default.DebugBackground);
                   } break;
                case "DebugForeground":
                   {
                      ConsoleBrushes.DebugForeground = new SolidColorBrush(Properties.Colors.Default.DebugForeground);
                   } break;
                case "ErrorBackground":
                   {
                      ConsoleBrushes.ErrorBackground = new SolidColorBrush(Properties.Colors.Default.ErrorBackground);
                   } break;
                case "ErrorForeground":
                   {
                      ConsoleBrushes.ErrorForeground = new SolidColorBrush(Properties.Colors.Default.ErrorForeground);
                   } break;
                case "VerboseBackground":
                   {
                      ConsoleBrushes.VerboseBackground = new SolidColorBrush(Properties.Colors.Default.VerboseBackground);
                   } break;
                case "VerboseForeground":
                   {
                      ConsoleBrushes.VerboseForeground = new SolidColorBrush(Properties.Colors.Default.VerboseForeground);
                   } break;
                case "WarningBackground":
                   {
                      ConsoleBrushes.WarningBackground = new SolidColorBrush(Properties.Colors.Default.WarningBackground);
                   } break;
                case "WarningForeground":
                   {
                      ConsoleBrushes.WarningForeground = new SolidColorBrush(Properties.Colors.Default.WarningForeground);
                   } break;
                case "NativeOutputForeground":
                   {
                      ConsoleBrushes.NativeOutputForeground = new SolidColorBrush(Properties.Colors.Default.NativeOutputForeground);
                   } break;
                case "NativeOutputBackground":
                   {
                      ConsoleBrushes.NativeOutputBackground = new SolidColorBrush(Properties.Colors.Default.NativeOutputBackground);
                   } break;
                case "NativeErrorForeground":
                   {
                      ConsoleBrushes.NativeErrorForeground = new SolidColorBrush(Properties.Colors.Default.NativeErrorForeground);
                   } break;
                case "NativeErrorBackground":
                   {
                      ConsoleBrushes.NativeErrorBackground = new SolidColorBrush(Properties.Colors.Default.NativeErrorBackground);
                   } break;

             }
             Properties.Colors.Default.Save();
          });
       }
		
   }
}
