using System;
using System.Drawing;

namespace GridLocked
{
    /// <summary>
    /// Bundles all lighting, wall texture, and environment settings for a specific zone/level.
    /// Can be loaded from dungeon.json or overridden per level.
    /// </summary>
    public sealed class ZoneConfig
    {
        // --- Lighting ---
        public bool EnableLighting { get; set; } = true;
        public bool EnableShadows { get; set; } = true;
        public float DarkMaxBrightness { get; set; } = 0.04f;
        public bool InvertColorsInDarkness { get; set; } = true;
        public float MaxViewDepth { get; set; } = 14.5f;
        public float MinViewDepth { get; set; } = 2.5f;
        public double SconceInnerRadius { get; set; } = 1.5;
        public double SconceOuterRadius { get; set; } = 5.5;
        public Color LanternColor { get; set; } = Color.FromArgb(255, 235, 195);
        public float AmbientLight { get; set; } = 0.0f;

        // --- Walls & Textures ---
        public string WallPreset { get; set; } = "brick";
        public int BrickWidth { get; set; } = 64;
        public int BrickHeight { get; set; } = 32;
        public int MortarThickness { get; set; } = 4;
        public Color BrickColor { get; set; } = Color.FromArgb(138, 138, 138);
        public Color MortarColor { get; set; } = Color.FromArgb(34, 34, 34);

        // --- Environment ---
        public Color FloorColor { get; set; } = Color.FromArgb(220, 216, 200);
        public Color CeilingColor { get; set; } = Color.FromArgb(90, 90, 90);
        public bool ShowFloorGrid { get; set; } = true;
        public Color FloorGridColor { get; set; } = Color.FromArgb(64, 64, 64);

        public void ApplyTo(dynamic canvas)
        {
            if (canvas == null) return;
            try
            {
                var lm = canvas.Lighting;
                if (lm != null)
                {
                    lm.EnableLighting = EnableLighting;
                    lm.EnableShadows = EnableShadows;
                    lm.DarkMaxBrightness = DarkMaxBrightness;
                    lm.InvertColorsInDarkness = InvertColorsInDarkness;
                    lm.MaxViewDepth = MaxViewDepth;
                    lm.MinViewDepth = MinViewDepth;
                    lm.AmbientLight = AmbientLight;
                    lm.ShowFloorTiles = ShowFloorGrid;
                    lm.FloorTileLineColor = FloorGridColor;
                }

                canvas.LightInnerRadius = SconceInnerRadius;
                canvas.LightOuterRadius = SconceOuterRadius;
                canvas.LanternColor = LanternColor;

                canvas.FloorColor = FloorColor;
                canvas.CeilingColor = CeilingColor;
            }
            catch { }
        }

        public static Color ParseColorHex(string hex, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(hex)) return defaultColor;
            hex = hex.Trim().TrimStart('#');
            if (hex.Length == 6)
            {
                if (int.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out int r) &&
                    int.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out int g) &&
                    int.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out int b))
                {
                    return Color.FromArgb(r, g, b);
                }
            }
            return defaultColor;
        }
    }
}
