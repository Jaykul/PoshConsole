using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace PoshWpf
{
   public class BindingTypeDescriptionProvider : TypeDescriptionProvider
   {
      private static readonly TypeDescriptionProvider _DEFAULT_TYPE_PROVIDER = TypeDescriptor.GetProvider(typeof(Binding));

      public BindingTypeDescriptionProvider() : base(_DEFAULT_TYPE_PROVIDER) { }

      public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
      {
         ICustomTypeDescriptor defaultDescriptor = base.GetTypeDescriptor(objectType, instance);
         return instance == null ? defaultDescriptor : new BindingCustomTypeDescriptor(defaultDescriptor);
      }
   }

   public class BindingCustomTypeDescriptor : CustomTypeDescriptor
   {
      public BindingCustomTypeDescriptor(ICustomTypeDescriptor parent) : base(parent) { }

      public override PropertyDescriptorCollection GetProperties()
      {
         PropertyDescriptor pd;
         var pdc = new PropertyDescriptorCollection(base.GetProperties().Cast<PropertyDescriptor>().ToArray());
         if ((pd = pdc.Find("Source", false)) != null)
         {
            pdc.Add(TypeDescriptor.CreateProperty(typeof(Binding), pd, new Attribute[] { new DefaultValueAttribute("null") }));
            pdc.Remove(pd);
         }
         return pdc;
      }

      public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
      {
         PropertyDescriptor pd;
         var pdc = new PropertyDescriptorCollection(base.GetProperties(attributes).Cast<PropertyDescriptor>().ToArray());
         if ((pd = pdc.Find("Source", false)) != null)
         {
            pdc.Add(TypeDescriptor.CreateProperty(typeof(Binding), pd, new Attribute[] { new DefaultValueAttribute("null") }));
            pdc.Remove(pd);
         }
         return pdc;
      }
   }
}
