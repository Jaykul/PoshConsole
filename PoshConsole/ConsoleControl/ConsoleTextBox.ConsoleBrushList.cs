using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Management.Automation.Host;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Management.Automation;
using System.Threading;
using System.Collections.ObjectModel;
using System.Text;

namespace Huddled.PoshConsole
{
    public partial class RichTextConsole
    {
        void ColorsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (BeginInvoke)delegate
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



        public class ConsoleBrushList
        {
            public Brush Black = new SolidColorBrush(Properties.Colors.Default.Black);
            public Brush Blue = new SolidColorBrush(Properties.Colors.Default.Blue);
            public Brush Cyan = new SolidColorBrush(Properties.Colors.Default.Cyan);
            public Brush DarkBlue = new SolidColorBrush(Properties.Colors.Default.DarkBlue);
            public Brush DarkCyan = new SolidColorBrush(Properties.Colors.Default.DarkCyan);
            public Brush DarkGray = new SolidColorBrush(Properties.Colors.Default.DarkGray);
            public Brush DarkGreen = new SolidColorBrush(Properties.Colors.Default.DarkGreen);
            public Brush DarkMagenta = new SolidColorBrush(Properties.Colors.Default.DarkMagenta);
            public Brush DarkRed = new SolidColorBrush(Properties.Colors.Default.DarkRed);
            public Brush DarkYellow = new SolidColorBrush(Properties.Colors.Default.DarkYellow);
            public Brush Gray = new SolidColorBrush(Properties.Colors.Default.Gray);
            public Brush Green = new SolidColorBrush(Properties.Colors.Default.Green);
            public Brush Magenta = new SolidColorBrush(Properties.Colors.Default.Magenta);
            public Brush Red = new SolidColorBrush(Properties.Colors.Default.Red);
            public Brush White = new SolidColorBrush(Properties.Colors.Default.White);
            public Brush Yellow = new SolidColorBrush(Properties.Colors.Default.Yellow);

            public Brush Transparent = Brushes.Transparent;

            public Brush DefaultBackground = null;
            public Brush DefaultForeground = null;
            public Brush DebugBackground = new SolidColorBrush(Properties.Colors.Default.DebugBackground);
            public Brush DebugForeground = new SolidColorBrush(Properties.Colors.Default.DebugForeground);
            public Brush ErrorBackground = new SolidColorBrush(Properties.Colors.Default.ErrorBackground);
            public Brush ErrorForeground = new SolidColorBrush(Properties.Colors.Default.ErrorForeground);
            public Brush VerboseBackground = new SolidColorBrush(Properties.Colors.Default.VerboseBackground);
            public Brush VerboseForeground = new SolidColorBrush(Properties.Colors.Default.VerboseForeground);
            public Brush WarningBackground = new SolidColorBrush(Properties.Colors.Default.WarningBackground);
            public Brush WarningForeground = new SolidColorBrush(Properties.Colors.Default.WarningForeground);
            public Brush NativeErrorBackground = new SolidColorBrush(Properties.Colors.Default.NativeErrorBackground);
            public Brush NativeErrorForeground = new SolidColorBrush(Properties.Colors.Default.NativeErrorForeground);
            public Brush NativeOutputBackground = new SolidColorBrush(Properties.Colors.Default.NativeOutputBackground);
            public Brush NativeOutputForeground = new SolidColorBrush(Properties.Colors.Default.NativeOutputForeground);

            public ConsoleBrushList()
            {
                DefaultBackground = BrushFromConsoleColor(Properties.Colors.Default.DefaultBackground);
                DefaultForeground = BrushFromConsoleColor(Properties.Colors.Default.DefaultForeground);
            }

            public Brush BrushFromConsoleColor(Nullable<ConsoleColor> color)
            {
                switch (color)
                {
                    case ConsoleColor.Black:
                        return Black;
                    case ConsoleColor.Blue:
                        return Blue;
                    case ConsoleColor.Cyan:
                        return Cyan;
                    case ConsoleColor.DarkBlue:
                        return DarkBlue;
                    case ConsoleColor.DarkCyan:
                        return DarkCyan;
                    case ConsoleColor.DarkGray:
                        return DarkGray;
                    case ConsoleColor.DarkGreen:
                        return DarkGreen;
                    case ConsoleColor.DarkMagenta:
                        return DarkMagenta;
                    case ConsoleColor.DarkRed:
                        return DarkRed;
                    case ConsoleColor.DarkYellow:
                        return DarkYellow;
                    case ConsoleColor.Gray:
                        return Gray;
                    case ConsoleColor.Green:
                        return Green;
                    case ConsoleColor.Magenta:
                        return Magenta;
                    case ConsoleColor.Red:
                        return Red;
                    case ConsoleColor.White:
                        return White;
                    case ConsoleColor.Yellow:
                        return Yellow;
                    default: // for NULL values
                        return Transparent;
                }
            }

            public ConsoleColor ConsoleColorFromBrush(Brush color)
            {
                if (color == Black)
                    return ConsoleColor.Black;
                else if (color == Blue)
                    return ConsoleColor.Blue;
                else if (color == Cyan)
                    return ConsoleColor.Cyan;
                else if (color == DarkBlue)
                    return ConsoleColor.DarkBlue;
                else if (color == DarkCyan)
                    return ConsoleColor.DarkCyan;
                else if (color == DarkGray)
                    return ConsoleColor.DarkGray;
                else if (color == DarkGreen)
                    return ConsoleColor.DarkGreen;
                else if (color == DarkMagenta)
                    return ConsoleColor.DarkMagenta;
                else if (color == DarkRed)
                    return ConsoleColor.DarkRed;
                else if (color == DarkYellow)
                    return ConsoleColor.DarkYellow;
                else if (color == Gray)
                    return ConsoleColor.Gray;
                else if (color == Green)
                    return ConsoleColor.Green;
                else if (color == Magenta)
                    return ConsoleColor.Magenta;
                else if (color == Red)
                    return ConsoleColor.Red;
                else if (color == White)
                    return ConsoleColor.White;
                else if (color == Yellow)
                    return ConsoleColor.Yellow;
                else
                    return ConsoleColor.White;
            }

        }
    }
}