using System;
using System.Collections.Generic;
using System.Drawing;

namespace GridLocked
{
    public enum VectorShapeType
    {
        Rect,
        Ellipse,
        Line,
        Polygon
    }

    /// <summary>
    /// Represents a single vector drawing primitive defined in normalized [0.0, 1.0] coordinates.
    /// Scaled and dynamically shaded at runtime based on lighting and distance.
    /// </summary>
    public sealed class VectorShape
    {
        public VectorShapeType Type { get; set; } = VectorShapeType.Rect;
        public float X { get; set; }
        public float Y { get; set; }
        public float W { get; set; }
        public float H { get; set; }

        public PointF[] Points { get; set; }

        public Color FillColor { get; set; } = Color.Empty;
        public Color StrokeColor { get; set; } = Color.Empty;
        public float StrokeWidth { get; set; } = 1.5f;

        public void Draw(Graphics g, float sx, float sy, float sw, float sh, Func<Color, Color> colorShade = null)
        {
            float px = sx + X * sw;
            float py = sy + Y * sh;
            float pw = W * sw;
            float ph = H * sh;

            Brush fillBrush = null;
            if (FillColor != Color.Empty && FillColor.A > 0)
            {
                Color c = colorShade != null ? colorShade(FillColor) : FillColor;
                fillBrush = new SolidBrush(c);
            }

            Pen strokePen = null;
            if (StrokeColor != Color.Empty && StrokeColor.A > 0)
            {
                Color c = colorShade != null ? colorShade(StrokeColor) : StrokeColor;
                strokePen = new Pen(c, Math.Max(1f, StrokeWidth * (sw / 64f)));
            }

            try
            {
                switch (Type)
                {
                    case VectorShapeType.Rect:
                        if (fillBrush != null) g.FillRectangle(fillBrush, px, py, pw, ph);
                        if (strokePen != null) g.DrawRectangle(strokePen, px, py, pw, ph);
                        break;

                    case VectorShapeType.Ellipse:
                        if (fillBrush != null) g.FillEllipse(fillBrush, px, py, pw, ph);
                        if (strokePen != null) g.DrawEllipse(strokePen, px, py, pw, ph);
                        break;

                    case VectorShapeType.Line:
                        if (strokePen != null)
                            g.DrawLine(strokePen, px, py, px + pw, py + ph);
                        break;

                    case VectorShapeType.Polygon:
                        if (Points != null && Points.Length >= 3)
                        {
                            var scaledPts = new PointF[Points.Length];
                            for (int i = 0; i < Points.Length; i++)
                                scaledPts[i] = new PointF(sx + Points[i].X * sw, sy + Points[i].Y * sh);

                            if (fillBrush != null) g.FillPolygon(fillBrush, scaledPts);
                            if (strokePen != null) g.DrawPolygon(strokePen, scaledPts);
                        }
                        break;
                }
            }
            finally
            {
                fillBrush?.Dispose();
                strokePen?.Dispose();
            }
        }
    }

    /// <summary>
    /// Complete runtime vector sprite made of scalable geometric primitives.
    /// Loaded from JSON (e.g. Tree, Pillar, Altar, Fountain).
    /// </summary>
    public sealed class VectorSprite
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float BaseScale { get; set; } = 1.0f;
        public List<VectorShape> Shapes { get; } = new List<VectorShape>();

        public void Draw(Graphics g, float sx, float sy, float sw, float sh, Func<Color, Color> colorShade = null)
        {
            for (int i = 0; i < Shapes.Count; i++)
            {
                Shapes[i].Draw(g, sx, sy, sw, sh, colorShade);
            }
        }

