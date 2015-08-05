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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;

namespace PoshCode.Interop
{
    // TODO: [CLSCompliant(false)]
    internal static class NativeMethods
    {
        #region user32!RegisterHotKey
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int uVirtKey);
        #endregion

        #region user32!UnregisterHotKey
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion


        /// <summary>The set of valid MapTypes used in MapVirtualKey
        /// </summary>
        /// <remarks></remarks>
        public enum MapType : uint
        {
            /// <summary>uCode is a virtual-key code and is translated into a scan code.
            /// If it is a virtual-key code that does not distinguish between left- and
            /// right-hand keys, the left-hand scan code is returned.
            /// If there is no translation, the function returns 0.
            /// </summary>
            /// <remarks></remarks>
            MAPVK_VK_TO_VSC = 0x0,

            /// <summary>uCode is a scan code and is translated into a virtual-key code that
            /// does not distinguish between left- and right-hand keys. If there is no
            /// translation, the function returns 0.
            /// </summary>
            /// <remarks></remarks>
            MAPVK_VSC_TO_VK = 0x1,

            /// <summary>uCode is a virtual-key code and is translated into an unshifted
            /// character value in the low-order word of the return value. Dead keys (diacritics)
            /// are indicated by setting the top bit of the return value. If there is no
            /// translation, the function returns 0.
            /// </summary>
            /// <remarks></remarks>
            MAPVK_VK_TO_CHAR = 0x2,

            /// <summary>Windows NT/2000/XP: uCode is a scan code and is translated into a
            /// virtual-key code that distinguishes between left- and right-hand keys. If
            /// there is no translation, the function returns 0.
            /// </summary>
            /// <remarks></remarks>
            MAPVK_VSC_TO_VK_EX = 0x3,

            /// <summary>Not currently documented
            /// </summary>
            /// <remarks></remarks>
            MAPVK_VK_TO_VSC_EX = 0x4
        }

        /// <summary>The ToUnicode function translates the specified virtual-key code and keyboard state 
        /// to the corresponding Unicode character or characters. To specify a handle to the keyboard layout 
        /// to use to translate the specified code, use the ToUnicodeEx function.
        /// </summary>
        /// <param name="wVirtKey">Specifies the virtual-key code to be translated.</param>
        /// <param name="wScanCode">Specifies the hardware scan code of the key to be translated. 
        /// The high-order bit of this value is set if the key is up.</param>
        /// <param name="lpKeyState">Pointer to a 256-byte array that contains the current keyboard state.
        ///     Each element (byte) in the array contains the state of one key. 
        ///     If the high-order bit of a byte is set, the key is down.</param>
        /// <param name="pwszBuff">Pointer to the buffer that receives the translated Unicode character 
        ///     or characters. However, this buffer may be returned without being null-terminated 
        ///     even though the variable name suggests that it is null-terminated.
        /// </param>
        /// <param name="cchBuff">Specifies the size, in wide characters, of the buffer pointed to by the pwszBuff parameter.</param>
        /// <param name="wFlags">Specifies the behavior of the function. If bit 0 is set, a menu is active. Bits 1 through 31 are reserved.</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        internal static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] 
          StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int ToAsciiEx(uint uVirtKey, uint uScanCode, byte[] lpKeyState, [Out] StringBuilder lpChar, uint uFlags, IntPtr hkl);

        /// <summary> The ToAscii function translates the specified virtual-key code and keyboard state 
        /// to the corresponding character or characters. The function translates the code using 
        /// the input language and physical keyboard layout identified by the keyboard layout handle.
        /// </summary>
        /// <param name="uVirtKey">Specifies the virtual-key code to be translated.</param>
        /// <param name="uScanCode">Specifies the hardware scan code of the key to be translated. 
        /// The high-order bit of this value is set if the key is up (not pressed). </param>
        /// <param name="lpKeyState">Pointer to a 256-byte array that contains the current keyboard state. 
        /// Each element (byte) in the array contains the state of one key. If the high-order bit of a byte is set, 
        /// the key is down (pressed). The low bit, if set, indicates that the key is toggled on. In this function, 
        /// only the toggle bit of the CAPS LOCK key is relevant. The toggle state of the NUM LOCK and SCROLL LOCK keys is ignored.</param>
        /// <param name="lpChar">Pointer to the buffer that receives the translated character or characters.</param>
        /// <param name="uFlags">Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise.</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int ToAscii(uint uVirtKey, uint uScanCode, byte[] lpKeyState, [Out] StringBuilder lpChar, uint uFlags);


    }
}