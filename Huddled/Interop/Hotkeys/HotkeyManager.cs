using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace Huddled.Interop.Hotkeys
{
    [ContentProperty("Items")]
    public class KeyBindingCollection : ObservableCollection<KeyBinding> { }

    //[ContentProperty("Hotkeys")]
    [ContentProperty("Items")]
    public class HotkeyManager : FrameworkElement, IDisposable, ISupportInitialize, IList<KeyBinding>//, DependencyObject, , , IAddChild, UserControl
    {
        // public event HotkeyEventHandler HotkeyPressed = (HotkeyEventHandler)delegate(object sender, HotkeyEventArgs e) { };

        #region [rgn] Fields (7)
		// private readonly Dictionary<Int32, HotkeyEntry> _entries;
		private IntPtr _hwnd;
		private HwndSource _hwndSource;
		private Window _window;
        #endregion [rgn]


        #region AttachedProperties
        public static DependencyProperty HotkeysProperty =
            DependencyProperty.RegisterAttached("Hotkeys",
            typeof(KeyBindingCollection),
            typeof(HotkeyManager),
            new FrameworkPropertyMetadata(new KeyBindingCollection(), FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender, new PropertyChangedCallback(KeyManagerChanged)));
        public static void SetHotkeys(UIElement element, HotkeyManager value)
        {
            element.SetValue(HotkeysProperty, value);
        }
        public static HotkeyManager GetHotkeys(UIElement element)
        {
            return (HotkeyManager)element.GetValue(HotkeysProperty);
        }

        public static void KeyManagerChanged(DependencyObject source, DependencyPropertyChangedEventArgs args) 
        {
            //HotkeyManager hkm = (HotkeyManager)source;

            HotkeyManager hotkeys = args.OldValue as HotkeyManager;
            if( hotkeys != null )
                hotkeys.Clear();

            hotkeys = args.NewValue as HotkeyManager;
            if( hotkeys != null )
                hotkeys.Window = source as Window;

        }
        #endregion

        public static DependencyProperty WindowProperty =
            DependencyProperty.Register("Window",
            typeof(Window), typeof(HotkeyManager), new FrameworkPropertyMetadata(null,  FrameworkPropertyMetadataOptions.None, new PropertyChangedCallback(WindowChanged)));

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

        private IntPtr Handle
        {
            get { return _hwnd; }
            set { _hwnd = value; }
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
            _window = null;
            _hwnd = IntPtr.Zero;

            #region Stuff we need when we're a control
            //// hook up a binding so we can find our parent window
            //Binding bind = new Binding();
            //bind.Mode = BindingMode.OneWay;
            //bind.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1);
            //BindingOperations.SetBinding(this, HotkeyManager.WindowProperty, bind);

            //this.Visibility = Visibility.Collapsed;
            #endregion

            _entries = new ObservableCollection<KeyBinding>();
            _entries.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnHotkeysChanged);

            _keysPending = new List<KeyBinding>();
            _hwnd = IntPtr.Zero;
        }

        public static void OnHotkeysChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    // TODO: Should we handle this?
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    // TODO: Remove pending also?
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
        }
		
		#endregion [rgn]

        #region [rgn] Methods (9)

		// [rgn] Public Methods (1)

		public void Dispose()
        {
            if (_hwnd == IntPtr.Zero)
            {
                return;
            }

            _window.Dispatcher.VerifyAccess();

            _entries.Clear();

            _hwnd = IntPtr.Zero;
        }
		
		// [rgn] Private Methods (8)

        //private void AddHotkey(KeyBinding binding)
        //{
        //    _entries.Add(binding);
        //    RegisterHotkey(_entries.Count - 1, binding.Key, binding.Modifiers);
        //}
		
		/// <summary>
        /// Adds the specified bindings to the pending list...
        /// </summary>
        /// <param name="bindings">The bindings.</param>
        //private void AddPending(IEnumerable<KeyBinding> bindings)
        //{
        //    _keysPending.AddRange(bindings);
        //}
		
		private void HookWindow(Window host)
        {
            _window = host;
            _hwnd = new WindowInteropHelper(_window).Handle;

            if (_hwnd != IntPtr.Zero)
            {
                OnWindowInitialized(_window, EventArgs.Empty);
            }
            else
            {
                _window.SourceInitialized += OnWindowInitialized;
            }
        }
		
		//public void Register(Hotkey key)
        //{
        //    _window.Dispatcher.VerifyAccess();

        //    if (_hwnd == IntPtr.Zero)
        //    {
        //        _keysPending.Add(key);
        //    }
        //    else
        //    {
        //        RegisterHotkey(key);
        //    }

        //}
        //public void Unregister(Hotkey key)
        //{
        //    _window.Dispatcher.VerifyAccess();

        //    int? nativeId = GetNativeId(key);

        //    if (nativeId.HasValue)
        //    {
        //        UnregisterHotkey(nativeId.Value);
        //        _entries.Remove(nativeId.Value);
        //    }
        //}
        bool _registered = false;
        private void OnWindowInitialized(object sender, EventArgs e)
        {
            if (!_registered)
            {
                _registered = true;

                _hwnd = new WindowInteropHelper(Window).Handle;
                _hwndSource = HwndSource.FromHwnd(_hwnd);
                _hwndSource.AddHook(WndProc);

                //_keysPending.AddRange(_entries);
                //_entries.Clear();
                foreach (KeyBinding key in _entries)
                {
                    RegisterHotkey(key);
                }
            }
            //_keysPending.Clear();
        }

        private void RegisterHotkey( KeyBinding key )
        {
            RegisterHotkey(_entries.IndexOf(key), key.Key, key.Modifiers);
            if (key.Command is WindowCommand)
            {
                ((WindowCommand)key.Command).Window = this.Window;
            }
            // unecessary // key.CommandTarget = this.Window;
        }

		private void RegisterHotkey( int id, Key key, ModifierKeys modifiers)
        {
            int virtualkey = KeyInterop.VirtualKeyFromKey(key);

            if (!NativeMethods.RegisterHotKey(_hwnd, id, (int)(modifiers), virtualkey))
            {
                throw new Win32Exception();
            }
            //_entries.Add(id, new HotkeyEntry (id, hotkey ));
        }

        private void UnregisterHotkey(KeyBinding key)
        {
            UnregisterHotkey(_entries.IndexOf(key));
        }

        private void UnregisterHotkey(int nativeId)
        {
            if (!NativeMethods.UnregisterHotKey(_hwnd, nativeId))
            {
                throw new Win32Exception();
            }
        }
		
		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();

                if (_entries.Count > id)
                {
                    // BUGBUG: RoutedCommands are all disabled unless Window.IsFocused
                    if(_entries[id].Command is RoutedCommand) Window.Focus();

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
            private ObservableCollection<KeyBinding> _entries;
            //private List<KeyBinding> _entries;
            private List<KeyBinding> _keysPending;
            
            //private IntPtr _hwnd;

            //public HotkeyCollection()
            //{
            //    _entries = new List<KeyBinding>();
            //    _keysPending = new List<KeyBinding>();
            //    _hwnd = IntPtr.Zero;
            //}

            public ObservableCollection<KeyBinding> Items
            {
                get
                {
                    return _entries;
                }
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
                UnregisterHotkey( _entries[index] );
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
            #region ICollection<KeyBinding> Members
            public void Add(KeyBinding item)
            {
                if (_hwnd == IntPtr.Zero)
                {
                    _keysPending.Add(item);
                }
                else
                {
                    _entries.Add(item);
                  
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
            #region ISupportInitialize Members
            public void BeginInit()
            {
                System.Diagnostics.Debug.WriteLine("BEGIN");

                // throw new Exception("The method or operation is not implemented.");
            }

            public void EndInit()
            {
                System.Diagnostics.Debug.WriteLine("END");

                // throw new Exception("The method or operation is not implemented.");
            }
            #endregion
        #endregion
    }
}