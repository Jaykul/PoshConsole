using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace PoshCode.Wpf.Controls.Utility
{
   public static class OtherHelpers
   {
      public static bool IsModifierOn(this KeyEventArgs e, ModifierKeys modifier)
      {
         return (e.KeyboardDevice.Modifiers & modifier) != ModifierKeys.None;
      }

      public static bool IsScrollLockToggled(this KeyboardDevice keyboard)
      {
         return keyboard.IsKeyToggled(Key.Scroll);
      }


   }
}
