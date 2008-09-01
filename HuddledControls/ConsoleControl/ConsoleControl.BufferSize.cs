using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Documents;
using System.Management.Automation.Host;
using System.Windows.Controls;

namespace Huddled.WPF.Controls
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
                (int)((ScrollViewer.ExtentWidth - (Padding.Left + Padding.Right)) / (FontSize * _characterWidth)) - 1,
                (int)(ScrollViewer.ExtentHeight / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)));
         }
         set
         {

            this.Width = (value.Width * FontSize * _characterWidth) + (ActualWidth - ScrollViewer.ExtentWidth);
            // ToDo: The "Height" of the buffer SHOULD control how much buffer history we keep, in lines...
            //this.Height = (value.Width * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)) + (ActualHeight - sv.ViewportHeight);
         }
      }

      /// <summary>
      /// Gets or sets the size of the window.
      /// </summary>
      /// <value>The size of the window.</value>
      public System.Management.Automation.Host.Size WindowSize
      {
         get
         {
            return new System.Management.Automation.Host.Size(
                (int)((ScrollViewer.ViewportWidth - (Padding.Left + Padding.Right)) / (FontSize * _characterWidth)) - 1,
                (int)(ScrollViewer.ViewportHeight / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)));

         }
         set
         {
            this.Width = (value.Width * FontSize * _characterWidth) + (ActualWidth - ScrollViewer.ViewportWidth);
            this.Height = (value.Height * (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)) + (ActualHeight - ScrollViewer.ViewportHeight); 
         }
      }

      /// <summary>
      /// Gets or sets the size of the max window.
      /// </summary>
      /// <value>The size of the max window.</value>
      public System.Management.Automation.Host.Size MaxWindowSize
      {
         get
         {
            // ToDo: should reduce the size by the difference between the viewport and the window...
            // eg: the topmost VisualParent's ActualWidth - ScrollViewer.ViewportWidth
            return new System.Management.Automation.Host.Size(
                (int)(System.Windows.SystemParameters.PrimaryScreenWidth - (Padding.Left + Padding.Right)
                        / (FontSize * _characterWidth)) - 1,
                (int)(System.Windows.SystemParameters.PrimaryScreenHeight - (Padding.Top + Padding.Bottom)
                        / (Double.IsNaN(Document.LineHeight) ? Document.FontSize : Document.LineHeight)));

         }
         //set { myMaxWindowSize = value; }
      }

      /// <summary>
      /// Gets or sets the size of the max physical window.
      /// </summary>
      /// <value>The size of the max physical window.</value>
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
            //Rect caret = CaretPosition.GetInsertionPosition(LogicalDirection.Forward).GetCharacterRect(LogicalDirection.Backward);
            //_cursorPosition.X = (int)caret.Left;
            //_cursorPosition.Y = (int)caret.Top;
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
            if (tf.Weight == FontWeights.Normal && tf.Style == FontStyles.Normal)
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
