using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Management.Automation.Host;
using PoshConsole.Interop;

namespace PoshConsole.Controls
{
    public class ReplayableKeyEventArgs 
    {

        public ReplayableKeyEventArgs() { }
        public ReplayableKeyEventArgs(KeyEventArgs e)
        {
            this.KeyEventArgs = e;
            IsDown = e.IsDown;
        }
        public ReplayableKeyEventArgs(KeyEventArgs e, ControlKeyStates s)
        {
            this.KeyEventArgs = e;
            this.ControlKeyStates = s;
            IsDown = e.IsDown;
        }

        public ControlKeyStates ControlKeyStates { get; set; }
        public KeyEventArgs KeyEventArgs { get; set; }
        public TextCompositionEventArgs TextCompositionEventArgs { get; set; }
        public bool IsDown { get; set; }

        KeyInfo? _keyInfo = null;
        public KeyInfo KeyInfo {
            get{
                if (!_keyInfo.HasValue)
                {
                    _keyInfo = GetKeyInfo(this.KeyEventArgs, this.ControlKeyStates);
                }
                return _keyInfo.Value;
            }
        }

        /// <summary>
        /// Get the character, or an empty string
        /// </summary>
        public string CharacterOrEmpty
        {
            get
            {
                if (this.KeyInfo.Character != (char)0)
                {
                    return this.KeyInfo.Character.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Test if the key event generates a character
        /// </summary>
        /// <returns>True if the character is not null</returns>
        public bool IsCharacter()
        {
            return ((int)this.KeyInfo.Character != 0);
        }


        public static KeyInfo GetKeyInfo(ReplayableKeyEventArgs e)
        {
            return GetKeyInfo(e.KeyEventArgs, e.ControlKeyStates);
        }

        public static KeyInfo GetKeyInfo(KeyEventArgs e, ControlKeyStates controlStates)
        {
            byte[] ks = new byte[256];
            NativeMethods.GetKeyboardState(ks);

            KeyConverter kc = new KeyConverter();
            int vk = KeyInterop.VirtualKeyFromKey(e.Key);
            uint sc = NativeMethods.MapVirtualKey((uint)vk, NativeMethods.MapType.MAPVK_VK_TO_VSC);

            System.Text.StringBuilder bs = new System.Text.StringBuilder(2);
            char c = (char)0;
            switch (PoshConsole.Interop.NativeMethods.ToUnicode((uint)vk, sc, ks, bs, bs.Capacity, (uint)(e.KeyboardDevice.IsKeyToggled(Key.CapsLock) ? 1 : 0)))
            {
                case -1: break;
                case 0: break;
                case 1:
                    {
                        c = bs[0];
                        break;
                    }
                default:
                    {
                        c = bs[0];
                        break;
                    }
            }

            return new KeyInfo(vk, c, controlStates, e.KeyboardDevice.IsKeyDown(e.Key));
        }
    }
}
