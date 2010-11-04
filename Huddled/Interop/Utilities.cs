/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.
using System;
using System.Windows;

namespace Huddled.Interop
{
    internal static class Utility
    {
       public static Thickness Union(this Thickness margin, Thickness padding)
       {
          return new Thickness(margin.Left + padding.Left,
                               margin.Top + padding.Top,
                               margin.Right + padding.Right,
                               margin.Bottom + padding.Bottom);
       }

       public static Thickness Clone(this Thickness margin, double? left = null, double? top = null, double? right = null, double? bottom = null)
       {
          left = left ?? margin.Left;
          top = top ?? margin.Top;
          right = right ?? margin.Right;
          bottom = bottom ?? margin.Bottom;

          return new Thickness(left.Value, top.Value, right.Value, bottom.Value);
       }

       

        public static int GET_X_LPARAM(this IntPtr lParam)
        {
            return LOWORD(lParam.ToInt32());
        }

        public static int GET_Y_LPARAM(this IntPtr lParam)
        {
            return HIWORD(lParam.ToInt32());
        }

        public static int HIWORD(this int i)
        {
            return (short)(i >> 16);
        }

        public static int LOWORD(this int i)
        {
            return (short)(i & 0xFFFF);
        }

        public static bool IsFlagSet(this int value, int mask)
        {
            return 0 != (value & mask);
        }
    }
}
