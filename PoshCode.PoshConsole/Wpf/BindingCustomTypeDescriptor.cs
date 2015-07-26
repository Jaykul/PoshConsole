using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace PoshCode.Wpf
{
    public class BindingCustomTypeDescriptor : CustomTypeDescriptor
    {
        public BindingCustomTypeDescriptor(ICustomTypeDescriptor parent) : base(parent) { }

        public override PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptor pd;
            var pdc = new PropertyDescriptorCollection(base.GetProperties().Cast<PropertyDescriptor>().ToArray());
            if ((pd = pdc.Find("Source", false)) != null)
            {
                pdc.Add(TypeDescriptor.CreateProperty(typeof(Binding), pd, new DefaultValueAttribute("null")));
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
                pdc.Add(TypeDescriptor.CreateProperty(typeof(Binding), pd, new DefaultValueAttribute("null")));
                pdc.Remove(pd);
            }
            return pdc;
        }
    }
}