        public void Draw(Graphics g, float sx, float sy, float sw, float sh, float light, float rT, float gT, float bT, double depth)
        {
            Func<Color, Color> shader = baseColor =>
            {
                int r = (int)(baseColor.R * light * rT);
                int gc = (int)(baseColor.G * light * gT);
                int b = (int)(baseColor.B * light * bT);
                if (r < 0) r = 0; if (r > 255) r = 255;
                if (gc < 0) gc = 0; if (gc > 255) gc = 255;
                if (b < 0) b = 0; if (b > 255) b = 255;
                return Color.FromArgb(baseColor.A, r, gc, b);
            };
            Draw(g, sx, sy, sw, sh, shader);
        }
        public static VectorSprite CreateDefaultStairsUp()
        {
            var s = new VectorSprite { Id = "stairs_up", Name = "Stairs Up", BaseScale = 0.95f };
            // Step 1
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.12f, Y = 0.72f, W = 0.76f, H = 0.25f, FillColor = Color.FromArgb(140, 140, 145), StrokeColor = Color.FromArgb(40, 40, 40), StrokeWidth = 2f });
            // Step 2
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.18f, Y = 0.50f, W = 0.64f, H = 0.24f, FillColor = Color.FromArgb(165, 165, 170), StrokeColor = Color.FromArgb(40, 40, 40), StrokeWidth = 2f });
            // Step 3
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.24f, Y = 0.30f, W = 0.52f, H = 0.22f, FillColor = Color.FromArgb(190, 190, 195), StrokeColor = Color.FromArgb(40, 40, 40), StrokeWidth = 2f });
            // Step 4
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.30f, Y = 0.12f, W = 0.40f, H = 0.20f, FillColor = Color.FromArgb(215, 215, 220), StrokeColor = Color.FromArgb(40, 40, 40), StrokeWidth = 2f });
            // Ascending glow arrow
            s.Shapes.Add(new VectorShape
            {
                Type = VectorShapeType.Polygon,
                Points = new[] { new PointF(0.50f, 0.02f), new PointF(0.58f, 0.10f), new PointF(0.42f, 0.10f) },
                FillColor = Color.FromArgb(80, 200, 100),
                StrokeColor = Color.FromArgb(20, 80, 30),
                StrokeWidth = 1.5f
            });
            return s;
        }

        public static VectorSprite CreateDefaultStairsDown()
        {
            var s = new VectorSprite { Id = "stairs_down", Name = "Stairs Down", BaseScale = 0.95f };
            // Outer stairwell frame
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.15f, Y = 0.25f, W = 0.70f, H = 0.72f, FillColor = Color.FromArgb(20, 20, 25), StrokeColor = Color.FromArgb(50, 50, 50), StrokeWidth = 2f });
            // Step 1
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.20f, Y = 0.32f, W = 0.60f, H = 0.13f, FillColor = Color.FromArgb(130, 130, 135), StrokeColor = Color.FromArgb(30, 30, 30), StrokeWidth = 1.5f });
            // Step 2
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.24f, Y = 0.46f, W = 0.52f, H = 0.13f, FillColor = Color.FromArgb(95, 95, 100), StrokeColor = Color.FromArgb(25, 25, 25), StrokeWidth = 1.5f });
            // Step 3
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.28f, Y = 0.60f, W = 0.44f, H = 0.13f, FillColor = Color.FromArgb(60, 60, 65), StrokeColor = Color.FromArgb(20, 20, 20), StrokeWidth = 1.5f });
            // Deep pit void
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.32f, Y = 0.74f, W = 0.36f, H = 0.18f, FillColor = Color.FromArgb(8, 8, 10), StrokeColor = Color.Black, StrokeWidth = 1.5f });
            // Descending glow arrow
            s.Shapes.Add(new VectorShape
            {
                Type = VectorShapeType.Polygon,
                Points = new[] { new PointF(0.50f, 0.22f), new PointF(0.58f, 0.14f), new PointF(0.42f, 0.14f) },
                FillColor = Color.FromArgb(70, 130, 220),
                StrokeColor = Color.FromArgb(15, 40, 90),
                StrokeWidth = 1.5f
            });
            return s;
        }

        public static VectorSprite CreateDefaultDoorClosed()
        {
            var s = new VectorSprite { Id = "door_closed", Name = "Closed Door", BaseScale = 0.98f };
            // Outer stone archway frame
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.10f, Y = 0.04f, W = 0.80f, H = 0.94f, FillColor = Color.FromArgb(90, 88, 92), StrokeColor = Color.FromArgb(35, 35, 40), StrokeWidth = 2.5f });
            // Inner doorway cutout
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.18f, Y = 0.12f, W = 0.64f, H = 0.86f, FillColor = Color.FromArgb(20, 20, 24) });
            // Wooden door panel
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.20f, Y = 0.14f, W = 0.60f, H = 0.84f, FillColor = Color.FromArgb(135, 82, 45), StrokeColor = Color.FromArgb(40, 25, 15), StrokeWidth = 2f });
            // Vertical plank seams
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Line, X = 0.40f, Y = 0.14f, W = 0.0f, H = 0.84f, StrokeColor = Color.FromArgb(85, 50, 25), StrokeWidth = 1.5f });
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Line, X = 0.60f, Y = 0.14f, W = 0.0f, H = 0.84f, StrokeColor = Color.FromArgb(85, 50, 25), StrokeWidth = 1.5f });
            // Horizontal iron straps
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.18f, Y = 0.28f, W = 0.64f, H = 0.08f, FillColor = Color.FromArgb(50, 52, 58), StrokeColor = Color.FromArgb(20, 20, 22), StrokeWidth = 1.5f });
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.18f, Y = 0.74f, W = 0.64f, H = 0.08f, FillColor = Color.FromArgb(50, 52, 58), StrokeColor = Color.FromArgb(20, 20, 22), StrokeWidth = 1.5f });
            // Iron rivets
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.24f, Y = 0.30f, W = 0.04f, H = 0.04f, FillColor = Color.FromArgb(180, 180, 185) });
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.50f, Y = 0.30f, W = 0.04f, H = 0.04f, FillColor = Color.FromArgb(180, 180, 185) });
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.72f, Y = 0.30f, W = 0.04f, H = 0.04f, FillColor = Color.FromArgb(180, 180, 185) });
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.24f, Y = 0.76f, W = 0.04f, H = 0.04f, FillColor = Color.FromArgb(180, 180, 185) });
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.50f, Y = 0.76f, W = 0.04f, H = 0.04f, FillColor = Color.FromArgb(180, 180, 185) });
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.72f, Y = 0.76f, W = 0.04f, H = 0.04f, FillColor = Color.FromArgb(180, 180, 185) });
            // Iron pull ring handle
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.24f, Y = 0.50f, W = 0.08f, H = 0.10f, StrokeColor = Color.FromArgb(180, 180, 185), StrokeWidth = 2.0f });
            // Keyhole escutcheon
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.26f, Y = 0.62f, W = 0.04f, H = 0.06f, FillColor = Color.FromArgb(25, 25, 28), StrokeColor = Color.FromArgb(120, 110, 80), StrokeWidth = 1f });
            return s;
        }

        public static VectorSprite CreateDefaultDoorOpen()
        {
            var s = new VectorSprite { Id = "door_open", Name = "Open Door", BaseScale = 0.98f };
            // Outer stone archway frame
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.10f, Y = 0.04f, W = 0.80f, H = 0.94f, FillColor = Color.FromArgb(90, 88, 92), StrokeColor = Color.FromArgb(35, 35, 40), StrokeWidth = 2.5f });
            // Open passage void
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.18f, Y = 0.12f, W = 0.64f, H = 0.86f, FillColor = Color.FromArgb(15, 15, 20), StrokeColor = Color.FromArgb(10, 10, 12), StrokeWidth = 1.5f });
            // Door swung open to side in perspective
            s.Shapes.Add(new VectorShape
            {
                Type = VectorShapeType.Polygon,
                Points = new[] { new PointF(0.18f, 0.12f), new PointF(0.32f, 0.20f), new PointF(0.32f, 0.90f), new PointF(0.18f, 0.98f) },
                FillColor = Color.FromArgb(115, 68, 35),
                StrokeColor = Color.FromArgb(35, 20, 10),
                StrokeWidth = 2f
            });
            // Floor threshold
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.18f, Y = 0.94f, W = 0.64f, H = 0.04f, FillColor = Color.FromArgb(130, 128, 132), StrokeColor = Color.FromArgb(40, 40, 45), StrokeWidth = 1f });
            return s;
        }

        public static VectorSprite CreateDefaultPit()
        {
            var s = new VectorSprite { Id = "pit", Name = "Pit", BaseScale = 0.95f };
            // Outer rim
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.12f, Y = 0.35f, W = 0.76f, H = 0.60f, FillColor = Color.FromArgb(70, 68, 72), StrokeColor = Color.FromArgb(30, 30, 32), StrokeWidth = 2f });
            // Abyss void
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Rect, X = 0.18f, Y = 0.42f, W = 0.64f, H = 0.48f, FillColor = Color.FromArgb(5, 5, 8), StrokeColor = Color.Black, StrokeWidth = 1.5f });
            // Warning marker
            s.Shapes.Add(new VectorShape
            {
                Type = VectorShapeType.Polygon,
                Points = new[] { new PointF(0.50f, 0.78f), new PointF(0.42f, 0.58f), new PointF(0.58f, 0.58f) },
                FillColor = Color.FromArgb(220, 60, 40),
                StrokeColor = Color.FromArgb(100, 20, 15),
                StrokeWidth = 1.5f
            });
            return s;
        }

        public static VectorSprite CreateDefaultPortal()
        {
            var s = new VectorSprite { Id = "portal", Name = "Dimensional Portal", BaseScale = 0.96f };
            // Outer glowing aura
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.15f, Y = 0.08f, W = 0.70f, H = 0.88f, FillColor = Color.FromArgb(40, 30, 90), StrokeColor = Color.FromArgb(80, 160, 255), StrokeWidth = 3f });
            // Middle swirl
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.24f, Y = 0.18f, W = 0.52f, H = 0.68f, FillColor = Color.FromArgb(65, 25, 125), StrokeColor = Color.FromArgb(180, 80, 255), StrokeWidth = 2.5f });
            // Inner vortex core
            s.Shapes.Add(new VectorShape { Type = VectorShapeType.Ellipse, X = 0.35f, Y = 0.32f, W = 0.30f, H = 0.40f, FillColor = Color.FromArgb(225, 240, 255), StrokeColor = Color.FromArgb(200, 225, 255), StrokeWidth = 1.5f });
            return s;
        }
    }

    /// <summary>
    /// Catalog of custom vector-drawn objects loaded from objects.json.
    /// </summary>
    public sealed class VectorSpriteRegistry
    {
        private readonly Dictionary<string, VectorSprite> _sprites =
            new Dictionary<string, VectorSprite>(StringComparer.OrdinalIgnoreCase);

        public void Register(VectorSprite sprite)
        {
            if (sprite != null && !string.IsNullOrEmpty(sprite.Id))
                _sprites[sprite.Id] = sprite;
        }

        public VectorSprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_sprites.TryGetValue(id, out var s)) return s;
            if (id.Equals("stairs_up", StringComparison.OrdinalIgnoreCase))
            {
                s = VectorSprite.CreateDefaultStairsUp();
                Register(s);
                return s;
            }
            if (id.Equals("stairs_down", StringComparison.OrdinalIgnoreCase))
            {
                s = VectorSprite.CreateDefaultStairsDown();
                Register(s);
                return s;
            }
            if (id.Equals("door", StringComparison.OrdinalIgnoreCase) || id.Equals("door_closed", StringComparison.OrdinalIgnoreCase))
            {
                s = VectorSprite.CreateDefaultDoorClosed();
                Register(s);
                return s;
            }
            if (id.Equals("door_open", StringComparison.OrdinalIgnoreCase))
            {
                s = VectorSprite.CreateDefaultDoorOpen();
                Register(s);
                return s;
            }
            if (id.Equals("pit", StringComparison.OrdinalIgnoreCase) || id.Equals("void", StringComparison.OrdinalIgnoreCase))
            {
                s = VectorSprite.CreateDefaultPit();
                Register(s);
                return s;
            }
            if (id.Equals("portal", StringComparison.OrdinalIgnoreCase))
            {
                s = VectorSprite.CreateDefaultPortal();
                Register(s);
                return s;
            }
            return null;
        }

        public bool Contains(string id) => !string.IsNullOrEmpty(id) && (_sprites.ContainsKey(id) ||
            id.Equals("stairs_up", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("stairs_down", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("door", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("door_closed", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("door_open", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("pit", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("void", StringComparison.OrdinalIgnoreCase) ||
            id.Equals("portal", StringComparison.OrdinalIgnoreCase));
    }
}
