using System;
using System.Windows.Media;

namespace Huddled.WPF.Controls
{
   public static class ConsoleBrushes
   {
      public static Brush Black = new SolidColorBrush(Properties.Colors.Default.Black);
      public static Brush Blue = new SolidColorBrush(Properties.Colors.Default.Blue);
      public static Brush Cyan = new SolidColorBrush(Properties.Colors.Default.Cyan);
      public static Brush DarkBlue = new SolidColorBrush(Properties.Colors.Default.DarkBlue);
      public static Brush DarkCyan = new SolidColorBrush(Properties.Colors.Default.DarkCyan);
      public static Brush DarkGray = new SolidColorBrush(Properties.Colors.Default.DarkGray);
      public static Brush DarkGreen = new SolidColorBrush(Properties.Colors.Default.DarkGreen);
      public static Brush DarkMagenta = new SolidColorBrush(Properties.Colors.Default.DarkMagenta);
      public static Brush DarkRed = new SolidColorBrush(Properties.Colors.Default.DarkRed);
      public static Brush DarkYellow = new SolidColorBrush(Properties.Colors.Default.DarkYellow);
      public static Brush Gray = new SolidColorBrush(Properties.Colors.Default.Gray);
      public static Brush Green = new SolidColorBrush(Properties.Colors.Default.Green);
      public static Brush Magenta = new SolidColorBrush(Properties.Colors.Default.Magenta);
      public static Brush Red = new SolidColorBrush(Properties.Colors.Default.Red);
      public static Brush White = new SolidColorBrush(Properties.Colors.Default.White);
      public static Brush Yellow = new SolidColorBrush(Properties.Colors.Default.Yellow);

      public static Brush Transparent = Brushes.Transparent;

      public static Brush DefaultBackground = null;
      public static Brush DefaultForeground = null;
      public static Brush DebugBackground = new SolidColorBrush(Properties.Colors.Default.DebugBackground);
      public static Brush DebugForeground = new SolidColorBrush(Properties.Colors.Default.DebugForeground);
      public static Brush ErrorBackground = new SolidColorBrush(Properties.Colors.Default.ErrorBackground);
      public static Brush ErrorForeground = new SolidColorBrush(Properties.Colors.Default.ErrorForeground);
      public static Brush VerboseBackground = new SolidColorBrush(Properties.Colors.Default.VerboseBackground);
      public static Brush VerboseForeground = new SolidColorBrush(Properties.Colors.Default.VerboseForeground);
      public static Brush WarningBackground = new SolidColorBrush(Properties.Colors.Default.WarningBackground);
      public static Brush WarningForeground = new SolidColorBrush(Properties.Colors.Default.WarningForeground);
      public static Brush NativeErrorBackground = new SolidColorBrush(Properties.Colors.Default.NativeErrorBackground);
      public static Brush NativeErrorForeground = new SolidColorBrush(Properties.Colors.Default.NativeErrorForeground);
      public static Brush NativeOutputBackground = new SolidColorBrush(Properties.Colors.Default.NativeOutputBackground);
      public static Brush NativeOutputForeground = new SolidColorBrush(Properties.Colors.Default.NativeOutputForeground);

      static ConsoleBrushes()
      {
         DefaultBackground = BrushFromConsoleColor(Properties.Colors.Default.DefaultBackground);
         DefaultForeground = BrushFromConsoleColor(Properties.Colors.Default.DefaultForeground);
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
         if (color == Black)
            return ConsoleColor.Black;
         if (color == Blue)
            return ConsoleColor.Blue;
         if (color == Cyan)
            return ConsoleColor.Cyan;
         if (color == DarkBlue)
            return ConsoleColor.DarkBlue;
         if (color == DarkCyan)
            return ConsoleColor.DarkCyan;
         if (color == DarkGray)
            return ConsoleColor.DarkGray;
         if (color == DarkGreen)
            return ConsoleColor.DarkGreen;
         if (color == DarkMagenta)
            return ConsoleColor.DarkMagenta;
         if (color == DarkRed)
            return ConsoleColor.DarkRed;
         if (color == DarkYellow)
            return ConsoleColor.DarkYellow;
         if (color == Gray)
            return ConsoleColor.Gray;
         if (color == Green)
            return ConsoleColor.Green;
         if (color == Magenta)
            return ConsoleColor.Magenta;
         if (color == Red)
            return ConsoleColor.Red;
         if (color == White)
            return ConsoleColor.White;
         if (color == Yellow)
            return ConsoleColor.Yellow;
         
         return ConsoleColor.White;
      }
   }
}
