using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using Huddled.Interop.Keyboard;

namespace Huddled.Wpf
{
   /// <summary>
   /// HotkeyManager is the core class of Huddled Hotkeys, and should be attached to a Window
   /// </summary>
   [Serializable, ContentProperty("Hotkeys")]
   public class HotkeyManager : DependencyObject, IDisposable, IList<KeyBinding>, IAddChild //ISupportInitialize, DependencyObject, , , IAddChild, UserControl
   {
      /// <summary>
      /// The Window Handle for the Window we're managing
      /// </summary>
      private IntPtr _windowHandle;

      /// <summary>
      /// The collection of registered hotkeys
      /// </summary>
      private KeyBindingCollection _entries;
      /// <summary>
      /// The collection of hotkeys that are waiting to be registered
      /// </summary>
      private List<KeyBinding> _keysPending;

      #region DependencyProperties - where the magic happens
      // The HotkeyManager only works when it's attached to a Window
      // The attached property "Changed" event is what allows us to find the Window to set all the hotkeys on!

      /// <summary>
      /// The HotkeyManager attached property lets you attach a HotkeyManager to a Window
      /// </summary>
      public static readonly DependencyProperty HotkeyManagerProperty =
          DependencyProperty.RegisterAttached("HotkeyManager",
         //            typeof(KeyBindingCollection),
             typeof(HotkeyManager),
             typeof(HotkeyManager),
             new PropertyMetadata( null, HotkeyManagerChanged, CoerceHotkeyManager));

