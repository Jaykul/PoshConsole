using System;
using System.ComponentModel;
using System.Windows.Data;

namespace PoshCode.Wpf
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
}