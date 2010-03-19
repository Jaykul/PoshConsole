using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace PoshWpf
{
   class BindingConverter : ExpressionConverter
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
               throw new Exception();
            return bindingExpression.ParentBinding;
         }

         return base.ConvertTo(context, culture, value, destinationType);
      }
   }
}
