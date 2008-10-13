// Copyright (c) 2008 Joel Bennett

// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// *****************************************************************************
// NOTE: YOU MAY *ALSO* DISTRIBUTE THIS FILE UNDER ANY OF THE FOLLOWING...
// PERMISSIVE LICENSES:
// BSD:	 http://www.opensource.org/licenses/bsd-license.php
// MIT:   http://www.opensource.org/licenses/mit-license.html
// Ms-PL: http://www.opensource.org/licenses/ms-pl.html
// RECIPROCAL LICENSES:
// Ms-RL: http://www.opensource.org/licenses/ms-rl.html
// GPL 2: http://www.gnu.org/copyleft/gpl.html
// *****************************************************************************
// LASTLY: THIS IS NOT LICENSED UNDER GPL v3 (although the above are compatible)
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Huddled.Utility
{
    class EnumHelper
    {
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

        /// <summary>
        /// Parses the specified string.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="str">The string.</param>
        /// <param name="ignoreCase">If <c>true</c>, ignore case.</param>
        /// <returns>An Enum value</returns>
        public static TEnum? Parse<TEnum>(string str, bool ignoreCase) where TEnum : struct
        {
            TEnum value = (TEnum)Enum.Parse(typeof(TEnum), str, ignoreCase);
            if (IsDefined<TEnum>(value)) return value;
            return null;
        }

        /// <summary>
        /// Verifies the argumentName is defined in the enum
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="argumentName">Name of the argument.</param>
        public static void VerifyIsDefined<TEnum>(TEnum value, string argumentName) where TEnum : struct
        {
            if (!IsDefined<TEnum>(value))
            {
                throw new InvalidEnumArgumentException(argumentName, Convert.ToInt32(value), typeof(TEnum));
            }
        }
    }
}
