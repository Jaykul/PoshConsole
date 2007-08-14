using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
//// Forms based
//using System.Windows.Forms;
// WPF based
using System.Windows;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Huddled.Hotkeys
{
	[Serializable]
	public class Hotkey
	{
		public Modifiers Modifiers;
		public Keys Key;
		internal int Id;

		/// <summary>
		/// Parses a hotkey from a string representation, like:
		/// <list type="">
		/// <item>win|ctrl|A</item>
		/// <item>win+shift+B</item>
		/// <item>ctrl+alt|OemTilde</item>
		/// </list>
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns></returns>
		public static Hotkey Parse(string key)
		{
			Regex rx = new Regex(@"(?:(?:(?<win>win)|(?<ctrl>ctrl|control|ctl)|(?<alt>alt)|(?<shift>shift))\s*(?:[\|+-])\s*)+(?<key>.*)", RegexOptions.IgnoreCase);

			Modifiers mods = Modifiers.None;

			Match m = rx.Match(key);			
			
			if(!m.Success) return null;

			if(m.Groups["win"].Success)
				mods |= Modifiers.Win;
			if(m.Groups["ctrl"].Success)
				mods |= Modifiers.Control;
			if(m.Groups["alt"].Success)
				mods |= Modifiers.Alt;
			if(m.Groups["shift"].Success)
				mods |= Modifiers.Shift;

			return new Hotkey(mods, (Keys)Enum.Parse(typeof(Keys), m.Groups["key"].Value, true));

		}
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			StringBuilder key = new StringBuilder();

			if((Modifiers.Win & Modifiers) == Modifiers.Win)
			{
				key.Append("WIN + ");
			}
			if((Modifiers.Control & Modifiers) == Modifiers.Control)
			{
				key.Append("CTRL + ");
			}
			if((Modifiers.Alt & Modifiers) == Modifiers.Alt)
			{
				key.Append("ALT + ");
			}
			if((Modifiers.Shift & Modifiers) == Modifiers.Shift)
			{
				key.Append("SHIFT + ");
			}
			key.Append(Key);

			return key.ToString();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Hotkey"/> class.
		/// </summary>
		/// <param name="Modifiers">The modifiers.</param>
		/// <param name="Key">The key.</param>
		public Hotkey(Modifiers Modifiers, Keys Key)
		{
			this.Modifiers = Modifiers;
			this.Key = Key;
		}

		public override bool Equals(object obj)
		{
			if(obj is Hotkey)
			{
				if(Id > 0 && ((Hotkey)obj).Id > 0)
				{
					return Id == ((Hotkey)obj).Id;
				}
				else
				{
					return ((Hotkey)obj).Key == this.Key && ((Hotkey)obj).Modifiers == this.Modifiers;
				}
			}
			return false;
		}

		/// <summary>
		/// Calls the base hashcode
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}
	}

	/// <summary>
	/// The Hotkey manager
	/// </summary>
	public class WPFHotkeyManager : IDisposable
	{
		private Dictionary<int, Hotkey> Hotkeys;
		private ObservableCollection<Hotkey> observableHotkeys;
		private IntPtr handle;
		private int id = 0;

		public delegate void HotkeyPressedEvent(Window window, Hotkey hotkey);
		public event HotkeyPressedEvent HotkeyPressed;

		protected WindowInteropHelper window;
		private Window actualWindow;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:HotKeys.Manager"/> class.
		/// </summary>
		/// <param name="HotkeyWindow">The window that will receive notifications when the Hotkeys are pressed.</param>
		public WPFHotkeyManager(Window TopLevelWindow)
		{
			actualWindow = TopLevelWindow;

			if(!actualWindow.IsArrangeValid)
			{
				actualWindow.SourceInitialized += new EventHandler(OnSourceInitialized);
			}
			else OnSourceInitialized(actualWindow, null);
		}

		void OnSourceInitialized(object sender, EventArgs e)
		{
			window = new WindowInteropHelper(actualWindow);
			handle = window.Handle;

			HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WndProc));

			Hotkeys = new Dictionary<int, Hotkey>();
			observableHotkeys = new ObservableCollection<Hotkey>();
			// AssignHandle(handle);
		}


		public ReadOnlyCollection<Hotkey> ReadOnlyHotkeys
		{
			get
			{
				Hotkey[] hotkeys = new Hotkey[Hotkeys.Count];
				Hotkeys.Values.CopyTo(hotkeys, 0);
				return new System.Collections.ObjectModel.ReadOnlyCollection<Hotkey>(hotkeys);
			}
		}

		public ObservableCollection<Hotkey> ObservableHotkeys
		{
			get
			{
				return observableHotkeys;
			}
		}

		/// <summary>
		/// Gets the hotkey from the Id sent by Windows in the WM.HOTKEY message
		/// </summary>
		/// <param name="Id">The id.</param>
		/// <returns></returns>
		public Hotkey GetKey(int Id)
		{
			return Hotkeys[Id];
		}


		private delegate bool HotkeyDelegate(Hotkey key);

		/// <summary>Register a new hotkey, and add it to our collection
		/// </summary>
		/// <param name="key">A reference to the Hotkey.</param>
		/// <returns>True if the hotkey was set successfully.</returns>
		public bool Register(Hotkey key)
		{
			if(handle == IntPtr.Zero) throw new InvalidOperationException("You can't register hotkeys until your Window is loaded.");
			if( actualWindow.Dispatcher.CheckAccess() ) {
				if(NativeMethods.RegisterHotKey(handle, ++id, key.Modifiers, key.Key))
				{
					key.Id = id;
					Hotkeys.Add(id, key);
					observableHotkeys.Add(key);
					return true;
				}
				else
				{
					int lastError = Marshal.GetLastWin32Error();
					Marshal.ThrowExceptionForHR(lastError);
					return false;
				}
			} else {
				return (bool)actualWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new HotkeyDelegate(Register), key);
			}
			
		}

		/// <summary>Unregister the specified hotkey if it's in our collection
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>True if we successfully unregister the hotkey.</returns>
		public bool Unregister(Hotkey key)
		{
			if(actualWindow.Dispatcher.CheckAccess())
			{

				int id = IndexOf(key);
				if(id > 0)
				{
					if(NativeMethods.UnregisterHotKey(handle, id))
					{
						Hotkeys.Remove(id);
						observableHotkeys.Remove(key);
						return true;
					}
					else
					{
						int lastError = Marshal.GetLastWin32Error();
						Marshal.ThrowExceptionForHR(lastError);
						return false;
					}
				}
				else return false;
			} else {
				return (bool)actualWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new HotkeyDelegate(Unregister), key);
			}
		}

		/// <summary>Clears the hotkey collection
		/// </summary>
		public void Clear()
		{
			foreach(KeyValuePair<int, Hotkey> key in Hotkeys)
			{
				NativeMethods.UnregisterHotKey(handle, key.Key);
			}
			// clear afterward so we don't break our enumeration
			Hotkeys.Clear();
		}

		/// <summary>Free all hotkey resources...
		/// </summary>
		public void Dispose()
		{
			Clear();
//			ReleaseHandle();
			handle = IntPtr.Zero;
		}

		/// <summary>
		/// Override the base WndProc ... but in WPF it's the old-fashioned multi-parameter WndProc
		/// <remarks>
		/// The .Net Framework is starting to feel ridiculously cobbled together ... why on earth 
		/// should WPF apps need to register WndProc's any differently than Windows.Forms apps?
		/// </remarks>
		/// </summary>
		/// <param name="hwnd">The window handle.</param>
		/// <param name="msg">The message.</param>
		/// <param name="wParam">The high word</param>
		/// <param name="lParam">The low word</param>
		/// <param name="handled"><c>true</c> if the message was already handled.</param>
		/// <returns>IntPtr.Zero - I have no idea what this is supposed to return.</returns>

        // This attribute tells the debugger to mark this function as non-user code so we don't end up in here all the time
        [System.Diagnostics.DebuggerNonUserCode]
		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if(WM.Hotkey == msg)
			{
				if(Hotkeys.ContainsKey(wParam.ToInt32()))
				{
                    if (HotkeyPressed != null)
                    {
                        HotkeyPressed(actualWindow, Hotkeys[wParam.ToInt32()]);
                        handled = true;
                    }
				}
			}
			return IntPtr.Zero;
		}

		#region ICollection<Hotkey> Members

		public void Add(Hotkey item)
		{
			if(Hotkeys.ContainsValue(item))
			{
				throw new ArgumentException("That Hotkey is already registered.");
			}
			Register(item);
		}

		public bool Contains(Hotkey item)
		{
			return Hotkeys.ContainsValue(item);
		}

		public void CopyTo(Hotkey[] array, int arrayIndex)
		{
			Hotkeys.Values.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return Hotkeys.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(Hotkey item)
		{
			return Unregister(item);
		}

		#endregion

		#region IEnumerable<Hotkey> Members

		public IEnumerator<Hotkey> GetEnumerator()
		{
			return Hotkeys.Values.GetEnumerator();
		}

		#endregion

		#region IList<Hotkey> Members

		public int IndexOf(Hotkey item)
		{
			if(item.Id > 0 && Hotkeys.ContainsKey(item.Id))
			{
				return item.Id;
			}
			else if(Hotkeys.ContainsValue(item))
			{
				foreach(KeyValuePair<int, Hotkey> k in Hotkeys)
				{
					if(item.Equals(k.Value))
					{
						item.Id = k.Value.Id;
						return item.Id;
					}
				}
			}
			
			throw new ArgumentOutOfRangeException("The hotkey \"{0}\" is not in this hotkey manager.");
		}

		public void Insert(int index, Hotkey item)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void RemoveAt(int index)
		{
			Remove(Hotkeys[index]);
		}

		public Hotkey this[int index]
		{
			get
			{
				return Hotkeys[index];
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}

	[Flags,Serializable]
	public enum Modifiers
	{
		None = 0,
		Alt = 0x1,
		Control = 0x2,
		Shift = 0x4,
		Win = 0x8
	}

    [Serializable]
    public enum Keys : int
    {
        KeyCode = 65535,
        Modifiers = -65536,
        None = 0,
        Back = 8,
        Tab = 9,
        LineFeed = 10,
        Clear = 12,
        Return = 13,
        Enter = 13,
        ShiftKey = 16,
        ControlKey = 17,
        Menu = 18,
        Pause = 19,
        Capital = 20,
        CapsLock = 20,
        KanaMode = 21,
        HanguelMode = 21,
        HangulMode = 21,
        JunjaMode = 23,
        FinalMode = 24,
        HanjaMode = 25,
        KanjiMode = 25,
        Escape = 27,
        Space = 32,
        Prior = 33,
        PageUp = 33,
        Next = 34,
        PageDown = 34,
        End = 35,
        Home = 36,
        Left = 37,
        Up = 38,
        Right = 39,
        Down = 40,
        Select = 41,
        Print = 42,
        Execute = 43,
        Snapshot = 44,
        PrintScreen = 44,
        Insert = 45,
        Delete = 46,
        Help = 47,
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,
        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,
        LWin = 91,
        RWin = 92,
        Apps = 93,
        Sleep = 95,
        NumPad0 = 96,
        NumPad1 = 97,
        NumPad2 = 98,
        NumPad3 = 99,
        NumPad4 = 100,
        NumPad5 = 101,
        NumPad6 = 102,
        NumPad7 = 103,
        NumPad8 = 104,
        NumPad9 = 105,
        Multiply = 106,
        Add = 107,
        Separator = 108,
        Subtract = 109,
        Decimal = 110,
        Divide = 111,
        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        F10 = 121,
        F11 = 122,
        F12 = 123,
        F13 = 124,
        F14 = 125,
        F15 = 126,
        F16 = 127,
        F17 = 128,
        F18 = 129,
        F19 = 130,
        F20 = 131,
        F21 = 132,
        F22 = 133,
        F23 = 134,
        F24 = 135,
        NumLock = 144,
        Scroll = 145,
        LShiftKey = 160,
        RShiftKey = 161,
        LControlKey = 162,
        RControlKey = 163,
        LMenu = 164,
        RMenu = 165,
        BrowserBack = 166,
        BrowserForward = 167,
        BrowserRefresh = 168,
        BrowserStop = 169,
        BrowserSearch = 170,
        BrowserFavorites = 171,
        BrowserHome = 172,
        VolumeMute = 173,
        VolumeDown = 174,
        VolumeUp = 175,
        MediaNextTrack = 176,
        MediaPreviousTrack = 177,
        MediaStop = 178,
        MediaPlayPause = 179,
        LaunchMail = 180,
        SelectMedia = 181,
        LaunchApplication1 = 182,
        LaunchApplication2 = 183,
        Oem1 = 186,
        OemSemicolon = 186,
        Oemplus = 187,
        Oemcomma = 188,
        OemMinus = 189,
        OemPeriod = 190,
        Oem2 = 191,
        OemQuestion = 191,
        Oem3 = 192,
        Oemtilde = 192,
        Oem4 = 219,
        OemOpenBrackets = 219,
        Oem5 = 220,
        OemPipe = 220,
        Oem6 = 221,
        OemCloseBrackets = 221,
        Oem7 = 222,
        OemQuotes = 222,
        Oem8 = 223,
        Oem102 = 226,
        OemBackslash = 226,
        ProcessKey = 229,
        Packet = 231,
        Attn = 246,
        Crsel = 247,
        Exsel = 248,
        EraseEof = 249,
        Play = 250,
        Zoom = 251,
        NoName = 252,
        Pa1 = 253,
        OemClear = 254,
        Shift = 65536,
        Control = 131072,
        Alt = 262144,
    }

	public partial struct WM
	{
		internal const int Hotkey = 0x312;
		internal const int SetHotkey = 0x0032;
		internal const int GetHotkey = 0x33;
	}

	public partial class NativeMethods
	{
		/// <summary>
		/// The RegisterHotKey function defines a system-wide hot key
		/// </summary>
		/// <param name="hWnd">Handle to the window that will receive WM_HOTKEY messages generated by the hot key. If this parameter is NULL, WM_HOTKEY messages are posted to the message queue of the calling thread and must be processed in the message loop.</param>
		/// <param name="id">Specifies the identifier of the hot key. No other hot key in the calling thread should have the same identifier. An application must specify a value in the range 0x0000 through 0xBFFF. A shared dynamic-link library (DLL) must specify a value in the range 0xC000 through 0xFFFF (the range returned by the GlobalAddAtom function). To avoid conflicts with hot-key identifiers defined by other shared DLLs, a DLL should use the GlobalAddAtom function to obtain the hot-key identifier.</param>
		/// <param name="fsModifiers">Specifies keys that must be pressed in combination with the key specified by the uVirtKey parameter in order to generate the WM_HOTKEY message.</param>
		/// <param name="uVirtKey">Specifies the virtual-key code of the hot key.</param>
		/// <returns>If the function succeeds, the return value is nonzero.</returns>
		[DllImport("User32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterHotKey(IntPtr hWnd, int id, Modifiers fsModifiers, Keys uVirtKey);
		/// <summary>
		/// The UnregisterHotKey function frees a hot key previously registered by the calling thread.
		/// </summary>
		/// <param name="hWnd">Handle to the window associated with the hot key to be freed. This parameter should be NULL if the hot key is not associated with a window.</param>
		/// <param name="id">Specifies the identifier of the hot key to be freed.</param>
		/// <returns>If the function succeeds, the return value is nonzero.</returns>
		[DllImport("User32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	}
}