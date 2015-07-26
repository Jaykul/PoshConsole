using System;
using System.Windows.Media;
using Colors = PoshCode.Properties.Colors;

namespace PoshCode.PowerShell
{
	public class ConsoleBrushes
	{
		public Brush Black = new SolidColorBrush(Colors.Default.Black);
		public Brush Blue = new SolidColorBrush(Colors.Default.Blue);
		public Brush Cyan = new SolidColorBrush(Colors.Default.Cyan);
		public Brush DarkBlue = new SolidColorBrush(Colors.Default.DarkBlue);
		public Brush DarkCyan = new SolidColorBrush(Colors.Default.DarkCyan);
		public Brush DarkGray = new SolidColorBrush(Colors.Default.DarkGray);
		public Brush DarkGreen = new SolidColorBrush(Colors.Default.DarkGreen);
		public Brush DarkMagenta = new SolidColorBrush(Colors.Default.DarkMagenta);
		public Brush DarkRed = new SolidColorBrush(Colors.Default.DarkRed);
		public Brush DarkYellow = new SolidColorBrush(Colors.Default.DarkYellow);
		public Brush Gray = new SolidColorBrush(Colors.Default.Gray);
		public Brush Green = new SolidColorBrush(Colors.Default.Green);
		public Brush Magenta = new SolidColorBrush(Colors.Default.Magenta);
		public Brush Red = new SolidColorBrush(Colors.Default.Red);
		public Brush White = new SolidColorBrush(Colors.Default.White);
		public Brush Yellow = new SolidColorBrush(Colors.Default.Yellow);

		public Brush Transparent = Brushes.Transparent;

		public Brush DefaultBackground;
		public Brush DefaultForeground;
		public Brush DebugBackground = new SolidColorBrush(Colors.Default.DebugBackground);
		public Brush DebugForeground = new SolidColorBrush(Colors.Default.DebugForeground);
		public Brush ErrorBackground = new SolidColorBrush(Colors.Default.ErrorBackground);
		public Brush ErrorForeground = new SolidColorBrush(Colors.Default.ErrorForeground);
		public Brush VerboseBackground = new SolidColorBrush(Colors.Default.VerboseBackground);
		public Brush VerboseForeground = new SolidColorBrush(Colors.Default.VerboseForeground);
		public Brush WarningBackground = new SolidColorBrush(Colors.Default.WarningBackground);
		public Brush WarningForeground = new SolidColorBrush(Colors.Default.WarningForeground);
		public Brush NativeErrorBackground = new SolidColorBrush(Colors.Default.NativeErrorBackground);
		public Brush NativeErrorForeground = new SolidColorBrush(Colors.Default.NativeErrorForeground);
		public Brush NativeOutputBackground = new SolidColorBrush(Colors.Default.NativeOutputBackground);
		public Brush NativeOutputForeground = new SolidColorBrush(Colors.Default.NativeOutputForeground);

		//public static ConsoleBrushes Default;

		//static ConsoleBrushes()
		//{
		//   Default = new ConsoleBrushes();
		//}

		public ConsoleBrushes()
		{
			Refresh();
		}

		public void Refresh()
		{
			DefaultBackground = BrushFromConsoleColor(Colors.Default.DefaultBackground);
			DefaultForeground = BrushFromConsoleColor(Colors.Default.DefaultForeground);
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
