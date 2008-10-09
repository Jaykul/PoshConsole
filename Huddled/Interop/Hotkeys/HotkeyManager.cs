using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace Huddled.Interop.Hotkeys
{
   [ContentProperty("Hotkeys")]
   [Serializable]
   public class HotkeyManager : DependencyObject, IDisposable, IList<KeyBinding>, IAddChild //ISupportInitialize, DependencyObject, , , IAddChild, UserControl
   {
      // public event HotkeyEventHandler HotkeyPressed = (HotkeyEventHandler)delegate(object sender, HotkeyEventArgs e) { };

      #region [rgn] Fields (7)
      // private readonly Dictionary<Int32, HotkeyEntry> _entries;
      private IntPtr _hwnd;
      private HwndSource _hwndSource;
      //private Window _window;
      private KeyBindingCollection _entries;
      private List<KeyBinding> _keysPending;
      #endregion [rgn]

      #region DependencyProperties - where the magic happens
      // The HotkeyManager only works when it's attached to a window
      // The attached property "Changed" event is what allows us to find the window to set all the hotkeys on!

      public static readonly DependencyProperty HotkeyManagerProperty =
          DependencyProperty.RegisterAttached("HotkeyManager",
         //            typeof(KeyBindingCollection),
             typeof(HotkeyManager),
             typeof(HotkeyManager),
             new PropertyMetadata( null, HotkeyManagerChanged, CoerceHotkeyManager));

      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      public static void SetHotkeyManager(Window window, HotkeyManager hotkeys)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }
         window.SetValue(HotkeyManagerProperty, hotkeys);
      }

      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      public static HotkeyManager GetHotkeyManager(Window window)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }
         return (HotkeyManager)window.GetValue(HotkeyManagerProperty);
      }

      private static object CoerceHotkeyManager(DependencyObject source, object value)
      {
         if (DesignerProperties.GetIsInDesignMode(source)) return value;
         
         var w = (Window)source;
         var hk = (HotkeyManager) value;

         if(w == null) throw new ArgumentNullException("source");
         if(hk == null) throw new ArgumentNullException("value");

         if(hk.Window != null)
         {
            throw new NotSupportedException("You can't move a HotkeyManager to a new window");
         }

         w.VerifyAccess();

         return hk;
      }

      private static void HotkeyManagerChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
      {
         if (DesignerProperties.GetIsInDesignMode(source)) return;

         var w = (Window) source;

         HotkeyManager hotkeys = args.OldValue as HotkeyManager;
         if (hotkeys != null)
            hotkeys.Clear();

         hotkeys = args.NewValue as HotkeyManager;
         if (hotkeys != null)
            hotkeys.Window = w;

      }

      public static DependencyProperty WindowProperty =
          DependencyProperty.Register("Window",
          typeof(Window), typeof(HotkeyManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, new PropertyChangedCallback(WindowChanged)));

      public Window Window
      {
         get { return (base.GetValue(WindowProperty) as Window); }
         set { base.SetValue(WindowProperty, value); }
      }

      private static void WindowChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
      {
         HotkeyManager manager = source as HotkeyManager;

         if (manager.Handle != IntPtr.Zero)
         {
            throw new InvalidOperationException("The window property cannot be changed once it is set.");
         }
         // store the new window
         Window window = (Window)args.NewValue;
         // get the handle from it
         manager.Handle = new WindowInteropHelper(window).Handle;
         // if we got a handle, yay.
         if (manager.Handle != IntPtr.Zero)
         {
            manager.OnWindowInitialized(window, EventArgs.Empty);
         }
         else // otherwise, hook something up for later.
         {
            window.SourceInitialized += manager.OnWindowInitialized;
         }
      }
      #endregion

      private IntPtr Handle
      {
         get { return _hwnd; }
         set { _hwnd = value; }
      }

      internal bool IsReady
      {
         get
         {
            return _registered;
         }
      }

      public List<KeyBinding> UnregisteredKeys
      {
         get
         {
            return _keysPending;
         }
      }


      #region [rgn] Constructors (2)

      //public HotkeyManager(Window window) : this()
      //{
      //    _window = window;
      //    _hwnd = new WindowInteropHelper(_window).Handle;


      //    if (_hwnd != IntPtr.Zero)
      //    {
      //        OnWindowInitialized(_window, EventArgs.Empty);
      //    }
      //    else
      //    {
      //        window.SourceInitialized += OnWindowInitialized;
      //    }
      //}
      //protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
      //{
      //    base.OnPropertyChanged(e);
      //}

      public HotkeyManager()
      {
         //_window = null;
         _hwnd = IntPtr.Zero;

         _entries = new KeyBindingCollection(this);//  ObservableCollection<KeyBinding>();
         // _entries.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnHotkeysChanged);

         _keysPending = new List<KeyBinding>();
         _hwnd = IntPtr.Zero;
      }
      #endregion [rgn]

      #region [rgn] Methods (9)

      public static ModifierKeys FindUnsetModifier(ModifierKeys mk)
      {
         if (ModifierKeys.Windows != (mk & ModifierKeys.Windows))
         {
            return ModifierKeys.Windows;
         }
         else if (ModifierKeys.Shift != (mk & ModifierKeys.Shift))
         {
            return ModifierKeys.Shift;
         }
         else if (ModifierKeys.Control != (mk & ModifierKeys.Control))
         {
            return ModifierKeys.Control;
         }
         else if (ModifierKeys.Alt != (mk & ModifierKeys.Alt))
         {
            return ModifierKeys.Alt;
         }
         else
         {
            return ModifierKeys.None;
         }
      }

      // [rgn] Public Methods (1)

      public void Dispose()
      {
         if (_hwnd == IntPtr.Zero)
         {
            return;
         }

         Window.Dispatcher.VerifyAccess();
         _entries.Clear();
         _hwnd = IntPtr.Zero;
      }

      bool _registered = false;
      private void OnWindowInitialized(object sender, EventArgs e)
      {
         lock (_entries)
         {
            if (!_registered)
            {

               _hwnd = new WindowInteropHelper(Window).Handle;
               _hwndSource = HwndSource.FromHwnd(_hwnd);
               _hwndSource.AddHook(WndProc);

               //_keysPending.AddRange(_entries);
               //_entries.Clear();
               foreach (KeyBinding key in _entries)
               {
                  RegisterHotkey(key);
               }
               _registered = true;
            }
         }
         //_keysPending.Clear();
      }

      internal bool RegisterHotkey(KeyBinding key)
      {
         if (key.Command is WindowCommand)
         {
            ((WindowCommand)key.Command).Window = this.Window;
         }
         if (!RegisterHotkey(_entries.IndexOf(key), key.Key, key.Modifiers))
         {
            _keysPending.Add(key);
            return false;
         }
         else return true;

         // unecessary // key.CommandTarget = this.Window;
      }

      private bool RegisterHotkey(int id, Key key, ModifierKeys modifiers)
      {
         if (_hwnd == IntPtr.Zero)
         {
            return false;
         }
         else
         {
            int virtualkey = KeyInterop.VirtualKeyFromKey(key);
            return NativeMethods.RegisterHotKey(_hwnd, id, (int)(modifiers), virtualkey);
         }
      }

      internal void UnregisterHotkey(KeyBinding key)
      {
         if (_keysPending.Contains(key))
         {
            _keysPending.Remove(key);
         }
         else
         {
            UnregisterHotkey(_entries.IndexOf(key));
         }
      }

      private bool UnregisterHotkey(int nativeId)
      {
         return NativeMethods.UnregisterHotKey(_hwnd, nativeId);
      }

      [System.Diagnostics.DebuggerHidden]
      private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         const int WM_HOTKEY = 0x312;

         if (msg == WM_HOTKEY)
         {
            int id = wParam.ToInt32();

            if (_entries.Count > id)
            {
               // BUGBUG: RoutedCommands are all disabled unless Window.IsFocused
               if (_entries[id].Command is RoutedCommand) Window.Focus();

               if (_entries[id].Command.CanExecute(_entries[id].CommandParameter))
               {
                  _entries[id].Command.Execute(_entries[id].CommandParameter);
               }

               handled = true;
            }
         }

         return IntPtr.Zero;
      }

      #endregion [rgn]


      //#region Hotkeys as an Attribute
      //public static DependencyProperty HotkeysProperty =
      //    DependencyProperty.RegisterAttached("Hotkeys",
      //    typeof(HotkeyCollection), typeof(HotkeyManager));

      //public HotkeyCollection Hotkeys
      //{
      //    get { return ((HotkeyCollection)base.GetValue(HotkeysProperty)); }
      //    set { base.SetValue(HotkeysProperty, value); }
      //}


      //public class HotkeyCollection : IList<KeyBinding>
      //{


      //private IntPtr _hwnd;

      //public HotkeyCollection()
      //{
      //    _entries = new List<KeyBinding>();
      //    _keysPending = new List<KeyBinding>();
      //    _hwnd = IntPtr.Zero;
      //}

      [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
      public KeyBindingCollection Hotkeys
      {
         get
         {
            return _entries;
         }
         //get
         //{
         //   base.VerifyAccess();
         //   if (this._entries == null)
         //   {
         //      this._entries = new KeyBindingCollection();
         //      if (this._sealed)
         //      {
         //         this._entries.Seal();
         //      }
         //   }
         //   return this._entries;
         //}
      }

      #region IList<KeyBinding> Members
      public int IndexOf(KeyBinding item)
      {
         return _entries.IndexOf(item);
      }
      public void Insert(int index, KeyBinding item)
      {
         throw new NotSupportedException("You can't insert Hotkeys by index");
      }
      public void RemoveAt(int index)
      {
         UnregisterHotkey(_entries[index]);
         /// We don't remove the entry, because we need to preserve the index of items

         //_entries.RemoveAt(index);
         _entries[index].Key = Key.None;
         //throw new Exception("The method or operation is not implemented.");
      }

      public KeyBinding this[int index]
      {
         get
         {
            return _entries[index];
         }
         set
         {
            UnregisterHotkey(_entries[index]);
            _entries[index] = value;
            RegisterHotkey(index, value.Key, value.Modifiers);
         }
      }
      #endregion

      #region IAddChild Members

      public void AddChild(object value)
      {
         if (value is KeyBinding)
            Add(value as KeyBinding);
         else
            throw new ArgumentException("The AddChild method is not implemented except for KeyBinding objects!");
      }

      public void AddText(string text)
      {
         throw new NotSupportedException("The AddText method is not implemented.");
      }

      #endregion

      #region ICollection<KeyBinding> Members
      public void Add(KeyBinding item)
      {
         if (!_entries.Contains(item))
         {
            _entries.Add(item);
         }
         if (Window != null && Window.IsInitialized)
         {
            RegisterHotkey(item);
         }
      }

      public void Clear()
      {
         for (int h = 0; h < _entries.Count; ++h)
         {
            if (_entries[h].Key != Key.None)
            {
               UnregisterHotkey(_entries[h]);
            }
         }
         _entries.Clear();
      }
      public bool Contains(KeyBinding item)
      {
         return _entries.Contains(item);
      }
      public void CopyTo(KeyBinding[] array, int arrayIndex)
      {
         _entries.CopyTo(array, arrayIndex);
      }
      public int Count
      {
         get
         {
            return _entries.Count;
         }
      }
      public bool IsReadOnly
      {
         get
         {
            return false;
         }
      }
      public bool Remove(KeyBinding item)
      {

         return _entries.Remove(item);
      }
      #endregion
      #region IEnumerable<KeyBinding> Members
      public IEnumerator<KeyBinding> GetEnumerator()
      {
         return _entries.GetEnumerator();
      }
      #endregion
      #region IEnumerable Members
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
         return _entries.GetEnumerator();
      }
      #endregion

      //}
      //#endregion Hotkeys as an Attribute

      #region Stuff we need when we're NOT a control
      //#region ISupportInitialize Members
      //public void BeginInit()
      //{
      //    System.Diagnostics.Debug.WriteLine("BEGIN");

      //    // throw new Exception("The method or operation is not implemented.");
      //}

      //public void EndInit()
      //{
      //    System.Diagnostics.Debug.WriteLine("END");

      //    // throw new Exception("The method or operation is not implemented.");
      //}
      //#endregion
      #endregion


   }

}