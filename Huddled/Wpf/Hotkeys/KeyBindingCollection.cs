using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Huddled.Wpf
{
   [Serializable]
   public class KeyBindingCollection : ObservableCollection<KeyBinding>
   {
      private HotkeyManager _manager;

      public KeyBindingCollection(HotkeyManager manager)
      {
         _manager = manager;
      }
      public KeyBindingCollection()
      {
         System.Diagnostics.Debug.WriteLine("Default KeyBindingCollection Constructor");
      }

      /// <summary>
      /// Gets or sets the manager.
      /// </summary>
      /// <value>The manager.</value>
      public HotkeyManager Manager
      {
         get { return _manager; }
         set { _manager = value; }
      }

      /// <summary>
      /// Inserts an item into the collection at the specified index.
      /// </summary>
      /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
      /// <param name="item">The object to insert.</param>
      protected override void InsertItem(int index, KeyBinding item)
      {
         base.InsertItem(index, item);
         if (Manager.IsReady)
         {
            Manager.RegisterHotkey(item);
         }
      }

      /// <summary>
      /// Removes the item at the specified index of the collection.
      /// </summary>
      /// <param name="index">The zero-based index of the element to remove.</param>
      protected override void RemoveItem(int index)
      {
         if (Manager.IsReady)
         {
            Manager.UnregisterHotkey(this[index]);
         }
         base.RemoveItem(index);
      }
      /// <summary>
      /// Replaces the element at the specified index.
      /// </summary>
      /// <param name="index">The zero-based index of the element to replace.</param>
      /// <param name="item">The new value for the element at the specified index.</param>
      protected override void SetItem(int index, KeyBinding item)
      {
         throw new NotSupportedException("KeyBindingCollection doesn't support setting items by index");
      }

      /// <summary>
      /// Moves the item at the specified index to a new location in the collection.
      /// </summary>
      /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
      /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
      protected override void MoveItem(int oldIndex, int newIndex)
      {
         // We don't allow moving, sorry.
         throw new NotSupportedException("You can't move items in the KeyBindingCollection because the index is registered with the Win32 Hotkey API");
         // base.MoveItem(oldIndex, newIndex);
      }
   }
}
