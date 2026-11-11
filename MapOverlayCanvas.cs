using System;
using System.Drawing;
using System.Windows.Forms;

namespace GridLocked
{
    /// <summary>
    /// Full-screen (over the camera) overlay that draws a fog-of-war style map:
    /// - shows only cells that RaycasterCanvas has "seen"
    /// - draws walls (black), seen floor (light), player icon + facing
    /// Toggle Visible from Form1 with Tab.
    /// </summary>
    public sealed class MapOverlayCanvas : Panel
    {
        private readonly RaycasterCanvas _view;

        // Look/feel
        public int PaddingAroundMap = 24;
        public Color OverlayTint = Color.FromArgb(110, 0, 0, 0);         // darken background
        public Color FrameColor = Color.Black;
        public Color FloorSeen = Color.FromArgb(220, 220, 220);
        public Color WallSeen = Color.Black;
        public Color GridLine = Color.FromArgb(200, 200, 200);
        public bool ShowGrid = true;

        // Scaling: how big the map box is relative to this overlay
        public float MapBoxFracW = 0.80f;   // 80% of overlay width
        public float MapBoxFracH = 0.80f;   // 80% of overlay height

        public MapOverlayCanvas(RaycasterCanvas view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Visible = false; // start hidden; toggle via Tab
        }

        // In GridLocked/MapOverlayCanvas.cs, inside class MapOverlayCanvas
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            // Dim the scene underneath
            using (var tint = new SolidBrush(OverlayTint))
                g.FillRectangle(tint, ClientRectangle);

            // Determine map box
            int boxW = Math.Max(100, (int)(ClientSize.Width * MapBoxFracW));
            int boxH = Math.Max(100, (int)(ClientSize.Height * MapBoxFracH));
            int boxX = (ClientSize.Width - boxW) / 2;
            int boxY = (ClientSize.Height - boxH) / 2;
            var mapBox = new Rectangle(boxX, boxY, boxW, boxH);

            // Frame
            using (var frame = new Pen(FrameColor, 2f))
                g.DrawRectangle(frame, mapBox);

            // Get map & seen grid
            int mapW = _view.MapWidth;
            int mapH = _view.MapHeight;
            if (mapW <= 0 || mapH <= 0) return;

            bool[,] seen = _view.GetSeenSnapshot(); // safe clone for drawing

            // Compute cell size
            int innerX = mapBox.X + PaddingAroundMap;
            int innerY = mapBox.Y + PaddingAroundMap;
            int innerW = Math.Max(1, mapBox.Width - PaddingAroundMap * 2);
            int innerH = Math.Max(1, mapBox.Height - PaddingAroundMap * 2);

            // Keep cells square; fit to min dimension
            float cellW = innerW / (float)mapW;
            float cellH = innerH / (float)mapH;
            float cell = Math.Min(cellW, cellH);

            // Center the grid inside the map box
            int gridW = (int)Math.Round(cell * mapW);
            int gridH = (int)Math.Round(cell * mapH);
            int gx = innerX + (innerW - gridW) / 2;
            int gy = innerY + (innerH - gridH) / 2;

            // Draw seen floor/walls
            using (var wallB = new SolidBrush(WallSeen))
            {
                for (int y = 0; y < mapH; y++)
                    for (int x = 0; x < mapW; x++)
                    {
                        if (!seen[y, x]) continue;

                        float px = gx + x * cell;
                        float py = gy + y * cell;
                        var cellRect = new RectangleF(px, py, cell, cell);

                        if (_view.IsWallCell(x, y))
                        {
                            g.FillRectangle(wallB, cellRect);
                        }
                        else
                        {
                            float light = _view.Lighting.SampleLight(x + 0.5, y + 0.5, out float rT, out float gT, out float bT);
                            Color c = Color.FromArgb(_view.Lighting.ShadeArgb(FloorSeen, light, rT, gT, bT));
                            using (var floorB = new SolidBrush(c))
                                g.FillRectangle(floorB, cellRect);

                            // Draw Stairs or Object marker if discovered
                            if (_view.CurrentLevel != null && x < _view.CurrentLevel.Width && y < _view.CurrentLevel.Height)
                            {
                                var tile = _view.CurrentLevel.Tiles[y, x];
                                if (tile.Type == TileType.StairsUp)
                                {
                                    using (var font = new Font(FontFamily.GenericSansSerif, Math.Max(6f, cell * 0.45f), FontStyle.Bold))
                                    using (var textB = new SolidBrush(Color.FromArgb(20, 160, 40)))
                                        g.DrawString("S", font, textB, px + 1, py + 1);
                                }
                                else if (tile.Type == TileType.StairsDown)
                                {
                                    using (var font = new Font(FontFamily.GenericSansSerif, Math.Max(6f, cell * 0.45f), FontStyle.Bold))
                                    using (var textB = new SolidBrush(Color.FromArgb(30, 90, 200)))
                                        g.DrawString("s", font, textB, px + 1, py + 1);
                                }
                                else if (tile.CenterOccupant is LargeObjectInstance lo)
                                {
                                    float dotR = Math.Max(2f, cell * 0.25f);
                                    using (var dotB = new SolidBrush(Color.FromArgb(180, 140, 20)))
                                        g.FillEllipse(dotB, px + (cell - dotR) * 0.5f, py + (cell - dotR) * 0.5f, dotR, dotR);
                                }
                            }
                        }
                    }
            }

