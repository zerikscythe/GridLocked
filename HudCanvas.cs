using System;
using System.Drawing;
using System.Windows.Forms;

namespace GridLocked
{
    public sealed class HudCanvas : Panel
    {
        // ===== Public data you can update from Form1 =====
        public int HP { get; set; } = 85;
        public int HPMax { get; set; } = 100;
        public int MP { get; set; } = 40;
        public int MPMax { get; set; } = 60;

        // Compass angle (engine convention: 0=East, π/2=South, π=West, 3π/2=North)
        public double DirectionRadians { get; set; } = 0.0;

        // Look/feel
        public int HudPadding { get; set; } = 10;

        // Compass geometry
        public float CompassScaleK { get; set; } = 0.48f;   // outer circle scale
        public float ArrowLineFrac { get; set; } = 0.12f;
        public float ArrowHeadFrac { get; set; } = 0.26f;
        public float ArrowHeadWidthFrac { get; set; } = 0.70f;
        public float ArrowMarginFrac { get; set; } = 0.14f; // ring clearance

        public HudCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;

            Height = 120;              // fixed HUD height
            Anchor = AnchorStyles.None; // positioned by Form1.LayoutViewportWithAspect()
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var hud = ClientRectangle;

            // Panel background + subtle frame
            using (var bg = new SolidBrush(Color.FromArgb(236, 236, 236)))
            using (var border = new Pen(Color.Black, 2f))
            using (var darkLine = new Pen(Color.FromArgb(180, 180, 180), 1f))
            using (var lightLine = new Pen(Color.White, 1f))
            {
                g.FillRectangle(bg, hud);
                g.DrawRectangle(border, hud);

                g.DrawLine(lightLine, hud.Left + 1, hud.Top + 1, hud.Right - 2, hud.Top + 1);
                g.DrawLine(lightLine, hud.Left + 1, hud.Top + 1, hud.Left + 1, hud.Bottom - 2);
                g.DrawLine(darkLine, hud.Left + 1, hud.Bottom - 2, hud.Right - 2, hud.Bottom - 2);
                g.DrawLine(darkLine, hud.Right - 2, hud.Top + 1, hud.Right - 2, hud.Bottom - 2);
            }

            var inner = Rectangle.Inflate(hud, -HudPadding, -HudPadding);

            // Layout: [HP/MP on left] [compass on right]
            int compBoxSize = Math.Min(inner.Height, Math.Max(72, hud.Height - HudPadding * 2));
            var compassBox = new Rectangle(
                inner.Right - compBoxSize,
                inner.Top + (inner.Height - compBoxSize) / 2,
                compBoxSize, compBoxSize);

            // Left stack for stat boxes (two rows)
            int statColW = Math.Max(200, inner.Width / 4);
            var statCol = new Rectangle(inner.Left, inner.Top, statColW, inner.Height);

            int gapY = Math.Max(6, inner.Height / 12);
            int boxH = (inner.Height - gapY) / 2;

            var hpBox = new Rectangle(statCol.Left, statCol.Top, statCol.Width, boxH);
            var mpBox = new Rectangle(statCol.Left, statCol.Top + boxH + gapY, statCol.Width, boxH);

            DrawStatBox(g, hpBox, "HP", HP, HPMax);
            DrawStatBox(g, mpBox, "MP", MP, MPMax);

            DrawCompass(g, compassBox, DirectionRadians);
        }

