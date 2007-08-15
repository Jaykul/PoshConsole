using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;

namespace Huddled.PoshConsole
{
    public sealed class HotkeyEventArgs : EventArgs
    {
        private Hotkey _key;

        public HotkeyEventArgs(Hotkey key)
        {
            _key = key;
        }

        public Hotkey Hotkey
        {
            get { return _key; }
        }
    }

    public delegate void HotkeyEventHandler(object sender, HotkeyEventArgs e);

    public class HotkeyManager : IDisposable
    {
        private struct HotkeyEntry
        {
            public Int32 Id;
            public Hotkey Hotkey;

            public HotkeyEntry(Int32 id, Hotkey hotkey)
            {
                Id = id;
                Hotkey = hotkey;
            }
        }

        private readonly Dictionary<Int32, HotkeyEntry> _entries;
        private readonly List<Hotkey> _keysPending;
        private readonly Window _window;

        private HwndSource _hwndSource;
        private IntPtr _hwnd;
        private Int32 _id;

        public event HotkeyEventHandler HotkeyPressed = delegate { };

        public HotkeyManager(Window window)
        {
            _window = window;

            _keysPending = new List<Hotkey>();
            _entries = new Dictionary<Int32, HotkeyEntry>();
            _hwnd = new WindowInteropHelper(_window).Handle;

            if (_hwnd != IntPtr.Zero)
            {
                OnSourceInitialized(_window, EventArgs.Empty);
            }
            else
            {
                window.SourceInitialized += OnSourceInitialized;
            }
        }

        public void Register(Hotkey key)
        {
            _window.Dispatcher.VerifyAccess();

            if (_hwnd == IntPtr.Zero)
            {
                _keysPending.Add(key);
            }
            else
            {
                RegisterHotkey(key);
            }

        }

        public void Unregister(Hotkey key)
        {
            _window.Dispatcher.VerifyAccess();

            int? nativeId = GetNativeId(key);

            if (nativeId.HasValue)
            {
                UnregisterHotkey(nativeId.Value);
                _entries.Remove(nativeId.Value);
            }
        }

        public void Dispose()
        {
            if (_hwnd == IntPtr.Zero)
            {
                return;
            }

            _window.Dispatcher.VerifyAccess();

            foreach (HotkeyEntry entry in _entries.Values)
            {
                UnregisterHotkey(entry.Id);
            }

            _entries.Clear();
            _hwnd = IntPtr.Zero;
        }

        private int? GetNativeId(Hotkey hotkey)
        {
            foreach (HotkeyEntry entry in _entries.Values)
            {
                if (entry.Hotkey == hotkey)
                {
                    return entry.Id;
                }
            }

            return null;
        }

        private void RegisterHotkey(Hotkey hotkey)
        {
            int id = ++_id;
            int modifiers = (int)(hotkey.Modifiers);
            int virtualkey = KeyInterop.VirtualKeyFromKey(hotkey.Key);

            if (!NativeMethods.RegisterHotKey(_hwnd, id, modifiers, virtualkey))
            {
                throw new Win32Exception();
            }

            _entries.Add(id, new HotkeyEntry (id, hotkey ));
        }

        private void UnregisterHotkey(int nativeId)
        {
            if (!NativeMethods.UnregisterHotKey(_hwnd, nativeId))
            {
                throw new Win32Exception();
            }
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(_window).Handle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);

            if (_keysPending.Count > 0)
            {
                foreach (Hotkey key in _keysPending)
                {
                    RegisterHotkey(key);
                }

                _keysPending.Clear();
            }

            _hwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();

                if (_entries.ContainsKey(id))
                {
                    HotkeyEventArgs e = new HotkeyEventArgs(_entries[id].Hotkey);
                    HotkeyPressed(this, e);

                    handled = true;
                }
            }

            return IntPtr.Zero;
        }
    }
}