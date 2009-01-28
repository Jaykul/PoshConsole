using System.Collections.Generic;
using System.Windows;

namespace PoshWpf
{
   public class BootsWindowDictionary : Dictionary<int, Window>
   {
      public static int Index { get { return _index; } }
      private static int _index;
      private static readonly BootsWindowDictionary _singleton = new BootsWindowDictionary();
      private BootsWindowDictionary(){}
      static  BootsWindowDictionary(){}

      public static BootsWindowDictionary Instance
      {
         get
         {
            return _singleton;
         }
      }

      public int Add(Window window)
      {
         _index += 1;
         base.Add(_index,window);
         return _index;
      }
   }
}
