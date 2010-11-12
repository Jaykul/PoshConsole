using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace PoshConsole
{
   public class CaptionButtonMarginConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         var captionLocation = (Rect)value;

         return new Thickness(0, captionLocation.Top, -captionLocation.Right, 0);
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotSupportedException();
      }
   }
}
