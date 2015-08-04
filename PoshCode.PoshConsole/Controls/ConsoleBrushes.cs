using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Colors = PoshCode.Properties.Colors;

namespace PoshCode.Controls
{
    public static class ConsoleBrushes
    {
        public static Brush Black = new SolidColorBrush(Colors.Default.Black);
        public static Brush Blue = new SolidColorBrush(Colors.Default.Blue);
        public static Brush Cyan = new SolidColorBrush(Colors.Default.Cyan);
        public static Brush DarkBlue = new SolidColorBrush(Colors.Default.DarkBlue);
        public static Brush DarkCyan = new SolidColorBrush(Colors.Default.DarkCyan);
        public static Brush DarkGray = new SolidColorBrush(Colors.Default.DarkGray);
        public static Brush DarkGreen = new SolidColorBrush(Colors.Default.DarkGreen);
        public static Brush DarkMagenta = new SolidColorBrush(Colors.Default.DarkMagenta);
        public static Brush DarkRed = new SolidColorBrush(Colors.Default.DarkRed);
        public static Brush DarkYellow = new SolidColorBrush(Colors.Default.DarkYellow);
        public static Brush Gray = new SolidColorBrush(Colors.Default.Gray);
        public static Brush Green = new SolidColorBrush(Colors.Default.Green);
        public static Brush Magenta = new SolidColorBrush(Colors.Default.Magenta);
        public static Brush Red = new SolidColorBrush(Colors.Default.Red);
        public static Brush White = new SolidColorBrush(Colors.Default.White);
        public static Brush Yellow = new SolidColorBrush(Colors.Default.Yellow);

        public static Brush Transparent = Brushes.Transparent;

        public static Brush DefaultBackground;
        public static Brush DefaultForeground;
        public static Brush DebugBackground = new SolidColorBrush(Colors.Default.DebugBackground);
        public static Brush DebugForeground = new SolidColorBrush(Colors.Default.DebugForeground);
        public static Brush ErrorBackground = new SolidColorBrush(Colors.Default.ErrorBackground);
        public static Brush ErrorForeground = new SolidColorBrush(Colors.Default.ErrorForeground);
        public static Brush VerboseBackground = new SolidColorBrush(Colors.Default.VerboseBackground);
        public static Brush VerboseForeground = new SolidColorBrush(Colors.Default.VerboseForeground);
        public static Brush WarningBackground = new SolidColorBrush(Colors.Default.WarningBackground);
        public static Brush WarningForeground = new SolidColorBrush(Colors.Default.WarningForeground);
        public static Brush NativeErrorBackground = new SolidColorBrush(Colors.Default.NativeErrorBackground);
        public static Brush NativeErrorForeground = new SolidColorBrush(Colors.Default.NativeErrorForeground);
        public static Brush NativeOutputBackground = new SolidColorBrush(Colors.Default.NativeOutputBackground);
        public static Brush NativeOutputForeground = new SolidColorBrush(Colors.Default.NativeOutputForeground);

        //public static ConsoleBrushes Default;

        //static ConsoleBrushes()
        //{
        //   Default = new ConsoleBrushes();
        //}

        static ConsoleBrushes()
        {
            Refresh();
        }

        public static void Refresh()
        {
            DefaultBackground = BrushFromConsoleColor(Colors.Default.DefaultBackground);
            DefaultForeground = BrushFromConsoleColor(Colors.Default.DefaultForeground);
        }

        /// <summary>
        /// Get the <see cref="Brush"/> for a specified <see cref="ConsoleColor"/>
        /// </summary>
        /// <param name="color">The <see cref="ConsoleColor"/> to get a brush for</param>
        /// <returns>A <see cref="Brush"/></returns>
        public static Brush BrushFromConsoleColor(ConsoleColor? color)
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

        /// <summary>
        /// Get a <see cref="System.ConsoleColor"/> from one of our <see cref="Brush"/> values.
        ///  </summary>
        /// <param name="color">The <see cref="Brush"/> you want to know the color of</param>
        /// <returns>The corresponding <see cref="System.ConsoleColor"/>, if the <see cref="Brush"/> is one of ours, or White otherwise</returns>
        public static ConsoleColor ConsoleColorFromBrush(Brush color)
        {
            if (Equals(color, Black))
                return ConsoleColor.Black;
            if (Equals(color, Blue))
                return ConsoleColor.Blue;
            if (Equals(color, Cyan))
                return ConsoleColor.Cyan;
            if (Equals(color, DarkBlue))
                return ConsoleColor.DarkBlue;
            if (Equals(color, DarkCyan))
                return ConsoleColor.DarkCyan;
            if (Equals(color, DarkGray))
                return ConsoleColor.DarkGray;
            if (Equals(color, DarkGreen))
                return ConsoleColor.DarkGreen;
            if (Equals(color, DarkMagenta))
                return ConsoleColor.DarkMagenta;
            if (Equals(color, DarkRed))
                return ConsoleColor.DarkRed;
            if (Equals(color, DarkYellow))
                return ConsoleColor.DarkYellow;
            if (Equals(color, Gray))
                return ConsoleColor.Gray;
            if (Equals(color, Green))
                return ConsoleColor.Green;
            if (Equals(color, Magenta))
                return ConsoleColor.Magenta;
            if (Equals(color, Red))
                return ConsoleColor.Red;
            if (Equals(color, White))
                return ConsoleColor.White;
            if (Equals(color, Yellow))
                return ConsoleColor.Yellow;

            return ConsoleColor.White;
        }

        public static ConsoleColor GetBackgroundAsConsoleColor(this TextRange character)
        {
            var brush = character.GetPropertyValue(Control.BackgroundProperty) as Brush;
            return ConsoleColorFromBrush(brush);
        }

        public static ConsoleColor GetForegroundAsConsoleColor(this TextRange character)
        {
            var brush = character.GetPropertyValue(Control.ForegroundProperty) as Brush;
            return ConsoleColorFromBrush(brush);
        }
    }
}
