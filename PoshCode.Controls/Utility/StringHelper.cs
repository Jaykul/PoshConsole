using System.Text.RegularExpressions;

namespace PoshCode.Controls.Utility
{
   public static class StringHelper
   {


      private static readonly Regex _CHUNKER = new Regex("[^ \"']+|([\"'])[^\\1]*?\\1[^ \"']*|([\"'])[^\\1]*$| $", RegexOptions.Compiled | RegexOptions.CultureInvariant);
      public static string GetLastWord(this string cmdline, bool trimQuotes = true)
      {
         if (string.IsNullOrWhiteSpace(cmdline))
         {
            return string.Empty;
         }

         string lastWord = null;
         MatchCollection words = _CHUNKER.Matches(cmdline);
         if (words.Count >= 1)
         {
            Match lw = words[words.Count - 1];
            if (trimQuotes)
            {
               lastWord = lw.Value.Trim();
               lastWord = lastWord.Replace("\"", "");
               //if (lastWord.StartsWith("\"", StringComparison.InvariantCultureIgnoreCase))
               //{
               //   lastWord = lastWord.Substring(1);
               //}
               //if (lastWord.EndsWith("\"", StringComparison.InvariantCultureIgnoreCase))
               //{
               //   lastWord = lastWord.Substring(0, lastWord.Length - 1);
               //}
            }
            else
            {
               lastWord = lw.Value.TrimEnd('\r', '\n');
            }           
         }
         return lastWord;
      }

      public static int LineCount(this string text)
      {
         var lineends = new[] { '\r', '\n' };
         int index = 0, count = 0;
         while ((index = 1 + text.IndexOfAny(lineends, index)) > 0)
         {
            count++;
            index += (text[index] == lineends[1]) ? 1 : 0;
         }
         return count;
      }
   }
}
