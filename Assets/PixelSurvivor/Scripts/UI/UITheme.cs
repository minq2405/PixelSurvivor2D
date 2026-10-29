using UnityEngine;

namespace PixelSurvivor.UI
{
    /// <summary>
    /// Centralized UI Design System & Color Palette for Pixel Survivor.
    /// Theme: Tone 1 - Dark Obsidian & Gemstone (Finalized).
    /// </summary>
    public static class UITheme
    {
        // ==========================
        // BASE SURFACE & PANELS
        // ==========================
        /// <summary>Primary deep obsidian panel background (#0B0F19, 92% Alpha)</summary>
        public static readonly Color BackgroundPanel = new Color(0.043f, 0.059f, 0.098f, 0.92f);

        /// <summary>Card & slot surface background (#1E293B, 90% Alpha)</summary>
        public static readonly Color CardSurface = new Color(0.118f, 0.161f, 0.231f, 0.90f);

        /// <summary>Subdued separation border (#334155)</summary>
        public static readonly Color BorderSubtle = new Color(0.200f, 0.255f, 0.333f, 1f);

        /// <summary>Card hover / secondary highlight border (#38BDF8 - Diamond Cyan)</summary>
        public static readonly Color BorderHighlight = new Color(0.220f, 0.741f, 0.973f, 1f);

        /// <summary>Selected card / primary focus border (#FACC15 - Aurum Gold)</summary>
        public static readonly Color BorderSelected = new Color(0.980f, 0.800f, 0.082f, 1f);

        // ==========================
        // SEMANTIC GEMSTONE ACCENTS
        // ==========================
        /// <summary>Emerald Green: Currency, positive stat buffs, health recovery (#10B981)</summary>
        public static readonly Color EmeraldGreen = new Color(0.063f, 0.725f, 0.506f, 1f);

        /// <summary>Crimson Redstone: Player HP Bar, danger alerts, critical damage (#EF4444)</summary>
        public static readonly Color CrimsonRed = new Color(0.937f, 0.267f, 0.267f, 1f);

        /// <summary>Diamond / Lapis Cyan: EXP bar, cooldown indicator, mobile dash (#38BDF8)</summary>
        public static readonly Color DiamondCyan = new Color(0.220f, 0.741f, 0.973f, 1f);

        /// <summary>Aurum Gold: Player Level, Boss HP, Evolution tier, CTA Buttons (#FACC15)</summary>
        public static readonly Color AurumGold = new Color(0.980f, 0.800f, 0.082f, 1f);

        /// <summary>Void Purple: Ultimate abilities, curse mechanics (#A855F7)</summary>
        public static readonly Color VoidPurple = new Color(0.659f, 0.333f, 0.969f, 1f);

        // ==========================
        // TEXT STYLING
        // ==========================
        /// <summary>Crisp high-contrast main title / value text (#F8FAFC)</summary>
        public static readonly Color TextMain = new Color(0.973f, 0.980f, 0.988f, 1f);

        /// <summary>Subdued secondary / description text (#94A3B8)</summary>
        public static readonly Color TextMuted = new Color(0.580f, 0.639f, 0.722f, 1f);

        // ==========================
        // TEXTMESHPRO RICH TEXT HEX TAGS
        // ==========================
        public const string HexEmerald = "#10B981";
        public const string HexGold = "#FACC15";
        public const string HexCyan = "#38BDF8";
        public const string HexRed = "#EF4444";
        public const string HexPurple = "#A855F7";
        public const string HexMuted = "#94A3B8";
        public const string HexMain = "#F8FAFC";

        /// <summary>
        /// Formats a stat buff string with rich text color for preview.
        /// e.g. "100% ➔ 128% (+28%)"
        /// </summary>
        public static string FormatBuffText(string baseVal, string newVal, string diffVal)
        {
            return $"{baseVal} ➔ <color={HexEmerald}>{newVal} (+{diffVal}) 🟢</color>";
        }

        /// <summary>
        /// Returns border color for weapon vs passive slots.
        /// </summary>
        public static Color GetSlotColor(bool isWeapon, bool isEquipped)
        {
            if (!isEquipped) return BorderSubtle;
            return isWeapon ? AurumGold : EmeraldGreen;
        }
    }
}