            // Draw Lanterns on map
            if (_view.Lighting != null && _view.Lighting.Lights.Count > 0)
            {
                using (var glowB = new SolidBrush(Color.FromArgb(140, 255, 200, 50)))
                using (var flameB = new SolidBrush(Color.FromArgb(255, 240, 100)))
                using (var flamePen = new Pen(Color.FromArgb(180, 80, 0), 1f))
                {
                    for (int i = 0; i < _view.Lighting.Lights.Count; i++)
                    {
                        var light = _view.Lighting.Lights[i];
                        int lx = (int)Math.Floor(light.X);
                        int ly = (int)Math.Floor(light.Y);
                        bool isSeen = (lx >= 0 && ly >= 0 && lx < mapW && ly < mapH && seen[ly, lx]);
                        if (!isSeen && lx > 0 && seen[ly, lx - 1]) isSeen = true;
                        if (!isSeen && lx + 1 < mapW && seen[ly, lx + 1]) isSeen = true;
                        if (!isSeen && ly > 0 && seen[ly - 1, lx]) isSeen = true;
                        if (!isSeen && ly + 1 < mapH && seen[ly + 1, lx]) isSeen = true;
                        if (!isSeen) continue;

                        float fx = gx + (float)light.X * cell;
                        float fy = gy + (float)light.Y * cell;
                        float r = Math.Max(2f, cell * 0.22f);
                        g.FillEllipse(glowB, fx - r * 1.5f, fy - r * 1.5f, r * 3f, r * 3f);
                        g.FillEllipse(flameB, fx - r, fy - r, r * 2f, r * 2f);
                        g.DrawEllipse(flamePen, fx - r, fy - r, r * 2f, r * 2f);
                    }
                }
            }

            // Optional grid overlay
            if (ShowGrid)
            {
                using (var gridPen = new Pen(GridLine, 1f))
                {
                    for (int x = 0; x <= mapW; x++)
                    {
                        float px = gx + x * cell;
                        g.DrawLine(gridPen, px, gy, px, gy + gridH);
                    }
                    for (int y = 0; y <= mapH; y++)
                    {
                        float py = gy + y * cell;
                        g.DrawLine(gridPen, gx, py, gx + gridW, py);
                    }
                }
            }

            // Player position & facing
            int pcx = (int)Math.Floor(_view.PlayerX);
            int pcy = (int)Math.Floor(_view.PlayerY);
            float cx = gx + (pcx + 0.5f) * cell;
            float cy = gy + (pcy + 0.5f) * cell;

            // Arrow triangle
            double dir = _view.PlayerAngle;

            float rad = cell * 0.45f;   // (renamed from r)
            float rad2 = rad * 0.55f;
            float rad3 = rad * 0.25f;

            var ux = (float)Math.Cos(dir);
            var uy = (float)Math.Sin(dir);
            var pxn = -uy; var pyn = ux;

            var tip = new PointF(cx + ux * rad, cy + uy * rad);
            var left = new PointF(cx + ux * (-rad2) + pxn * (-rad3),
                                   cy + uy * (-rad2) + pyn * (-rad3));
            var right = new PointF(cx + ux * (-rad2) + pxn * (+rad3),
                                   cy + uy * (-rad2) + pyn * (+rad3));

            using (var fill = new SolidBrush(Color.Red))
            using (var pen = new Pen(Color.Black, 1f))
            {
                g.FillPolygon(fill, new[] { tip, right, left });
                g.DrawPolygon(pen, new[] { tip, right, left });
            }
        }

    }
}
