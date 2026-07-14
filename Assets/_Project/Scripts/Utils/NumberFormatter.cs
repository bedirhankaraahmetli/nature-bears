using System;
using System.Globalization;

namespace NatureBears.Utils
{
    /// <summary>
    /// Shared display formatter for economy numbers (CLAUDE.md rule 3):
    /// 1.2K / 3.4M / 5.6B ... Callers should only invoke on value CHANGE —
    /// each call allocates the returned string.
    /// </summary>
    public static class NumberFormatter
    {
        // Immutable — safe as a plain static readonly (no domain-reload reset needed).
        private static readonly string[] Suffixes =
            { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No" };

        public static string Format(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return "∞";
            if (value < 0) return "-" + Format(-value);

            if (value < 1000)
                return value.ToString("0", CultureInfo.InvariantCulture);

            int tier = (int)(Math.Log10(value) / 3);

            // Beyond the last named suffix: fall back to scientific notation.
            if (tier >= Suffixes.Length)
                return value.ToString("0.#e+0", CultureInfo.InvariantCulture);

            double mantissa = value / Math.Pow(10, tier * 3);

            // Rounding can tip the mantissa to 1000 (e.g. 999_950 → "1000K"); bump the tier.
            if (mantissa >= 999.95 && tier + 1 < Suffixes.Length)
            {
                tier++;
                mantissa /= 1000.0;
            }

            return mantissa.ToString("0.#", CultureInfo.InvariantCulture) + Suffixes[tier];
        }
    }
}
