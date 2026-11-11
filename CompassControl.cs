using System;
using System.Drawing;
using System.Windows.Forms;

namespace GridLocked
{
    public sealed class CompassControl : Panel
    {
        /// <summary>0 = East, π/2 = South, π = West, 3π/2 = North (engine convention).</summary>
        public double DirectionRadians { get; set; } = 0.0;

        // Tweakables
        public float ScaleK { get; set; } = 0.48f;          // overall bezel size
        public float ArrowLineFrac { get; set; } = 0.12f;   // line thickness vs bezel radius
        public float ArrowHeadFrac { get; set; } = 0.26f;   // head length vs *arrow* radius
        public float ArrowHeadWidthFrac { get; set; } = 0.70f; // head width vs head length
        public float ArrowMarginFrac { get; set; } = 0.12f; // **padding** from bezel (0.12 = 12% of bezel radius)

        public CompassControl()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Size = new Size(90, 90);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            float cx = w * 0.5f, cy = h * 0.45f;

            // "N"
            using (var f = new Font(FontFamily.GenericSansSerif, Math.Max(8, h * 0.18f), FontStyle.Bold))
            using (var b = new SolidBrush(Color.Black))
            {
                const string north = "N";
                var sz = TextRenderer.MeasureText(north, f);
                g.DrawString(north, f, b, cx - sz.Width / 2f + 2f, 2f);
            }

            // Bezel radius
            float bezelR = Math.Min(w, h) * 0.5f * ScaleK;

            using (var ring = new Pen(Color.Black, 1.5f))
                g.DrawEllipse(ring, cx - bezelR, cy - bezelR, bezelR * 2, bezelR * 2);

            // Snap to NSEW
            double a = DirectionRadians;
            double q = Math.Round(a / (Math.PI / 2.0)) * (Math.PI / 2.0);

            // Unit vectors
            float ux = (float)Math.Cos(q);
            float uy = (float)Math.Sin(q);
            float px = -uy, py = ux;

            // Keep the arrow tip inside the ring by a margin
            float margin = Math.Max(0f, ArrowMarginFrac) * bezelR;
            float arrowR = Math.Max(4f, bezelR - margin); // effective radius for arrow tip

            // Arrow geometry (based on arrowR)
            float lineW = Math.Max(2f, bezelR * ArrowLineFrac);
            float tailLen = arrowR * 0.50f;
            float headLen = arrowR * ArrowHeadFrac;
            float headW = headLen * ArrowHeadWidthFrac;

            // Tail, head base, tip (all within arrowR)
            var tipX = cx + ux * arrowR;
            var tipY = cy + uy * arrowR;

            var headBaseX = cx + ux * (arrowR - headLen);
            var headBaseY = cy + uy * (arrowR - headLen);

            var tailX = cx - ux * tailLen;
            var tailY = cy - uy * tailLen;

            // Tail line
            using (var pen = new Pen(Color.Black, lineW))
                g.DrawLine(pen, tailX, tailY, headBaseX, headBaseY);

            // Arrow head
            var halfW = headW * 0.5f;
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
