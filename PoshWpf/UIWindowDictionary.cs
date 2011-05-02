using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace PoshWpf
{
   [System.Serializable]
   public class UIWindowDictionary : Dictionary<int, Window>
   {
      public static int Index { get { return _index; } }
      private static int _index;
      private static readonly UIWindowDictionary _singleton = new UIWindowDictionary();
      private UIWindowDictionary(){}

      protected UIWindowDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

      public static UIWindowDictionary Instance
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

      public void Remove(Window window)
      {

         if (window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
         {
            window.Dispatcher.Invoke((Action)(() => window.Close()));
         }
         int key = -1;
         foreach (var intWin in _singleton)
         {
            if (intWin.Value == window)
            {
               key = intWin.Key;
               break;
            }
         }
         if (key >= 0)
         {
            base.Remove(key);
         }
      }

      public new void Remove(int index)
      {
         Window window;
         if( base.TryGetValue(index, out window) && window.Dispatcher.Thread.IsAlive && !window.Dispatcher.HasShutdownStarted)
         {
            window.Dispatcher.Invoke((Action)(() => window.Close()));
         }

         base.Remove(index);
      }
   }
}
