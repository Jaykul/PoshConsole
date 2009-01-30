using System.Collections.Generic;
using System.Windows;
using System.Runtime.Serialization;

namespace PoshWpf
{
   [System.Serializable]
   public class BootsWindowDictionary : Dictionary<int, Window>
   {
      public static int Index { get { return _index; } }
      private static int _index;
      private static readonly BootsWindowDictionary _singleton = new BootsWindowDictionary();
      private BootsWindowDictionary(){}
      //static  BootsWindowDictionary(){}

      protected BootsWindowDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

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
