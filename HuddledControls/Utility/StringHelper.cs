using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Huddled.WPF.Controls.Utility
{
   public static class StringHelper
   {

      #region [rgn] Fields (1)

      public static Regex chunker = new Regex(@"[^ ""']+|([""'])[^\1]*?\1[^ ""']*|([""'])[^\1]*$", RegexOptions.Compiled);

      #endregion [rgn]

      #region [rgn] Methods (2)

      // [rgn] Public Methods (2)

      public static string GetLastWord(this string cmdline)
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

      public static int LineCount(this string text)
      {
         char[] lineends = new char[] { '\r', '\n' };
         int index = 0, count = 0;
         while ((index = 1 + text.IndexOfAny(lineends, index)) > 0)
         {
            count++;
            index += (text[index] == lineends[1]) ? 1 : 0;
         }
         return count;
      }
      #endregion [rgn]

   }

}
