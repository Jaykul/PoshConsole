using System;
using System.Windows.Media;
using PoshCode.Controls.Properties;
using Colors = PoshCode.Controls.Properties.Colors;

namespace PoshCode.Controls
{
   public class ConsoleBrushes
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

      //public static ConsoleBrushes Default;

      //static ConsoleBrushes()
      //{
      //   Default = new ConsoleBrushes();
      //}

      public ConsoleBrushes() {
         Refresh();
      }

      public void Refresh() {
         DefaultBackground = BrushFromConsoleColor(Properties.Colors.Default.DefaultBackground);
         DefaultForeground = BrushFromConsoleColor(Properties.Colors.Default.DefaultForeground);
      }

      /// <summary>
      /// Get the <see cref="Brush"/> for a specified <see cref="ConsoleColor"/>
      /// </summary>
      /// <param name="color">The <see cref="ConsoleColor"/> to get a brush for</param>
      /// <returns>A <see cref="Brush"/></returns>
      public Brush BrushFromConsoleColor(ConsoleColor? color)
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
      public ConsoleColor ConsoleColorFromBrush(Brush color)
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
