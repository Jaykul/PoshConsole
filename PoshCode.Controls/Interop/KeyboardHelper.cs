// Copyright (c) 2008 Joel Bennett http://HuddledMasses.org/

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
using System.Management.Automation.Host;
using System.Text;
using System.Windows.Input;

namespace PoshCode.Interop
{
    /// <summary>
    /// Extension methods for working with Keyboards and KeyEvents
    /// </summary>
    public static class KeyboardHelper
    {
        /// <summary>Create a KeyInfo from a <see cref="KeyEventArgs"/>
        /// </summary>
        /// <param name="e">The KeyEventArgs</param>
        /// <returns>A KeyInfo struct</returns>
        [CLSCompliant(false)]
        public static KeyInfo ToKeyInfo(this KeyEventArgs e)
        {
            int vk = KeyInterop.VirtualKeyFromKey(e.Key);
            char c = GetChar(vk);
            return new KeyInfo(vk, c, e.KeyboardDevice.GetControlKeyStates(), e.IsDown);
        }

        /// <summary>Get the char from a VirtualKey -- MUST be called within the keyboard event handler.
        /// </summary>
        /// <param name="vk">VirtualKey code</param>
        /// <returns>the character represented by the VirtualKey</returns>
        private static char GetChar(int vk)
        {
            var ks = new byte[256];
            NativeMethods.GetKeyboardState(ks);

            var sc = NativeMethods.MapVirtualKey((uint)vk, NativeMethods.MapType.MAPVK_VK_TO_VSC);
            var sb = new StringBuilder(2);
            var ch = (char)0;

            switch (NativeMethods.ToUnicode((uint)vk, sc, ks, sb, sb.Capacity, 0))
            {
                case -1: break;
                case 0: break;
                case 1:
                    {
                        ch = sb[0];
                        break;
                    }
                default:
                    {
                        ch = sb[0];
                        break;
                    }
            }
            return ch;
        }

        /// <summary>
        /// Gets the control key states.
        /// </summary>
        /// <param name="kb">The <see cref="KeyboardDevice"/>.</param>
        /// <returns>The <see cref="ControlKeyStates"/> Flags</returns>
        private static ControlKeyStates GetControlKeyStates(this KeyboardDevice kb)
        {
            ControlKeyStates controlStates = default(ControlKeyStates);

            if (kb.IsKeyDown(Key.LeftCtrl))
            {
                controlStates |= ControlKeyStates.LeftCtrlPressed;
            }
            if (kb.IsKeyDown(Key.LeftAlt))
            {
                controlStates |= ControlKeyStates.LeftAltPressed;
            }
            if (kb.IsKeyDown(Key.RightAlt))
            {
                controlStates |= ControlKeyStates.RightAltPressed;
            }
            if (kb.IsKeyDown(Key.RightCtrl))
            {
                controlStates |= ControlKeyStates.RightCtrlPressed;
            }
            if (kb.IsKeyToggled(Key.Scroll))
            {
                controlStates |= ControlKeyStates.ScrollLockOn;
            }
            if (kb.IsKeyToggled(Key.CapsLock))
            {
                controlStates |= ControlKeyStates.CapsLockOn;
            }
            if (kb.IsKeyToggled(Key.NumLock))
            {
                controlStates |= ControlKeyStates.NumLockOn;
            }
            if (kb.IsKeyDown(Key.LeftShift) || kb.IsKeyDown(Key.RightShift))
            {
                controlStates |= ControlKeyStates.ShiftPressed;
            }
            return controlStates;
        }


        #region We didn't end up needing these, but I'm saving them anyway
        //private static int VK_CAPITAL = 0x14;
        //private static byte TOGGLED = 0x01;
        //private static byte PRESSED = 0x80;
        // System.Diagnostics.Trace.WriteLine(string.Format("CapsLock: {0}",((ks[VK_CAPITAL] & TOGGLED)>0)));


        //// These overloads are only PARTIALLY correct
        //public static KeyInfo ToKeyInfo(this Key k)
        //{
        //   var xConverter = new KeyConverter();
        //   char xChar = xConverter.ConvertToString(k)[0];
        //   return new KeyInfo(KeyInterop.VirtualKeyFromKey(k), xChar, ControlKeyStates.NumLockOn, false);
        //}

        //public static KeyInfo ToKeyInfo(this char c)
        //{
        //   var kConverter = new KeyConverter();
        //   Key xKey = (Key)kConverter.ConvertFromString(new string(c, 1));
        //   return new KeyInfo(KeyInterop.VirtualKeyFromKey(xKey), c, ControlKeyStates.NumLockOn, false);
        //}

        ///// <summary>
        ///// Create a KeyInfo from a character, specifying whether it's up or down
        ///// </summary>
        ///// <param name="c">Alphanumeric characters only...</param>
        ///// <param name="keyDown">True for key down</param>
        ///// <returns>A KeyInfo object</returns>
        //public static KeyInfo ToKeyInfo(this char c, bool keyDown)
        //{
        //   var kConverter = new KeyConverter();
        //   Key xKey = (Key)kConverter.ConvertFromString(new string(c, 1));
        //   return new KeyInfo(KeyInterop.VirtualKeyFromKey(xKey), c, ControlKeyStates.NumLockOn, keyDown);
        //}
        #endregion We didn't end up needing these, but I'm saving them anyway

    }
}