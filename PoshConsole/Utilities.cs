using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Huddled.PoshConsole
{
    public class Utilities
    {
        public static bool IsModifierOn(KeyEventArgs e, ModifierKeys modifier)
        {
            return (e.KeyboardDevice.Modifiers & modifier) == modifier;
        }

        public static Regex chunker = new Regex(@"[^ ""']+|([""'])[^\1]*?\1[^ ""']*|([""'])[^\1]*$", RegexOptions.Compiled);
        public static string GetLastWord(string cmdline)
        {
            string lastWord = null;
            MatchCollection words = chunker.Matches(cmdline);
            if (words.Count >= 1)
            {
                Match lw = words[words.Count - 1];
                lastWord = lw.Value;
                if (lastWord[0] == '"')
                {
                    lastWord = lastWord.Replace("\"", string.Empty);
                }
                else if (lastWord[0] == '\'')
                {
                    lastWord = lastWord.Replace("'", string.Empty);
                }
            }
            return lastWord;
        }
    }

    public static class EnumHelper
    {
        public static TEnum? Parse<TEnum>(string str) where TEnum : struct
        {
            return Parse<TEnum>(str, false);
        }
        
        public static TEnum? Parse<TEnum>(string str, bool ignoreCase) where TEnum : struct
        {
            TEnum value = (TEnum)Enum.Parse(typeof(TEnum), str, ignoreCase);

            if (IsDefined<TEnum>(value))
            {
                return value;
            }

            return null;
        }

        public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct
        {
            return Enum.IsDefined(typeof(TEnum), value);
        }

        public static void VerifyIsDefined<TEnum>(TEnum value, string argumentName) where TEnum : struct
        {
            if (!IsDefined<TEnum>(value))
            {
                throw new InvalidEnumArgumentException(argumentName, Convert.ToInt32(value), typeof(TEnum));
            }
        }
    }
}
