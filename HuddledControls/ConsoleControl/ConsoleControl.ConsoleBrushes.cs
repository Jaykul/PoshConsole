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
                   _consoleBrushes.Black = new SolidColorBrush(Properties.Colors.Default.Black);
                   goto case "ConsoleColors";
                case "Blue":
                   _consoleBrushes.Blue = new SolidColorBrush(Properties.Colors.Default.Blue);
                   goto case "ConsoleColors";
                case "Cyan":
                   _consoleBrushes.Cyan = new SolidColorBrush(Properties.Colors.Default.Cyan);
                   goto case "ConsoleColors";
                case "DarkBlue":
                   _consoleBrushes.DarkBlue = new SolidColorBrush(Properties.Colors.Default.DarkBlue);
                   goto case "ConsoleColors";
                case "DarkCyan":
                   _consoleBrushes.DarkCyan = new SolidColorBrush(Properties.Colors.Default.DarkCyan);
                   goto case "ConsoleColors";
                case "DarkGray":
                   _consoleBrushes.DarkGray = new SolidColorBrush(Properties.Colors.Default.DarkGray);
                   goto case "ConsoleColors";
                case "DarkGreen":
                   _consoleBrushes.DarkGreen = new SolidColorBrush(Properties.Colors.Default.DarkGreen);
                   goto case "ConsoleColors";
                case "DarkMagenta":
                   _consoleBrushes.DarkMagenta = new SolidColorBrush(Properties.Colors.Default.DarkMagenta);
                   goto case "ConsoleColors";
                case "DarkRed":
                   _consoleBrushes.DarkRed = new SolidColorBrush(Properties.Colors.Default.DarkRed);
                   goto case "ConsoleColors";
                case "DarkYellow":
                   _consoleBrushes.DarkYellow = new SolidColorBrush(Properties.Colors.Default.DarkYellow);
                   goto case "ConsoleColors";
                case "Gray":
                   _consoleBrushes.Gray = new SolidColorBrush(Properties.Colors.Default.Gray);
                   goto case "ConsoleColors";
                case "Green":
                   _consoleBrushes.Green = new SolidColorBrush(Properties.Colors.Default.Green);
                   goto case "ConsoleColors";
                case "Magenta":
                   _consoleBrushes.Magenta = new SolidColorBrush(Properties.Colors.Default.Magenta);
                   goto case "ConsoleColors";
                case "Red":
                   _consoleBrushes.Red = new SolidColorBrush(Properties.Colors.Default.Red);
                   goto case "ConsoleColors";
                case "White":
                   _consoleBrushes.White = new SolidColorBrush(Properties.Colors.Default.White);
                   goto case "ConsoleColors";
                case "Yellow":
                   _consoleBrushes.Yellow = new SolidColorBrush(Properties.Colors.Default.Yellow);
                   goto case "ConsoleColors";

                case "ConsoleColors":
                   {
                      // These are read for each color change.
                      // If the color that was changed is *already* the default background or foreground color ...
                      // Then we need to update the brush!
                      if (Enum.GetName(typeof(ConsoleColor), ((IPSRawConsole)this).ForegroundColor).Equals(e.PropertyName))
                      {
                         Foreground = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)Enum.Parse(typeof(ConsoleColor), e.PropertyName));
                      }
                      if (Enum.GetName(typeof(ConsoleColor), ((IPSRawConsole)this).BackgroundColor).Equals(e.PropertyName))
                      {
                         Background = _consoleBrushes.BrushFromConsoleColor((ConsoleColor)Enum.Parse(typeof(ConsoleColor), e.PropertyName));
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
                      _consoleBrushes.DebugBackground = new SolidColorBrush(Properties.Colors.Default.DebugBackground);
                   } break;
                case "DebugForeground":
                   {
                      _consoleBrushes.DebugForeground = new SolidColorBrush(Properties.Colors.Default.DebugForeground);
                   } break;
                case "ErrorBackground":
                   {
                      _consoleBrushes.ErrorBackground = new SolidColorBrush(Properties.Colors.Default.ErrorBackground);
                   } break;
                case "ErrorForeground":
                   {
                      _consoleBrushes.ErrorForeground = new SolidColorBrush(Properties.Colors.Default.ErrorForeground);
                   } break;
                case "VerboseBackground":
                   {
                      _consoleBrushes.VerboseBackground = new SolidColorBrush(Properties.Colors.Default.VerboseBackground);
                   } break;
                case "VerboseForeground":
                   {
                      _consoleBrushes.VerboseForeground = new SolidColorBrush(Properties.Colors.Default.VerboseForeground);
                   } break;
                case "WarningBackground":
                   {
                      _consoleBrushes.WarningBackground = new SolidColorBrush(Properties.Colors.Default.WarningBackground);
                   } break;
                case "WarningForeground":
                   {
                      _consoleBrushes.WarningForeground = new SolidColorBrush(Properties.Colors.Default.WarningForeground);
                   } break;
                case "NativeOutputForeground":
                   {
                      _consoleBrushes.NativeOutputForeground = new SolidColorBrush(Properties.Colors.Default.NativeOutputForeground);
                   } break;
                case "NativeOutputBackground":
                   {
                      _consoleBrushes.NativeOutputBackground = new SolidColorBrush(Properties.Colors.Default.NativeOutputBackground);
                   } break;
                case "NativeErrorForeground":
                   {
                      _consoleBrushes.NativeErrorForeground = new SolidColorBrush(Properties.Colors.Default.NativeErrorForeground);
                   } break;
                case "NativeErrorBackground":
                   {
                      _consoleBrushes.NativeErrorBackground = new SolidColorBrush(Properties.Colors.Default.NativeErrorBackground);
                   } break;

             }
             Properties.Colors.Default.Save();
          });
       }
		
   }
}
