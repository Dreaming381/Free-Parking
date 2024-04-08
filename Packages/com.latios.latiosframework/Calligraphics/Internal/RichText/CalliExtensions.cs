using Unity.Collections;
using UnityEngine;

namespace Latios.Calligraphics
{
    internal static class CalligraphicsInternalExtensions
    {
        public static void GetSubString(this in CalliString calliString, ref FixedString128Bytes htmlTag, int startIndex, int length)
        {
            htmlTag.Clear();
            for (int i = startIndex, end = startIndex + length; i < end; i++)
                htmlTag.Append((char)calliString[i]);
        }
        public static bool Compare(this Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        public static bool IsAscii(this Unicode.Rune rune) => rune.value < 0x80;

        // Todo: Add support for other languages in a Burst-compatible way.
        public static Unicode.Rune ToLower(this Unicode.Rune rune)
        {
            if (rune.IsAscii())
                return new Unicode.Rune(rune.value + (((uint)(rune.value - 'A') <= ('Z' - 'A')) ? 0x20 : 0));
            return rune;
        }

        public static Unicode.Rune ToUpper(this Unicode.Rune rune)
        {
            if (rune.IsAscii())
                return new Unicode.Rune(rune.value - (((uint)(rune.value - 'a') <= ('z' - 'a')) ? 0x20 : 0));
            return rune;
        }
    }
}
