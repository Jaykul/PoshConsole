using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Markup;

namespace Huddled.PoshConsole
{
    [Serializable]
    [TypeConverter(typeof(HotkeyConverter))]
    [ValueSerializer(typeof(HotkeyValueSerializer))]
    public struct Hotkey : IEquatable<Hotkey>
    {
        public static readonly Hotkey None = new Hotkey();

        private readonly ModifierKeys _modifiers;
        private readonly Key _key;

        public Hotkey(ModifierKeys modifiers, Key key)
        {
            _modifiers = modifiers;
            _key = key;
        }

        public ModifierKeys Modifiers
        {
            get { return _modifiers; }
        }

        public Key Key
        {
            get { return _key; }
        }

        public override string ToString()
        {
            return TypeDescriptor.GetConverter(this).ConvertToInvariantString(this);
        }

        public bool Equals(Hotkey other)
        {
            return other._key == _key &&
                   other._modifiers == _modifiers;
        }

        public override bool Equals(object obj)
        {
            if (obj is Hotkey)
            {
                return Equals((Hotkey)(obj));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return
                Modifiers.GetHashCode() ^
                Key.GetHashCode();
        }

        private bool IsModifierOn(ModifierKeys m)
        {
            return m == (_modifiers & m);
        }

        public static bool operator ==(Hotkey first, Hotkey second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(Hotkey first, Hotkey second)
        {
            return !first.Equals(second);
        }

        public static Hotkey Parse(string str)
        {
            return (Hotkey)TypeDescriptor.GetConverter(typeof(Hotkey)).ConvertFromInvariantString(str);
        }
    }
}