        // === Draw a labeled stat box (no bar graph, just X/X) ===
        private void DrawStatBox(Graphics g, Rectangle box, string label, int value, int max)
        {
            value = Math.Max(0, value);
            max = Math.Max(1, max);

            using (var bg = new SolidBrush(Color.White))
            using (var border = new Pen(Color.Black, 2f))
            using (var light = new Pen(Color.White, 1f))
            using (var dark = new Pen(Color.FromArgb(180, 180, 180), 1f))
            {
                // Box
                g.FillRectangle(bg, box);
                g.DrawRectangle(border, box);

                // slight bevel
                g.DrawLine(light, box.Left + 1, box.Top + 1, box.Right - 2, box.Top + 1);
                g.DrawLine(light, box.Left + 1, box.Top + 1, box.Left + 1, box.Bottom - 2);
                g.DrawLine(dark, box.Left + 1, box.Bottom - 2, box.Right - 2, box.Bottom - 2);
                g.DrawLine(dark, box.Right - 2, box.Top + 1, box.Right - 2, box.Bottom - 2);
            }

            // Text layout inside box
            var inner = Rectangle.Inflate(box, -10, -8);

            // Label on the left
            using (var fontLabel = new Font(FontFamily.GenericSansSerif, Math.Max(9, box.Height * 0.30f), FontStyle.Bold))
            using (var b = new SolidBrush(Color.Black))
            {
                var size = TextRenderer.MeasureText(label, fontLabel);
                var pos = new Point(inner.Left, inner.Top + (inner.Height - size.Height) / 2);
                g.DrawString(label, fontLabel, b, pos);
            }

            // Value "X / X" on the right, bigger
            string text = $"{value} / {max}";
            using (var fontVal = new Font(FontFamily.GenericSansSerif, Math.Max(10, box.Height * 0.38f), FontStyle.Bold))
            using (var b = new SolidBrush(Color.Black))
            {
                var size = TextRenderer.MeasureText(text, fontVal);
                var pos = new Point(inner.Right - size.Width, inner.Top + (inner.Height - size.Height) / 2);
                g.DrawString(text, fontVal, b, pos);
            }
        }

        // === Compass (round with snapped NSEW arrow) ===
        private void DrawCompass(Graphics g, Rectangle box, double dir)
        {
            float cx = box.Left + box.Width * 0.5f;
            float cy = box.Top + box.Height * 0.52f;
            float bezelR = Math.Min(box.Width, box.Height) * 0.5f * CompassScaleK;

            // N label
            using (var f = new Font(FontFamily.GenericSansSerif, Math.Max(8, box.Height * 0.14f), FontStyle.Bold))
            using (var b = new SolidBrush(Color.Black))
            {
                var sz = TextRenderer.MeasureText("N", f);
                g.DrawString("N", f, b, cx - sz.Width / 2f + 2f, box.Top);
            }

            // Outer ring
            using (var ring = new Pen(Color.Black, 1.5f))
                g.DrawEllipse(ring, cx - bezelR, cy - bezelR, bezelR * 2, bezelR * 2);

            // Snap to NSEW
            double q = Math.Round(dir / (Math.PI / 2.0)) * (Math.PI / 2.0);
            float ux = (float)Math.Cos(q), uy = (float)Math.Sin(q);
            float px = -uy, py = ux;

            // Arrow geometry
            float margin = Math.Max(0f, ArrowMarginFrac) * bezelR;
            float arrowR = Math.Max(4f, bezelR - margin);
            float lineW = Math.Max(2f, bezelR * ArrowLineFrac);
            float tailLen = arrowR * 0.50f;
            float headLen = arrowR * ArrowHeadFrac;
            float headW = headLen * ArrowHeadWidthFrac;

            var tipX = cx + ux * arrowR;
            var tipY = cy + uy * arrowR;
            var headBaseX = cx + ux * (arrowR - headLen);
            var headBaseY = cy + uy * (arrowR - headLen);
            var tailX = cx - ux * tailLen;
            var tailY = cy - uy * tailLen;

            // Shaft
            using (var pen = new Pen(Color.Black, lineW))
                g.DrawLine(pen, tailX, tailY, headBaseX, headBaseY);

            // Head
            float halfW = headW * 0.5f;
            var leftX = headBaseX + px * (-halfW);
            var leftY = headBaseY + py * (-halfW);
            var rightX = headBaseX + px * (halfW);
            var rightY = headBaseY + py * (halfW);

            using (var fill = new SolidBrush(Color.Black))
            using (var outline = new Pen(Color.Black, 1f))
            {
                var tri = new PointF[]
                {
                    new PointF(tipX, tipY),
                    new PointF(rightX, rightY),
                    new PointF(leftX,  leftY)
                };
                g.FillPolygon(fill, tri);
                g.DrawPolygon(outline, tri);
            }
        }
    }
}