      /// <summary>
      /// Sets the hotkey manager.
      /// </summary>
      /// <param name="window">The Window.</param>
      /// <param name="hotkeys">The hotkeys.</param>
      public static void SetHotkeyManager(Window window, HotkeyManager hotkeys)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }
         window.SetValue(HotkeyManagerProperty, hotkeys);
      }

      /// <summary>
      /// Gets the hotkey manager.
      /// </summary>
      /// <param name="window">The Window.</param>
      /// <returns></returns>
      public static HotkeyManager GetHotkeyManager(Window window)
      {
         if (window == null)
         {
            throw new ArgumentNullException("window");
         }
         return (HotkeyManager)window.GetValue(HotkeyManagerProperty);
      }

      /// <summary>
      /// Coerces a value to be a hotkey manager.
      /// </summary>
      /// <param name="source">The source.</param>
      /// <param name="value">The value.</param>
      /// <returns></returns>
      private static object CoerceHotkeyManager(DependencyObject source, object value)
      {
         if (DesignerProperties.GetIsInDesignMode(source)) return value;
         
         var window = (Window)source;
         var hotkeyManager = (HotkeyManager) value;

         if(window == null) throw new ArgumentNullException("source");
         if(hotkeyManager == null) throw new ArgumentNullException("value");

         if(hotkeyManager.Window != null)
         {
            throw new NotSupportedException("You can't move a HotkeyManager to a new Window");
         }

         window.VerifyAccess();

         return hotkeyManager;
      }

      /// <summary>
      /// Handles the case where a new HotkeyManager is assigned to a Window
      /// </summary>
      /// <param name="source">The source.</param>
      /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
      private static void HotkeyManagerChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
      {
         if (DesignerProperties.GetIsInDesignMode(source)) return;

         var window = (Window) source;

         HotkeyManager hotkeys = args.OldValue as HotkeyManager;
         if (hotkeys != null)
            hotkeys.Clear();

         hotkeys = args.NewValue as HotkeyManager;
         if (hotkeys != null)
            hotkeys.Window = window;

      }

      /// <summary>
      /// The Window this <see cref="HotkeyManager"/> is managing
      /// </summary>
      public static DependencyProperty WindowProperty =
          DependencyProperty.Register("Window",
          typeof(Window), typeof(HotkeyManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, new PropertyChangedCallback(WindowChanged)));

      /// <summary>
      /// Gets or sets the Window this <see cref="HotkeyManager"/>  is managing.
      /// </summary>
      /// <value>The Window.</value>
      public Window Window
      {
         get { return (base.GetValue(WindowProperty) as Window); }
         set { base.SetValue(WindowProperty, value); }
      }

      /// <summary>
      /// Handle the initial setting of the Window. This property can only be called once.
      /// If it's called again after the Window is initialized, it will throw <see cref="InvalidOperationException"/>.
      /// </summary>
      /// <param name="source">The source.</param>
      /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
      private static void WindowChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
      {
         HotkeyManager manager = source as HotkeyManager;

         if (manager.Handle != IntPtr.Zero)
         {
            throw new InvalidOperationException("The Window property cannot be changed once it is set.");
         }
         // store the new Window
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
      #endregion DependencyProperties - where the magic happens

      /// <summary>
      /// Gets or sets the Window handle.
      /// </summary>
      /// <value>The handle.</value>
      private IntPtr Handle
      {
         get { return _windowHandle; }
         set { _windowHandle = value; }
      }

      /// <summary>
      /// Gets a value indicating whether this instance is ready.
      /// </summary>
      /// <value><c>true</c> if this instance is ready; otherwise, <c>false</c>.</value>
      internal bool IsReady
      {
         get
         {
            return _registered;
         }
      }

      /// <summary>
      /// Gets the unregistered keys.
      /// </summary>
      /// <value>The unregistered keys.</value>
      public List<KeyBinding> UnregisteredKeys
      {
         get
         {
            return _keysPending;
         }
      }



      /// <summary>
      /// Initializes a new instance of the <see cref="HotkeyManager"/> class, with no <see cref="Window"/> and an empty <see cref="Hotkeys"/> collection.
      /// </summary>
      public HotkeyManager()
      {
         throw new InvalidProgramException();
         _entries = new KeyBindingCollection(null);
         _keysPending = new List<KeyBinding>();
      }

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

      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose()
      {
         if (_windowHandle == IntPtr.Zero)
         {
            return;
         }

         Window.Dispatcher.VerifyAccess();
         _entries.Clear();
         _windowHandle = IntPtr.Zero;
      }

      bool _registered = false;
      /// <summary>
      /// Handles the SourceInitialized event of the Window to perform registration of hotkeys.
      /// </summary>
      /// <param name="sender">The sender.</param>
      /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
      private void OnWindowInitialized(object sender, EventArgs e)
      {
         lock (_entries)
         {
            if (!_registered)
            {

               _windowHandle = new WindowInteropHelper(Window).Handle;
               HwndSource.FromHwnd(_windowHandle).AddHook(WndProc);

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

      /// <summary>
      /// Registers the <see cref="KeyBinding"/> as a global hotkey.
      /// </summary>
      /// <param name="key">The key.</param>
      /// <returns></returns>
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

      /// <summary>
      /// Registers the <see cref="Key"/> and <see cref="ModifierKeys"/> as a global hotkey
      /// </summary>
      /// <param name="id">The id.</param>
      /// <param name="key">The key.</param>
      /// <param name="modifiers">The modifiers.</param>
      /// <returns></returns>
      private bool RegisterHotkey(int id, Key key, ModifierKeys modifiers)
      {
         if (_windowHandle == IntPtr.Zero)
         {
            return false;
         }
         else
         {
            int virtualkey = KeyInterop.VirtualKeyFromKey(key);
            return NativeMethods.RegisterHotKey(_windowHandle, id, (int)(modifiers), virtualkey);
         }
      }

      /// <summary>
      /// Unregisters the specified <see cref="KeyBinding"/>.
      /// </summary>
      /// <param name="key">The key.</param>
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

      /// <summary>
      /// Unregisters the <see cref="KeyBinding"/> by id.
      /// </summary>
      /// <param name="nativeId">The native id.</param>
      /// <returns></returns>
      private bool UnregisterHotkey(int nativeId)
      {
         return NativeMethods.UnregisterHotKey(_windowHandle, nativeId);
      }

      /// <summary>
      /// A WndProc we attach to the Window to handle the WM_HOTKEY event
      /// </summary>
      /// <param name="hwnd">The Window Handle.</param>
      /// <param name="msg">The Message (we only handly WM_Hotkey).</param>
      /// <param name="wParam">The wParam.</param>
      /// <param name="lParam">The lParam.</param>
      /// <param name="handled">if set to <c>true</c> [handled].</param>
      /// <returns></returns>
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



      /// <summary>
      /// Gets the hotkeys.
      /// </summary>
      /// <value>The hotkeys.</value>
      [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
      public KeyBindingCollection Hotkeys
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
   }
}