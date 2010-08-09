using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace PoshWpf.Converters
{
   public class BindingConverter : ExpressionConverter
   {

      public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
      {
         return (destinationType == typeof(MarkupExtension)) ? true : false;
      }
      public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
      {
         if (destinationType == typeof(MarkupExtension))
         {
            var bindingExpression = value as BindingExpression;
            if (bindingExpression == null)
               throw new ArgumentException("Invalid value, can't convert to BindingExpression", "value");
            return bindingExpression.ParentBinding;
         }

         return base.ConvertTo(context, culture, value, destinationType);
      }
   }
}
