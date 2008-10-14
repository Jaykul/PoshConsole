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
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Huddled.Wpf
{
   /// <summary>
   /// A collection of <see cref="KeyBinding"/>s for the Global Hotkey Behavior.
   /// </summary>
   [Serializable]
   public class KeyBindingCollection : ObservableCollection<KeyBinding>
   {
      private HotkeyBehavior _manager;

      /// <summary>
      /// Initializes a new instance of the <see cref="KeyBindingCollection"/>
      /// class with the specified <see cref="HotkeyBehavior"/>.
      /// </summary>
      /// <param name="manager">The <see cref="HotkeyBehavior"/> 
      /// which manages these <see cref="KeyBinding"/>s.</param>
      public KeyBindingCollection(HotkeyBehavior manager)
      {
         _manager = manager;
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="KeyBindingCollection"/> 
      /// class with no <see cref="HotkeyBehavior"/> manager.
      /// </summary>
      public KeyBindingCollection()
      {
         System.Diagnostics.Debug.WriteLine("Default KeyBindingCollection Constructor");
      }

      /// <summary>
      /// Gets or sets the manager.
      /// </summary>
      /// <value>The manager.</value>
      public HotkeyBehavior Manager
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
         if (Manager.IsInitialized)
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
         if (Manager.IsInitialized)
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
