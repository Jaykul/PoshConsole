using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.Windows.NativeMethods.WindowMessage, Huddled.Interop.Windows.NativeMethods.MessageHandler>;
using System.Collections.Specialized;
using Huddled.Interop.Keyboard;

namespace Huddled.Wpf
{
   [Serializable, ContentProperty("Hotkeys")]
   public class HotkeyBehavior : NativeBehavior
   {

      /// <summary>
      /// A reference to the Window this command is for
      /// </summary>
      private WeakReference _weakWindow;
      /// <summary>Gets or sets the Window that is the target of this command
      /// </summary>
      /// <value>The Window.</value>
      public Window Target
      {
         get
         {
            if (_weakWindow == null)
            {
               return null;
            }
            else
            {
               return _weakWindow.Target as Window;
            }
         }
         set
         {
            if (value == null)
            {
               _weakWindow = null;
            }
            else
            {
               _weakWindow = new WeakReference(value);
            }
         }
      }

      /// <summary>
      /// The Window Handle for the Window we're managing
      /// </summary>
      private IntPtr _windowHandle;
      /// <summary>
      /// The Hwnd Presentation source fo the Window we're managing
      /// </summary>
      private HwndSource _hwndSource;
      /// <summary>
      /// The collection of registered hotkeys
      /// </summary>
      private KeyBindingCollection _entries;


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

      /// <summary>
      /// The collection of hotkeys that are waiting to be registered
      /// </summary>
      private List<KeyBinding> _keysPending;

      private bool _isInitialized;
      public bool IsInitialized { get { return _isInitialized; } }

      public HotkeyBehavior()
      {
         _entries = new KeyBindingCollection(this);
         _entries.CollectionChanged += KeyCollectionChanged;
         _keysPending = new List<KeyBinding>();
      }

      private void KeyCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea)
      {
         //var behavior = (HotkeyBehavior)sender;
         if (nccea.Action == NotifyCollectionChangedAction.Add || nccea.Action == NotifyCollectionChangedAction.Replace)
         {
            foreach (KeyBinding item in nccea.NewItems)
            {
               if (IsInitialized)
               {
                  RegisterHotkey(item);
               }
            }
         }
         if (nccea.Action == NotifyCollectionChangedAction.Remove || nccea.Action == NotifyCollectionChangedAction.Replace)
         {
            foreach (KeyBinding item in nccea.OldItems)
            {
               if (IsInitialized)
               {
                  UnregisterHotkey(item);
               }
            }
         }
      }

      /// <summary>
      /// Gets the <see cref="MessageMapping"/>s for this behavior 
      /// (one for each Window Message you need to handle)
      /// </summary>
      /// <value>A collection of <see cref="MessageMapping"/> objects.</value>
      public override IEnumerable<MessageMapping> GetHandlers()
      {
         yield return new MessageMapping(Huddled.Interop.Windows.NativeMethods.WindowMessage.Hotkey, OnHotkeyPressed);
      }

      /// <summary>
      /// Called when this behavior is initially hooked up to a <see cref="System.Windows.Window"/>
      /// <see cref="Behavior"/> implementations may override this to perfom actions
      /// on the actual window (the Chrome behavior uses this to change the template)
      /// </summary>
      /// <param name="window"></param>
      override public void AddTo(Window window)
      {
         Target = window;
         // get the handle from it
         _windowHandle = new WindowInteropHelper(window).Handle;
         // if we got a handle, yay.
         if (_windowHandle != IntPtr.Zero)
         {
            OnWindowSourceInitialized(window, EventArgs.Empty);
         }
         else // otherwise, hook something up for later.
         {
            window.SourceInitialized += OnWindowSourceInitialized;
         }
      }

      /// <summary>
      /// Called when this behavior is unhooked from a <see cref="System.Windows.Window"/>
      /// <see cref="Behavior"/> implementations may override this to perfom actions
      /// on the actual window.
      /// </summary>
      /// <param name="window"></param>
      override public void RemoveFrom(Window window)
      {
         _entries.Clear();
         Target = null;
         _windowHandle = IntPtr.Zero;
      }

      private IntPtr OnHotkeyPressed(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         int id = wParam.ToInt32();

         if (_entries.Count > id)
         {
            // BUGBUG: RoutedCommands are all disabled unless Window.IsFocused
            if (_entries[id].Command is RoutedCommand)
            {
               Target.Focus();
            }

            if (_entries[id].Command.CanExecute(_entries[id].CommandParameter))
            {
               _entries[id].Command.Execute(_entries[id].CommandParameter);
            }

            handled = true;
         }
         return IntPtr.Zero;
      }

      /// <summary>
      /// Handles the SourceInitialized event of the Window to perform registration of hotkeys.
      /// </summary>
      /// <param name="sender">The sender.</param>
      /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
      private void OnWindowSourceInitialized(object sender, EventArgs e)
      {
         lock (_entries)
         {
            if (!_isInitialized)
            {
               _windowHandle = new WindowInteropHelper(Target).Handle;
               foreach (KeyBinding key in _entries)
               {
                  RegisterHotkey(key);
               }
               _isInitialized = true;
            }
         }
         //_keysPending.Clear();
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
      /// Registers the <see cref="KeyBinding"/> as a global hotkey.
      /// </summary>
      /// <param name="key">The key.</param>
      /// <returns></returns>
      internal bool RegisterHotkey(KeyBinding key)
      {
         if (key.Command is WindowCommand)
         {
            ((WindowCommand)key.Command).Window = Target;
         }
         if (!RegisterHotkey(_entries.IndexOf(key), key.Key, key.Modifiers))
         {
            _keysPending.Add(key);
            return false;
         }
         else return true;

         // unecessary // key.CommandTarget = Window;
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

      public static ModifierKeys AddModifier(ModifierKeys mk)
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
   }
}
