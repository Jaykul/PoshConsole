using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows.Markup;

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
    }

    internal static class PipelineHelper
    {
        public static bool IsDone(System.Management.Automation.Runspaces.PipelineStateInfo psi)
        {
            return
                psi.State == System.Management.Automation.Runspaces.PipelineState.Completed ||
                psi.State == System.Management.Automation.Runspaces.PipelineState.Stopped ||
                psi.State == System.Management.Automation.Runspaces.PipelineState.Failed;
        }

        public static bool IsFailed(System.Management.Automation.Runspaces.PipelineStateInfo info)
        {
            return info.State == System.Management.Automation.Runspaces.PipelineState.Failed;
        }
    }


    internal static class RectHelper
    {
        public static int Width(System.Management.Automation.Host.Rectangle rect)
        {
            return rect.Right - rect.Left;
        }

        public static int Height(System.Management.Automation.Host.Rectangle rect)
        {
            return rect.Bottom - rect.Top;
        }
    }

    internal static class ThicknessHelper
    {
        public static double Width(System.Windows.Thickness t)
        {
            return t.Left + t.Right;
        }

        public static double Height(System.Windows.Thickness t)
        {
            return t.Top + t.Bottom;
        }
    }

    internal static class XamlHelper
    {
        public static bool TryLoad<T>(System.IO.Stream stream, out T xamlObject, out string errorMessage)
        {
            try
            {
                xamlObject = (T)XamlReader.Load(stream);//, context);
                errorMessage = String.Empty;
                return true;
            }
            catch (Exception ex)
            {
                // ToDo: Offer some help about how to fix this.
                // ToDo: Show (at least one level of) InnerException if it's not null
                string innerMessage = string.Empty;
                if (null != ex.InnerException)
                {
                    innerMessage = ex.InnerException.Message;
                }

                errorMessage = string.Format("Syntax error loading XAML\n{0}\n\n\n\nRoot Cause:\n{1}Stack Trace:\n{2}", ex.Message, innerMessage, ex.StackTrace);

                xamlObject = default(T);
                return false;
            }
        }

        public static bool TryLoadFromFile<T>(string sourceFile, out T xamlObject, out string errorMessage)
        {
            return TryLoad<T>(new System.IO.FileStream(sourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read), out xamlObject, out errorMessage);
        }

        public static bool TryLoadFromFile<T>(string sourceFile, out T xamlObject)
        {
            string errorMessage;
            return TryLoad<T>(new System.IO.FileStream(sourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read), out xamlObject, out errorMessage);
        }


        //public static bool TryLoadFromSource<T>(string xamlSource, out T xamlObject, out string errorMessage)
        //{
        //    return TryLoad<T>(new System.IO.StringReader(xamlSource), out xamlObject, out errorMessage);
        //}

        //public static bool TryLoadFromSource<T>(string xamlSource, out T xamlObject)
        //{
        //    string errorMessage;
        //    return TryLoad<T>(new System.IO.StringReader(xamlSource), out xamlObject, out errorMessage);
        //}


        public static T Load<T>(System.IO.Stream stream)
        {
            return (T)XamlReader.Load(stream);//, context);
        }

        public static T LoadFromFile<T>(string sourceFile)
        {
            return (T)XamlReader.Load(new System.IO.FileStream(sourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read));
        }

        //public static T LoadFromSource<T>(string xamlSource)
        //{
        //    return (T)XamlReader.Load(new System.IO.StringReader(xamlSource));
        //}


    }

}
