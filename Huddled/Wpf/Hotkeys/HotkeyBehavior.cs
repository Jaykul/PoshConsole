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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using Huddled.Interop.Keyboard;
using MessageMapping = System.Collections.Generic.KeyValuePair<Huddled.Interop.Windows.NativeMethods.WindowMessage, Huddled.Interop.Windows.NativeMethods.MessageHandler>;
using KeyBindingCollection = System.Collections.ObjectModel.ObservableCollection<System.Windows.Input.KeyBinding>;

namespace Huddled.Wpf
{
   [Serializable, ContentProperty("Hotkeys")]
   public class HotkeyBehavior : NativeBehavior
   {

      /// <summary>
      /// A reference to the Window this command is for
      /// </summary>
      private WeakReference _weakWindow;
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
      /// Whether the window has been SourceInitialized 
      /// and the Hotkeys registered
      /// </summary>
      private bool _isInitialized;
      /// <summary>
      /// The collection of hotkeys that are waiting to be registered
      /// </summary>
      private List<KeyBinding> _keysPending;

      /// <summary>
      /// Gets a value indicating whether this instance has been initialized.
      /// </summary>
      /// <value>
      /// 	<c>true</c> if this instance is initialized; otherwise, <c>false</c>.
      /// </value>
      public bool IsInitialized { get { return _isInitialized; } }
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
      /// Initializes a new instance of the <see cref="HotkeyBehavior"/> class.
      /// </summary>
      public HotkeyBehavior()
      {
         _entries = new KeyBindingCollection();
         _entries.CollectionChanged += OnKeyCollectionChanged;
         _keysPending = new List<KeyBinding>();
      }

      /// <summary>
      /// Handles changes to the key collection so new items can be registered, and old ones unregistered.
      /// </summary>
      /// <param name="sender">The KeyCollection.</param>
      /// <param name="nccea">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
      private void OnKeyCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea)
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
      /// <see cref="window"/> implementations may override this to perfom actions
      /// on the actual window.
      /// </summary>
      /// <param name="window"></param>
      override public void RemoveFrom(Window window)
      {
         _entries.Clear();
         Target = null;
         _windowHandle = IntPtr.Zero;
      }

      /// <summary>
      /// Handles the WM_HOTKEY message
      /// </summary>
      /// <param name="wParam">The wParam.</param>
      /// <param name="lParam">The lParam.</param>
      /// <param name="handled">Set to true if the message was handled, false otherwise</param>
      /// <returns>IntPtr.Zero</returns>
      private IntPtr OnHotkeyPressed(IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         int id = wParam.ToInt32();

         if (_entries.Count > id)
         {
            // BUGBUG: RoutedCommands are disabled byt the WPF system unless Window.IsFocused
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
         return true;
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
      /// <para>
      /// Adds helper method which adds a modifier to a <see cref="ModifierKeys"/> flag.
      /// Keys are added in this order:
      /// <see cref="ModifierKeys.Windows"/>, <see cref="ModifierKeys.Shift"/>,
      /// <see cref="ModifierKeys.Control"/>, <see cref="ModifierKeys.Alt"/>
      /// </para><para>
      /// If all modifiers are set, <see cref="ModifierKeys.None"/> is returned.
      /// Note that the first one missing is added, so if you pass in: 
      /// <see cref="ModifierKeys.Windows"/> | <see cref="ModifierKeys.Control"/>, then
      /// <see cref="ModifierKeys.Windows"/> | <see cref="ModifierKeys.Control"/> | <see cref="ModifierKeys.Shift"/> 
      /// is returned...
      /// </para>
      /// </summary>
      /// <param name="mk">The mk.</param>
      /// <returns>A <see cref="ModifierKeys"/></returns>
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
