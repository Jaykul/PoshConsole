using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using System.Windows.Markup;
using System.ComponentModel.Design.Serialization;
using System.Reflection;

namespace PoshConsole.Interop
{
    public sealed class HotkeyConverter : TypeConverter
    {
        
		#region [rgn] Fields (2)

		private static readonly ConstructorInfo _ctor;
		private const char Delimiter = '+';

		#endregion [rgn]

		#region [rgn] Constructors (1)

		static HotkeyConverter()
        {
            _ctor = typeof(Hotkey).GetConstructor(new Type[] { typeof(ModifierKeys), typeof(Key) });
        }
		
		#endregion [rgn]

		#region [rgn] Methods (10)

		// [rgn] Public Methods (6)

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }
		
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return 
                destinationType == typeof(string) ||
                destinationType == typeof(InstanceDescriptor);
        }
		
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string source = (value as string);

            if (string.IsNullOrEmpty(source))
            {
                throw GetConvertFromException(value);
            }

            return FromString(source, culture);
        }
		
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            Hotkey hotkey = (Hotkey)(value);
            VerifyIsHotkeyDefined(hotkey);

            if (destinationType == typeof(string))
            {
                return ToString(hotkey, culture);
            }

            if (destinationType == typeof(InstanceDescriptor))
            {
                return new InstanceDescriptor(_ctor, new object[] { hotkey.Modifiers, hotkey.Key });
            }

            throw GetConvertToException(value, destinationType);
        }
		
		public static bool IsHotkeyDefined(Hotkey hotkey)
        {
            return
                ModifierKeysConverter.IsDefinedModifierKeys(hotkey.Modifiers) &&
                EnumHelper.IsDefined<Key>(hotkey.Key);
        }
		
		public static void VerifyIsHotkeyDefined(Hotkey hotkey)
        {
            if (!ModifierKeysConverter.IsDefinedModifierKeys(hotkey.Modifiers))
            {
                throw new InvalidEnumArgumentException("hotkey.Modifiers", Convert.ToInt32(hotkey.Modifiers), typeof(ModifierKeys));
            }

            EnumHelper.VerifyIsDefined<Key>(hotkey.Key, "Key");
        }
		
		// [rgn] Private Methods (1)

		private static T ConvertFromString<T>(string text, CultureInfo culture)
        {
            return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(null, culture, text);
        }
		
		// [rgn] Internal Methods (3)

		internal static string ConvertToString(object component, CultureInfo culture)
        {
            return TypeDescriptor.GetConverter(component).ConvertToString(null, culture, component);
        }
		
		internal static Hotkey FromString(string source, CultureInfo culture)
        {
            Key key = Key.None;
            ModifierKeys modifiers = ModifierKeys.None;

            int lastDelimiter = source.LastIndexOf(Delimiter);

            if (lastDelimiter == 0)
            {
                return Hotkey.None;
            }

            string strMod = source.Substring(0, lastDelimiter);
            string strKey = source.Substring(lastDelimiter + 1);

            key = ConvertFromString<Key>(strKey, culture);

            if (lastDelimiter > -1)
            {
                modifiers = ConvertFromString<ModifierKeys>(strMod, culture);
            }

            return new Hotkey(modifiers, key);
        }
		
		internal static string ToString(Hotkey hotkey, CultureInfo culture)
        {
            StringBuilder result = new StringBuilder();

            if (hotkey.Key != Key.None && hotkey.Modifiers != ModifierKeys.None)
            {
                result.Append(ConvertToString(hotkey.Modifiers, culture));
                result.Append(Delimiter);
            }

            result.Append(ConvertToString(hotkey.Key, culture));

            return result.ToString();
        }
		
		#endregion [rgn]

    }

    public sealed class HotkeyValueSerializer : ValueSerializer
    {
        
		#region [rgn] Methods (4)

		// [rgn] Public Methods (4)

		public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }
		
		public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            if (value is Hotkey)
            {
                return HotkeyConverter.IsHotkeyDefined((Hotkey)(value));
            }

            return false;
        }
		
		public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            return HotkeyConverter.FromString(value, CultureInfo.InvariantCulture);
        }
		
		public override string ConvertToString(object value, IValueSerializerContext context)
        {
            return HotkeyConverter.ToString((Hotkey)(value), CultureInfo.InvariantCulture);
        }
		
		#endregion [rgn]

    }
}
