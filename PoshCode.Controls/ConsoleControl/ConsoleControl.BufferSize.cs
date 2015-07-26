using System;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Host;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Size = System.Management.Automation.Host.Size;

namespace PoshCode.Controls
{
   partial class ConsoleControl
   {
      #region ConsoleSizeCalculations
      //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
      //{
      //    base.OnRenderSizeChanged(sizeInfo);
      //}

      /// <summary>
      /// Gets or sets the name of the specified font.
      /// </summary>
      /// <value></value>
      /// <returns>A font family. The default value is the system dialog font.</returns>
      public new FontFamily FontFamily
      {
         get
         {
            return base.FontFamily;
         }
         set
         {
            base.FontFamily = value;
            UpdateCharacterWidth();
            Document.LineHeight = FontSize * FontFamily.LineSpacing;
         }
      }

      /// <summary>
      /// Gets or sets the font size.
      /// </summary>
      /// <value></value>
      /// <returns>A font size. The default value is the system dialog font size. The font size must be a positive number and in the range of the <see cref="P:System.Windows.SystemFonts.MessageFontSize"></see>.</returns>
      public new double FontSize
      {
         get
         {
            return base.FontSize;
         }
         set
         {
            base.FontSize = value;
            UpdateCharacterWidth();
            Document.LineHeight = FontSize * FontFamily.LineSpacing;
         }
      }

      private double _characterWidth = 0.5498046875; // Default to the size for Consolas
      private double _dpiX;
      private double _dpiY;

      public double CharacterWidthRatio
      {
         get { return _characterWidth; }
      }

      /// <summary>
      /// Gets or sets the size of the buffer.
      /// </summary>
      /// <value>The size of the buffer.</value>
      public Size BufferSize
      { 
         get
         {
            return new Size(
                (int)Math.Floor(((ScrollViewer.ExtentWidth - (Padding.Left + Padding.Right))/ ((Zoom/100.0)*_characterWidth))),
                (int)Math.Floor(ScrollViewer.ExtentHeight / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)));
         }
         set
         {
            // ToDo: The "Height" of the buffer SHOULD control how much buffer history we keep, in lines...
            Width = (value.Width * (Zoom / 100.0) * _characterWidth) + (ActualWidth - ScrollViewer.ExtentWidth);
            //this.Height = (value.Width * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)) + (ActualHeight - sv.ViewportHeight);
         }
      }

