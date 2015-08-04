using System.Windows.Input;

namespace PoshCode.Utility
{
   public static class KeyboardHelper
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
