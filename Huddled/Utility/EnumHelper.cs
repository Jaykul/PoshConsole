using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Huddled.Utility
{
    class EnumHelper
    {

        #region [rgn] Methods (7)

        // [rgn] Public Methods (7)

        public static IEnumerable<TOutput> ConvertAll<TInput, TOutput>(IEnumerable<TInput> input, Converter<TInput, TOutput> converter)
        {
            if (input != null)
            {
                foreach (TInput item in input)
                {
                    yield return converter(item);
                }
            }
        }

        public static T First<T>(System.Collections.ObjectModel.Collection<T> collection)
        {
            return (T)collection[0];
        }

        public static void ForEach<T>(IEnumerable<T> input, Action<T> action)
        {
            if (input != null)
            {
                foreach (T item in input)
                {
                    action(item);
                }
            }
        }

        public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct
        {
            return Enum.IsDefined(typeof(TEnum), value);
        }

        public static TEnum? Parse<TEnum>(string str) where TEnum : struct
        {
            return Parse<TEnum>(str, false);
        }

        public static TEnum? Parse<TEnum>(string str, bool ignoreCase) where TEnum : struct
        {
            TEnum value = (TEnum)Enum.Parse(typeof(TEnum), str, ignoreCase);
            if (IsDefined<TEnum>(value)) return value;
            return null;
        }

        public static void VerifyIsDefined<TEnum>(TEnum value, string argumentName) where TEnum : struct
        {
            if (!IsDefined<TEnum>(value))
            {
                throw new InvalidEnumArgumentException(argumentName, Convert.ToInt32(value), typeof(TEnum));
            }
        }

        #endregion [rgn]

    }
}
