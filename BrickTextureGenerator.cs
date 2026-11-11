// GridLocked/BrickTextureGenerator.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GridLocked
{
    public static class BrickTextureGenerator
    {
        /// <summary>
        /// Generate a square, black/white brick tile bitmap (white bricks, black mortar).
        /// No bounding box is drawn; use your wall renderer to outline the overall wall.
        /// </summary>
        /// <param name="size">Square size in pixels (min 4).</param>
        /// <param name="bricksX">Number of bricks per row.</param>
        /// <param name="bricksY">Number of brick rows.</param>
        /// <param name="mortarFracX">Mortar thickness as fraction of a brick width (0..1).</param>
        /// <param name="mortarFracY">Mortar thickness as fraction of a brick height (0..1).</param>
        /// <param name="uOffset">Horizontal phase offset 0..1 (e.g., 0.5 for half-brick shift).</param>
        /// <param name="staggerRows">If true, odd rows are shifted by half a brick.</param>
        /// <param name="includeOuterMortar">
        /// If true, draw mortar along the tile’s outermost edges.
        /// If false (default), avoid painting mortar exactly on the image borders to prevent visible tile seams.
        /// </param>
        public static Bitmap GenerateBrickSquare(
            int size,
            int bricksX,
            int bricksY,
            double mortarFracX,
            double mortarFracY,
            double uOffset = 0.0,     // 0..1, e.g., 0.5 for half-brick
            bool staggerRows = true,
            bool includeOuterMortar = false)
        {
            if (size < 4) size = 4;
            bricksX = Math.Max(1, bricksX);
            bricksY = Math.Max(1, bricksY);
            mortarFracX = Clamp01(mortarFracX);
            mortarFracY = Clamp01(mortarFracY);
            uOffset = Wrap01(uOffset);

            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                // Crisp 1px lines
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                // Background = brick fill (white)
                g.Clear(Color.White);

                // Brick cell sizes (in pixels)
                double brickW = size / (double)bricksX;
                double brickH = size / (double)bricksY;

                // Mortar thickness in pixels (ensure at least 1px to stay visible)
                double mx = Math.Max(1.0, mortarFracX * brickW);
                double my = Math.Max(1.0, mortarFracY * brickH);

                using (var mortar = new SolidBrush(Color.Black))
                {
                    // -----------------------------
                    // Horizontal mortar (between rows)
                    // -----------------------------
                    int rStart = includeOuterMortar ? 0 : 1;
                    int rEnd = includeOuterMortar ? bricksY : bricksY - 1;

                    for (int r = rStart; r <= rEnd; r++)
                    {
                        double y = r * brickH - (my / 2.0);
                        FillClamped(g, mortar, 0, y, size, my, size, size);
                    }

                    // -----------------------------
                    // Vertical mortar (columns with per-row phase + uOffset)
                    // -----------------------------
                    for (int r = 0; r < bricksY; r++)
                    {
                        // Row stagger (0 or 0.5) + user offset
                        double rowPhase = (staggerRows && (r % 2 == 1)) ? 0.5 : 0.0;
                        double phase = Wrap01(rowPhase + uOffset);

                        double yTop = r * brickH;
                        double yBot = (r + 1) * brickH;

                        double startX = (phase * brickW);

                        // Determine column indices that span the tile; add a small safety margin
                        int extra = 2;
                        int firstCol = (int)Math.Floor((-startX) / brickW) - extra;
                        int lastCol = (int)Math.Ceiling((size - startX) / brickW) + extra;

                        for (int c = firstCol; c <= lastCol; c++)
                        {
                            double xCenter = startX + c * brickW;
                            double x = xCenter - (mx / 2.0);

                            // If we don't want outer mortar, skip if this line would lie exactly on the edges
                            if (!includeOuterMortar)
                            {
                                // Treat a line as "outer" if its center is within half a mortar of the border
                                if (xCenter <= mx * 0.5 || xCenter >= size - mx * 0.5)
                                    continue;
                            }

                            FillClamped(g, mortar, x, yTop, mx, (yBot - yTop), size, size);
                        }
                    }
                }
            }

            return bmp;
        }

        private static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);

        private static double Wrap01(double x)
        {
            x = x - Math.Floor(x);
            return x < 0 ? x + 1 : x;
        }

        private static void FillClamped(Graphics g, Brush b, double x, double y, double w, double h, int maxW, int maxH)
        {
            int ix = (int)Math.Round(x);
            int iy = (int)Math.Round(y);
            int iw = (int)Math.Round(w);
            int ih = (int)Math.Round(h);

            if (ix < 0) { iw += ix; ix = 0; }
            if (iy < 0) { ih += iy; iy = 0; }
            if (ix + iw > maxW) iw = maxW - ix;
            if (iy + ih > maxH) ih = maxH - iy;
            if (iw <= 0 || ih <= 0) return;

            g.FillRectangle(b, ix, iy, iw, ih);
        }
    }
}
