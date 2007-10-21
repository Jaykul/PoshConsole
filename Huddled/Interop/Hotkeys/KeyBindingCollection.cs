using System;
using System.Text;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Huddled.Interop.Hotkeys
{
    [System.Windows.Markup.ContentProperty("Items")]
    public class KeyBindingCollection : ObservableCollection<KeyBinding>
    {

        #region [rgn] Fields (1)

        private HotkeyManager _manager;

        #endregion [rgn]

        #region [rgn] Constructors (2)

        public KeyBindingCollection(HotkeyManager manager)
            : base()
        {
            _manager = manager;
        }
        public KeyBindingCollection()
            : base()
        {
            System.Diagnostics.Debug.WriteLine("Default KeyBindingCollection Constructor");
        }

        #endregion [rgn]

        #region [rgn] Properties (1)

        public HotkeyManager Manager
        {
            get { return _manager; }
            set { _manager = value; }
        }

        #endregion [rgn]

        #region [rgn] Methods (2)

        // [rgn] Protected Methods (2)

        protected override void InsertItem(int index, KeyBinding item)
        {
            base.InsertItem(index, item);
            if (Manager.IsReady)
            {
                Manager.RegisterHotkey(item);
            }
        }

        protected override void RemoveItem(int index)
        {
            if (Manager.IsReady)
            {
                Manager.UnregisterHotkey(this[index]);
            }
            base.RemoveItem(index);
        }
        protected override void SetItem(int index, KeyBinding item)
        {
            throw new NotImplementedException("KeyBindingCollection doesn't support setting items by index");
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            // We don't allow moving, sorry.
            // base.MoveItem(oldIndex, newIndex);
        }

        #endregion [rgn]

    }

}
