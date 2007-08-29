using System;
using System.Windows;

namespace PoshConsole.Interop
{
    public static class HotkeyService
    {
        
		#region [rgn] Fields (1)

		private static readonly DependencyPropertyKey HotkeyServiceHelperPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "HotkeyServiceHelper", typeof(HotkeyServiceHelper), typeof(HotkeyService),
            new FrameworkPropertyMetadata(null)
        );

		#endregion [rgn]

		#region [rgn] Methods (1)

		// [rgn] Private Methods (1)

		private static HotkeyServiceHelper EnsureHotkeyServiceHelper(Window window)
        {
            HotkeyServiceHelper helper = (HotkeyServiceHelper)window.GetValue(HotkeyServiceHelperPropertyKey.DependencyProperty);

            if (helper == null)
            {
                helper = new HotkeyServiceHelper(window);
                window.SetValue(HotkeyServiceHelperPropertyKey, helper);
            }

            return helper;
        }
		
		#endregion [rgn]

		#region [rgn] Nested Classes (1)

		private sealed class HotkeyServiceHelper
        {
            private readonly Window _window;
            private readonly HotkeyManager _manager;

            public HotkeyServiceHelper(Window window)
            {
                _window = window;
                _window.Closed += OnWindowClosed;

                _manager = new HotkeyManager(window);
                _manager.HotkeyPressed += new HotkeyEventHandler(OnHotkeyPressed);

            }

            public void UpdateFocusKey(Hotkey oldValue, Hotkey newValue)
            {
                if (oldValue != Hotkey.None)
                {
                    _manager.Unregister(oldValue);
                }

                if (newValue != Hotkey.None)
                {
                    _manager.Register(newValue);
                }
            }

            private void OnHotkeyPressed(object sender, HotkeyEventArgs e)
            {
                if (e.Hotkey == GetFocusHotkey(_window))
                {
                    if (_window.IsActive)
                    {
                        WindowSwitcher.ActivateNextWindow(_window);
                    }
                    else
                    {
                        _window.Activate();
                    }
                }
            }

            private void OnWindowClosed(object sender, EventArgs e)
            {
                _manager.Dispose();
            }
        }

		#endregion [rgn]
#region DependencyProperty FocusHotkey
        public static readonly DependencyProperty FocusHotkeyProperty = DependencyProperty.RegisterAttached(
            "FocusHotkey", typeof(Hotkey), typeof(HotkeyService),
            new FrameworkPropertyMetadata(Hotkey.None, OnFocusHotkeyChanged)
        );
        [AttachedPropertyBrowsableForType(typeof(Window))]
        public static Hotkey GetFocusHotkey(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            return (Hotkey)element.GetValue(FocusHotkeyProperty);
        }
        public static void SetFocusHotkey(DependencyObject element, Hotkey value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            element.SetValue(FocusHotkeyProperty, value);
        }
        private static void OnFocusHotkeyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            Window window = (Window)target;
            HotkeyServiceHelper helper = EnsureHotkeyServiceHelper(window);

            Hotkey oldValue = (Hotkey)e.OldValue;
            Hotkey newValue = (Hotkey)e.NewValue;

            helper.UpdateFocusKey(oldValue, newValue);
        }
        #endregion
    }
}