      /// <summary>
      /// Gets or sets the size of the Window.
      /// </summary>
      /// <value>The size of the Window.</value>
      public Size WindowSize
      {
         get
         {
            return new Size(
                (int)Math.Floor(((ScrollViewer.ExtentWidth - (Padding.Left + Padding.Right)) / ((Zoom / 100.0) * _characterWidth))),
                (int)Math.Floor(ScrollViewer.ViewportHeight / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)));

         }
         set
         {
            Width = (value.Width * (Zoom / 100.0) * _characterWidth) + (ActualWidth - ScrollViewer.ViewportWidth);
            Height = (value.Height * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)) + (ActualHeight - ScrollViewer.ViewportHeight); 
         }
      }

      /// <summary>
      /// Gets or sets the size of the max Window.
      /// </summary>
      /// <value>The size of the max Window.</value>
      public Size MaxWindowSize
      {
         get
         {
            // ToDo: should reduce the reported "max" size by the difference between the viewport and the Window...
            // eg: the topmost VisualParent's ActualWidth - ScrollViewer.ViewportWidth
            return new Size(
                (int)(SystemParameters.PrimaryScreenWidth - (Padding.Left + Padding.Right) / ((Zoom / 100.0) * _characterWidth)),
                (int)(SystemParameters.PrimaryScreenHeight - (Padding.Top + Padding.Bottom)
                        / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)));

         }
         //set { myMaxWindowSize = value; }
      }

      /// <summary>
      /// Gets the size of the max physical Window.
      /// </summary>
      /// <value>The size of the max physical Window.</value>
      public Size MaxPhysicalWindowSize
      {
         get { return MaxWindowSize; }
         //set { myMaxPhysicalWindowSize = value; }
      }

      private Coordinates _cursorPosition;

      /// <summary>
      /// Gets or sets the cursor position.
      /// </summary>
      /// <value>The cursor position.</value>
      public Coordinates CursorPosition
      {
         get
         {
            Rect caret = (_commandContainer.Child.IsFocused) ? // Document.ContentEnd
               Current.ElementEnd.GetCharacterRect(LogicalDirection.Backward) :
               Selection.End.GetCharacterRect(LogicalDirection.Backward);
            
               _cursorPosition.X = (int)((caret.Left + ScrollViewer.ContentHorizontalOffset) * _characterWidth);
               _cursorPosition.Y = (int)((caret.Top + ScrollViewer.ContentVerticalOffset) / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight));
            
            return _cursorPosition;
         }
         set
         {
            _cursorPosition = value;
            //TextPointer p = GetPositionFromPoint(new Point(value.X * FontSize * _characterWidth, value.Y * Document.LineHeight), true);
            // CaretPosition = p ?? Document.ContentEnd;
         }
      }


      /// <summary>
      /// Updates the value of the CharacterWidthRatio
      /// <remarks>
      /// Called each time the font-family changes
      /// </remarks>
      /// </summary>
      private void UpdateCharacterWidth()
      {

         PresentationSource source = PresentationSource.FromVisual(this);

         if (source != null)
         {
            _dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            _dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
         }


         // Calculate the font width (as a percentage of it's height)            
         foreach (var tf in FontFamily.GetTypefaces().Where(tf => tf.Weight == FontWeights.Normal && tf.Style == FontStyles.Normal))
         {
            GlyphTypeface glyph; 
            if (tf.TryGetGlyphTypeface(out glyph))
            {
               // if this is really a fixed width font, then the widths should be equal:
               // glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'M']]
               // glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'i']]
               // glyph.GetGlyphOutline(glyph.CharacterToGlyphMap['M'], this.FontSize, this.FontSize).Bounds.Width;
               _characterWidth = (new FormattedText(
                                             "MMMMMMMMMM",
                                             CultureInfo.CurrentUICulture,
                                             FlowDirection.LeftToRight, 
                                             tf, 
                                             FontSize,
                                             System.Windows.Media.Brushes.Black, 
                                             null, 
                                             TextFormattingMode.Display)).Width / 10;
               break;
            }
         }
      }

      #endregion ConsoleSizeCalculations

      public BufferCell[,] GetBufferContents(Rectangle rectangle)
      {
         BufferCell[,] bufferCells = new BufferCell[(rectangle.Top - rectangle.Bottom), (rectangle.Right - rectangle.Left)];
         try
         {
            int cur =
               (int)
               ((Next.ElementEnd.GetCharacterRect(LogicalDirection.Backward).Bottom +
                 ScrollViewer.ContentVerticalOffset)/
                (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight));
            int count;
            TextPointer next,
                        end,
                        start =
                           Next.ElementEnd.GetNextContextPosition(LogicalDirection.Backward).GetLineStartPosition(
                              rectangle.Bottom - cur, out count);
            if (start != null)
            {
               for (int ln = 0; ln <= rectangle.Top - rectangle.Bottom; ln++)
               {
                  next = start.GetLineStartPosition(1);
                  start = start.GetPositionAtOffset(rectangle.Left);
                  // if there's text on this line after that char
                  if (start.GetOffsetToPosition(next) <= 0)
                  {
                     // no output on this line
                     continue;
                  }

                  end = start.GetPositionAtOffset(1);
                  int c = 0, width = rectangle.Right - rectangle.Left;
                  while (end.GetOffsetToPosition(next) <= 0 && c < width)
                  {
                     var character = new TextRange(start, end);
                     bufferCells[ln, c++] = new BufferCell(character.Text[0],
                                                           _brushes.ConsoleColorFromBrush(
                                                              character.GetPropertyValue(ForegroundProperty) as Brush),
                                                           _brushes.ConsoleColorFromBrush(
                                                              character.GetPropertyValue(BackgroundProperty) as Brush),
                                                           BufferCellType.Complete);

                     end = end.GetPositionAtOffset(1);
                  }
                  for (; c < width; c++)
                  {
                     bufferCells[ln, c] = new BufferCell(' ', ForegroundColor, BackgroundColor, BufferCellType.Complete);
                  }
                  start = next;
               }
            }
         } catch( Exception ex )
         {
            Write( _brushes.ErrorForeground, _brushes.ErrorBackground, ex.Message);
         }
         return bufferCells;
      }


      /// <summary>
      /// Gets the viewport position (in lines/chars)
      /// </summary>
      /// <returns></returns>
      public Coordinates WindowPosition
      {
         get
         {
            int x = 0;
            int y = (int)(ScrollViewer.ContentVerticalOffset / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight));
            return new Coordinates(x, y);
         }
         set
         {
            ScrollViewer.ScrollToVerticalOffset(value.Y * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight));
         }
      }
   }
}
