using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Documents;
using System.Management.Automation.Host;
using System.Windows.Controls;

namespace Huddled.Wpf.Controls
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
            Document.LineHeight = FontSize * FontFamily.LineSpacing;
         }
      }

      private double _characterWidth = 0.5498046875; // Default to the size for Consolas

      public double CharacterWidthRatio
      {
         get { return _characterWidth; }
      }

      /// <summary>
      /// Gets or sets the size of the buffer.
      /// </summary>
      /// <value>The size of the buffer.</value>
      public System.Management.Automation.Host.Size BufferSize
      {
         get
         {
            return new System.Management.Automation.Host.Size(
               (int)
               Math.Floor(((ScrollViewer.ExtentWidth - (Padding.Left + Padding.Right))/ (FontSize*(Zoom/100.0)*_characterWidth)) - 1), //(1.75 * (Zoom / 100.0))),
                (int)Math.Floor(ScrollViewer.ExtentHeight / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)));
         }
         set
         {
            // ToDo: The "Height" of the buffer SHOULD control how much buffer history we keep, in lines...
            this.Width = (value.Width * FontSize * (Zoom / 100.0) * _characterWidth) + (ActualWidth - ScrollViewer.ExtentWidth);
            //this.Height = (value.Width * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)) + (ActualHeight - sv.ViewportHeight);
         }
      }

      /// <summary>
      /// Gets or sets the size of the Window.
      /// </summary>
      /// <value>The size of the Window.</value>
      public System.Management.Automation.Host.Size WindowSize
      {
         get
         {
            return new System.Management.Automation.Host.Size(
                (int)Math.Floor(((ScrollViewer.ViewportWidth - (Padding.Left + Padding.Right)) / (FontSize * (Zoom / 100.0) * _characterWidth)) - (1.75 * (Zoom / 100.0))),
                (int)Math.Floor(ScrollViewer.ViewportHeight / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)));

         }
         set
         {
            this.Width = (value.Width * FontSize * (Zoom / 100.0) * _characterWidth) + (ActualWidth - ScrollViewer.ViewportWidth);
            this.Height = (value.Height * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)) + (ActualHeight - ScrollViewer.ViewportHeight); 
         }
      }

      /// <summary>
      /// Gets or sets the size of the max Window.
      /// </summary>
      /// <value>The size of the max Window.</value>
      public System.Management.Automation.Host.Size MaxWindowSize
      {
         get
         {
            // ToDo: should reduce the reported "max" size by the difference between the viewport and the Window...
            // eg: the topmost VisualParent's ActualWidth - ScrollViewer.ViewportWidth
            return new System.Management.Automation.Host.Size(
                (int)(System.Windows.SystemParameters.PrimaryScreenWidth - (Padding.Left + Padding.Right)
                        / (FontSize * (Zoom / 100.0) * _characterWidth)) - 1,
                (int)(System.Windows.SystemParameters.PrimaryScreenHeight - (Padding.Top + Padding.Bottom)
                        / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight) * (Zoom / 100.0)));

         }
         //set { myMaxWindowSize = value; }
      }

      /// <summary>
      /// Gets or sets the size of the max physical Window.
      /// </summary>
      /// <value>The size of the max physical Window.</value>
      public System.Management.Automation.Host.Size MaxPhysicalWindowSize
      {
         get { return MaxWindowSize; }
         //set { myMaxPhysicalWindowSize = value; }
      }

      private System.Management.Automation.Host.Coordinates _cursorPosition;

      /// <summary>
      /// Gets or sets the cursor position.
      /// </summary>
      /// <value>The cursor position.</value>
      public System.Management.Automation.Host.Coordinates CursorPosition
      {
         get
         {
            Rect caret = (_commandContainer.Child.IsFocused) ? // Document.ContentEnd
               _current.ElementEnd.GetCharacterRect(LogicalDirection.Backward) :
               Selection.End.GetCharacterRect(LogicalDirection.Backward);
            
               _cursorPosition.X = (int)((caret.Left + ScrollViewer.ContentHorizontalOffset) * CharacterWidthRatio);
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
         // Calculate the font width (as a percentage of it's height)            
         foreach (Typeface tf in FontFamily.GetTypefaces())
         {
            if (tf.Weight == FontWeights.Light && tf.Style == FontStyles.Normal)
            {
               GlyphTypeface glyph;// = new GlyphTypeface();
               if (tf.TryGetGlyphTypeface(out glyph))
               {
                  // if this is really a fixed width font, then the widths should be equal:
                  // glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'M']]
                  // glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'i']]
                  _characterWidth = glyph.AdvanceWidths[glyph.CharacterToGlyphMap[(int)'M']];
                  break;
               }
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
               ((_next.ElementEnd.GetCharacterRect(LogicalDirection.Backward).Bottom +
                 ScrollViewer.ContentVerticalOffset)/
                (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight));
            int count;
            TextPointer next,
                        end,
                        start =
                           _next.ElementEnd.GetNextContextPosition(LogicalDirection.Backward).GetLineStartPosition(
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
            this.Write( _brushes.ErrorForeground, _brushes.ErrorBackground, ex.Message);
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
