using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GridLocked
{
    public enum WallTexture
    {
        None,       // plain white walls (with optional wireframe outlines)
        BrickBW,    // staggered brick pattern (black mortar on white)
        GridBW,     // uniform grid (black lines on white)
        PlanksBW    // horizontal planks (black gaps on white)
    }

    public class RaycasterCanvas : UserControl
    {
        // --- Turn tween state ---
        private bool _isTurning;
        private double _turnStart;     // radians
        private double _turnTarget;    // radians (exact cardinal)
        private double _turnElapsed;   // seconds
        private double _turnDuration = .7; // 120ms feels snappy

        public bool IsTurning => _isTurning;
        // High-precision render loop
        private System.Diagnostics.Stopwatch _sw;
        private double _lastSec;


        // ===== Compass HUD =====
        public bool ShowCompass = true;
        public int CompassWidth = 84;
        public int CompassHeight = 64;
        public int CompassMargin = 10;  // distance from top edge
        public Color CompassBack = Color.FromArgb(220, 245, 245, 245); // slightly translucent light gray
        public Color CompassBorder = Color.FromArgb(90, 90, 90);
        public Color CompassArrow = Color.Black;
        public Color CompassText = Color.Black;
        public float CompassBorderWidth = 1.5f;

        // ===== HUD (drawn overlay) =====
        public bool ShowHud = true;
        public int HudHeightPx = 120;         // height of the bottom HUD bar
        public int HudPadding = 10;           // inner padding
        public int HudBevel = 6;              // small bevel radius

        // Simple stats you can tweak from Form1 later
        public int HP = 85, HPMax = 100;
        public int MP = 40, MPMax = 60;

        // Compass look (same geometry as your control, but drawn inside HUD)
        public float HudCompassScaleK = 0.48f;      // bezel size vs box
        public float HudCompassArrowLineFrac = 0.12f;
        public float HudCompassArrowHeadFrac = 0.26f;
        public float HudCompassArrowHeadWidthFrac = 0.70f;
        public float HudCompassArrowMarginFrac = 0.14f;  // keeps tip away from ring

        // How many tiles ahead the automap can reveal
        public int MapVisionTiles = 3;


        private int[,] _map = new int[,]
        {
            {1,1,1,1,1,1,1,1,1,1,1,1},
            {1,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,1,1,0,0,0,1,0,0,1},
            {1,0,0,1,0,0,0,0,1,0,0,1},
            {1,0,0,1,0,0,0,0,1,0,0,1},
            {1,0,0,0,0,0,1,0,0,0,0,1},
            {1,0,0,0,1,0,1,0,0,0,0,1},
            {1,0,0,0,1,0,0,0,0,1,0,1},
            {1,0,0,0,0,0,0,0,0,1,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,1},
            {1,0,0,0,0,0,0,0,0,0,0,1},
            {1,1,1,1,1,1,1,1,1,1,1,1},
        };

        // --- Grid step settings ---
        public double StepCooldownSeconds = .25;   // 1 sec between steps (tweak)
        private DateTime _nextMoveTime = DateTime.MinValue;

        // ===== Floor/Ceiling colors (tweak to taste) =====
        public Color CeilingColor = Color.FromArgb(190, 190, 190); // light gray ceiling in light
        public Color FloorColor = Color.FromArgb(220, 220, 220);   // bright stone floor
        // ===== Wall colors =====
        public Color WallFillColor = Color.FromArgb(200, 200, 200); // bright stone bricks
        public Color MortarLineColor = Color.Black;                // black mortar lines (inverts to 10% white in dark)
        public Color WireframeColor = Color.Black;                 // outline helpers (inverts to 10% white in dark)

        // Tile constants (if you haven't already)
        private const int TILE_FLOOR = 0;
        private const int TILE_WALL = 1;
        private const int TILE_SPAWN = 2;

        // --- Discrete facing to make controls stable ---
        public enum Cardinal { East = 0, South = 1, West = 2, North = 3 }
        private Cardinal _facing = Cardinal.East;

        // --- Simple per-step texture phase (0..1 wrap) ---
        public bool AnimateTextureOnStep = true;
        public double TexPhaseU = 0.0;          // horizontal phase (adds to 'u')
        public double TexPhaseV = 0.0;          // vertical phase (adds to 'v')
        public double PhaseStepU = 0.25;        // how much to shift each tile step (try 0.25 or 0.5)
        public double PhaseStepV = 0.00;        // usually 0; set small value if you want some vertical drift

        // Camera
        public double PlayerX = 3.5;
        public double PlayerY = 3.5;
        public double PlayerAngle = 0.0;

        // Settings
        public double FOV = 60.0 * Math.PI / 180.0;
        public double MoveSpeed = 3.0;
        public double TurnSpeed = 2.5;
        public double MaxRayDistance = 20.0;
        public int TileSize = 1;

        // ---- Auto FOV when close to a wall ----
        public bool AutoAdjustFovNearWall = true;
        public double FovDefaultRadians = 60.0 * Math.PI / 180.0;   // your normal FOV (synced from Form1)
        public double FovCloseRadians = 90.0 * Math.PI / 180.0;   // widened FOV when up-close
        public double CloseWallEnter = 0.60;  // tiles: enter widen if <= this distance
        public double CloseWallExit = 0.80;  // tiles: return to default if >= this distance
        private bool _fovWidened = false;
        public bool IsFovWidened => _fovWidened;                   // read-only for UI logic


        // --- Movement / collision ---
        public double PlayerRadius = 0.20;   // in map units (tile = 1.0). 0.20 ≈ 20% of a tile
        public bool EnableCollision = true;  // quick toggle

        // --- Rotation lock (old-school feel) ---
        public bool LockRotation = true;                 // if true, clamp around RotationCenter
        public double RotationCenter = 0.0;              // radians; set at spawn
        public double RotationLimit = Math.PI / 2.0;     // ±90° in radians

        // RaycasterCanvas fields (near your other settings)
        public double VerticalScale = 0.7; // 0.7–0.9 are good starting points

        // Modes
        // Lighting & Shadows
        public LightingManager Lighting { get; } = new LightingManager();
        public double LightInnerRadius
        {
            get => Lighting.Lights.Count > 0 ? Lighting.Lights[0].InnerRadius : 1.5;
            set
            {
                foreach (var l in Lighting.Lights) l.InnerRadius = value;
            }
        }
        public double LightOuterRadius
        {
            get => Lighting.Lights.Count > 0 ? Lighting.Lights[0].OuterRadius : 5.5;
            set
            {
                foreach (var l in Lighting.Lights) l.OuterRadius = value;
                Lighting.RebakeAllShadows(_map);
            }
        }
        public Color LanternColor = Color.FromArgb(255, 235, 195);
        private DirectBitmap _frameBuffer;
        private readonly System.Collections.Generic.List<WallSconce> _sconces = new System.Collections.Generic.List<WallSconce>();

        public bool WireframeBW { get; set; } = true;             // draw horizon + outlines
        public WallTexture CurrentTexture { get; set; } = WallTexture.BrickBW;

        // Texture parameters (tweak to taste)
        public int BricksPerY = 8;            // rows per tile (vertical)
        public int BricksPerX = 12;           // columns per tile (horizontal)
        public double MortarFracY = 0.10;     // 0..1 : mortar thickness fraction vertically
        public double MortarFracX = 0.08;     // 0..1 : mortar thickness fraction horizontally

        public int GridTilesY = 6;            // for GridBW
        public int GridTilesX = 6;
        public double GridLineFrac = 0.08;

        public int PlanksPerTile = 10;        // for PlanksBW
        public double PlankGapFrac = 0.08;

        private Timer _frameTimer;


        // Items rendering
        public double ItemSpriteScale = 0.14;   // relative to wall height
        public float ItemOutlineWidth = 2.5f;  // thicker outlines
        public int MaxItemTilesAhead = 5;    // draw clutter up to N tiles forward

        private void AutoAdjustFovIfNeeded()
        {
            if (!AutoAdjustFovNearWall) return;

            // Cast a single ray straight ahead
            RayHit hit;
            CastRayDDA(PlayerX, PlayerY, PlayerAngle, out hit);

            // If no hit, or absurd geometry, unwind to default if we were widened
            if (!hit.Hit)
            {
                if (_fovWidened) { FOV = FovDefaultRadians; _fovWidened = false; }
                return;
            }

            // Fisheye-correct distance for forward-facing feel
            double delta = NormalizeAngle(PlayerAngle - PlayerAngle); // 0, but keep pattern
            double correctedDist = hit.Distance * Math.Cos(delta);    // = hit.Distance

            // Hysteresis: enter at CloseWallEnter, exit at CloseWallExit
            if (!_fovWidened && correctedDist <= CloseWallEnter)
            {
                FOV = FovCloseRadians;
                _fovWidened = true;
            }
            else if (_fovWidened && correctedDist >= CloseWallExit)
            {
                FOV = FovDefaultRadians;
                _fovWidened = false;
            }
        }

        // Idle-loop plumbing (inside RaycasterCanvas)
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        // Returns true while the message queue is empty → we're "idle"
        private static bool AppStillIdle()
        {
            NativeMessage msg;
            return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
        }

        private void StartRenderLoop()
        {
            _sw = System.Diagnostics.Stopwatch.StartNew();
            _lastSec = 0.0;
            Application.Idle += OnAppIdle;
        }

        private void StopRenderLoop()
        {
            Application.Idle -= OnAppIdle;
        }

        private void OnAppIdle(object sender, EventArgs e)
        {
            // Drain idle frames: update tween based on precise dt and repaint
            while (AppStillIdle())
            {
                double now = _sw.Elapsed.TotalSeconds;
                double dt = now - _lastSec;
                if (dt < 0) dt = 0;
                if (dt > 0.1) dt = 0.1; // clamp long stalls

                // Advance turn tween with precise dt
                AdvanceTurnTween(dt);

                _lastSec = now;
                Invalidate(); // schedule a paint with latest angle
                              // Let message pump breathe:
                              // Application.DoEvents() is NOT needed; returning to pump is enough.
            }
        }


        // ===== Items / clutter =====
        public enum ItemKind { Rock, Potion, Chest }
        public enum Quadrant { Q1 = 1, Q2 = 2, Q3 = 3, Q4 = 4 } // Q1=TL, Q2=TR, Q3=BL, Q4=BR

        public sealed class InanimateItem
        {
            public ItemKind Kind;
            public int TileX, TileY;
            public Quadrant Quad;
            public bool PassThru;
            public ItemInstance SourceInstance;
            public Color TintColor = Color.FromArgb(200, 200, 200);
        }

        public DungeonLevel CurrentLevel { get; private set; }
        public System.Collections.Generic.List<ItemInstance> PlayerInventory { get; } =
            new System.Collections.Generic.List<ItemInstance>();

        public void LoadLevel(DungeonLevel level)
        {
            if (level == null) return;
            CurrentLevel = level;

            SetMap(level.ToRaycasterMap());
            SetPlayerStart((int)Math.Floor(level.SpawnX), (int)Math.Floor(level.SpawnY), level.SpawnAngle * 180.0 / Math.PI);
            level.Config.ApplyTo(this);

            _items.Clear();
            _occupancy.Clear();

            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    var tile = level.Tiles[y, x];
                    for (int s = 0; s < 4; s++)
                    {
                        var inst = tile.Slots[s];
                        if (inst != null)
                        {
                            var q = (Quadrant)(s + 1);
                            ItemKind k = ItemKind.Rock;
                            string sk = inst.Definition.SpriteKind?.ToLowerInvariant() ?? "";
                            if (sk == "potion") k = ItemKind.Potion;
                            else if (sk == "chest") k = ItemKind.Chest;

                            AddItem(k, x, y, q, inst.Definition.PassThru, inst, inst.Definition.Color);
                        }
                    }
                }
            }

            Invalidate();
        }

        public bool TryDropItemFromInventory(int inventoryIndex = -1)
        {
            if (PlayerInventory.Count == 0) return false;
            if (inventoryIndex < 0 || inventoryIndex >= PlayerInventory.Count)
                inventoryIndex = PlayerInventory.Count - 1;

            var itemToDrop = PlayerInventory[inventoryIndex];
            int pcx = (int)Math.Floor(PlayerX);
            int pcy = (int)Math.Floor(PlayerY);

            if (CurrentLevel != null)
            {
                if (pcx < 0 || pcy < 0 || pcx >= CurrentLevel.Width || pcy >= CurrentLevel.Height) return false;
                var tile = CurrentLevel.Tiles[pcy, pcx];

                // Preferred quadrant based on facing
                int prefQuad = 0;
                switch (_facing)
                {
                    case Cardinal.East: prefQuad = 1; break;  // Q2 (TR)
                    case Cardinal.West: prefQuad = 0; break;  // Q1 (TL)
                    case Cardinal.North: prefQuad = 0; break; // Q1 (TL)
                    case Cardinal.South: prefQuad = 2; break; // Q3 (BL)
                }

                if (!tile.TryDropItem(itemToDrop, prefQuad, out int droppedSlot))
                    return false; // all 4 quadrants full!

                var q = (Quadrant)(droppedSlot + 1);
                ItemKind k = ItemKind.Rock;
                string sk = itemToDrop.Definition.SpriteKind?.ToLowerInvariant() ?? "";
                if (sk == "potion") k = ItemKind.Potion;
                else if (sk == "chest") k = ItemKind.Chest;

                AddItem(k, pcx, pcy, q, itemToDrop.Definition.PassThru, itemToDrop, itemToDrop.Definition.Color);
                PlayerInventory.RemoveAt(inventoryIndex);
                return true;
            }
            return false;
        }

        private readonly System.Collections.Generic.List<InanimateItem> _items =
            new System.Collections.Generic.List<InanimateItem>();

        // enforce "only one per quadrant per tile"
        private readonly System.Collections.Generic.HashSet<string> _occupancy =
            new System.Collections.Generic.HashSet<string>();

        private static string OccKey(int x, int y, Quadrant q) { return x + "|" + y + "|" + (int)q; }

        public bool AddItem(ItemKind kind, int tileX, int tileY, Quadrant quad, bool passThru, ItemInstance source = null, Color? tint = null)
        {
            string key = OccKey(tileX, tileY, quad);
            if (_occupancy.Contains(key)) return false; // taken

            _items.Add(new InanimateItem
            {
                Kind = kind,
                TileX = tileX,
                TileY = tileY,
                Quad = quad,
                PassThru = passThru,
                SourceInstance = source,
                TintColor = tint ?? Color.FromArgb(200, 200, 200)
            });
            _occupancy.Add(key);
            Invalidate();
            return true;
        }

        private bool HasBlockingItemAt(int cx, int cy)
        {
            if (CurrentLevel != null)
            {
                if (cx >= 0 && cy >= 0 && cx < CurrentLevel.Width && cy < CurrentLevel.Height)
                {
                    if (!CurrentLevel.Tiles[cy, cx].IsPassable())
                        return true;
                }
            }

            for (int i = 0; i < _items.Count; i++)
                if (_items[i].TileX == cx && _items[i].TileY == cy && !_items[i].PassThru)
                    return true;
            return false;
        }

        public RaycasterCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            // Make sure we can receive focus and mouse events
            this.TabStop = true;
            this.SetStyle(ControlStyles.Selectable, true);
            this.Cursor = Cursors.Cross;

            BackColor = Color.White;

            // _frameTimer = new Timer { Interval = 16 };
            //_frameTimer = new Timer { Interval = 10};
            //_frameTimer.Tick += (s, e) => Invalidate();
            //_frameTimer.Start();

            RebuildWallLanterns();
            StartRenderLoop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _frameBuffer?.Dispose();
                _frameBuffer = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawScene(e.Graphics, ClientSize);
        }

        private void DrawScene(Graphics g, Size size)
        {
            int w = Math.Max(1, size.Width);
            int h = Math.Max(1, size.Height);
            int horizon = h / 2;

            // Normal alpha blending
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

            // Ensure direct pixel buffer is ready
            if (_frameBuffer == null || _frameBuffer.Width != w || _frameBuffer.Height != h)
            {
                _frameBuffer?.Dispose();
                _frameBuffer = new DirectBitmap(w, h);
            }

            // 1) BACKGROUND: Ceiling (ambient solid fill)
            int ceilArgb = CeilingColor.ToArgb();
            for (int y = 0; y < horizon; y++)
            {
                int rowOff = y * w;
                for (int x = 0; x < w; x++)
                    _frameBuffer.Bits[rowOff + x] = ceilArgb;
            }

            AutoAdjustFovIfNeeded();

            double halfFov = FOV * 0.5;
            double tanHalfFov = Math.Tan(halfFov);
            double projDist = (w * 0.5) / tanHalfFov;

            // 2) FLOOR: Perspective raycasting with light falloff and wall-cast shadows
            double rayDirX0 = Math.Cos(PlayerAngle - halfFov);
            double rayDirY0 = Math.Sin(PlayerAngle - halfFov);
            double rayDirX1 = Math.Cos(PlayerAngle + halfFov);
            double rayDirY1 = Math.Sin(PlayerAngle + halfFov);

            for (int y = horizon; y < h; y++)
            {
                int dy = y - horizon;
                if (dy == 0) dy = 1;

                double rowDist = (TileSize * projDist * VerticalScale * 0.5) / dy;

                double floorStepX = rowDist * (rayDirX1 - rayDirX0) / w;
                double floorStepY = rowDist * (rayDirY1 - rayDirY0) / w;

                double floorX = PlayerX + rowDist * rayDirX0;
                double floorY = PlayerY + rowDist * rayDirY0;

                int rowOff = y * w;

                for (int x = 0; x < w; x++)
                {
                    float light = Lighting.SampleLight(floorX, floorY, out float rT, out float gT, out float bT);

                    Color baseCol = FloorColor;
                    if (Lighting.ShowFloorTiles)
                    {
                        double fx = floorX - (int)floorX;
                        double fy = floorY - (int)floorY;
                        if (fx < 0.04 || fy < 0.04)
                            baseCol = Lighting.FloorTileLineColor;
                    }

                    _frameBuffer.Bits[rowOff + x] = Lighting.ShadeArgb(baseCol, light, rT, gT, bT, rowDist);

                    int ceilY = horizon - dy;
                    if (ceilY >= 0)
                    {
                        _frameBuffer.Bits[ceilY * w + x] = Lighting.ShadeArgb(CeilingColor, light * 0.75f, rT, gT, bT, rowDist);
                    }

                    floorX += floorStepX;
                    floorY += floorStepY;
                }
            }

            // 3) WALLS: render into direct pixel buffer, build depth buffer & spans
            double[] depthBuf = new double[w];
            for (int i = 0; i < w; i++) depthBuf[i] = double.PositiveInfinity;

            var wallSpans = new System.Collections.Generic.Dictionary<int, WallSpan>();
            int SpanKey(int cx, int cy, WallFace f) => (cy << 18) ^ (cx << 2) ^ (int)f;

            int[] sliceTops = new int[w];
            int[] sliceBots = new int[w];
            int[] sliceCellsX = new int[w];
            int[] sliceCellsY = new int[w];
            bool[] sliceHadHit = new bool[w];
            float[] sliceLights = new float[w];

            for (int x = 0; x < w; x++)
            {
                double cameraX = ((x + 0.5) / (double)w) * 2.0 - 1.0;
                double rayAngle = PlayerAngle + Math.Atan(cameraX * tanHalfFov);

                RayHit hit;
                CastRayDDA(PlayerX, PlayerY, rayAngle, out hit);
                if (!hit.Hit)
                {
                    sliceHadHit[x] = false;
                    continue;
                }

                sliceHadHit[x] = true;
                sliceCellsX[x] = hit.CellX;
                sliceCellsY[x] = hit.CellY;

                int pcx0 = (int)Math.Floor(PlayerX);
                int pcy0 = (int)Math.Floor(PlayerY);
                RevealAlongRayLimited(pcx0, pcy0, hit);

                double delta = NormalizeAngle(rayAngle - PlayerAngle);
                double correctedDist = hit.Distance * Math.Cos(delta);
                if (correctedDist < 1e-4) correctedDist = 1e-4;
                depthBuf[x] = correctedDist;

                int sliceHeight = (int)Math.Round(((TileSize * projDist) / correctedDist) * VerticalScale);
                int yTop = horizon - sliceHeight / 2;
                int yBot = yTop + sliceHeight;
                if (yTop < 0) yTop = 0;
                if (yBot > h) yBot = h;

                sliceTops[x] = yTop;
                sliceBots[x] = yBot;

                // Wall lighting sample: push sample point outward along the true wall face normal
                // so shallow/grazing view angles don't bleed into the wall's internal shadow
                double normX = (hit.Side == HitSide.Vertical) ? -hit.StepX : 0.0;
                double normY = (hit.Side == HitSide.Horizontal) ? -hit.StepY : 0.0;

                double hitWorldX = PlayerX + hit.Distance * Math.Cos(rayAngle);
                double hitWorldY = PlayerY + hit.Distance * Math.Sin(rayAngle);
                double wxSample = hitWorldX + normX * 0.05;
                double wySample = hitWorldY + normY * 0.05;

                float wallLight = Lighting.ComputeLightDirect(wxSample, wySample, out float wrT, out float wgT, out float wbT, normX, normY);
                if (hit.Side == HitSide.Horizontal)
                    wallLight *= 0.85f; // classic 3D directional side shading

                sliceLights[x] = wallLight;

                int shadedFill = Lighting.ShadeArgb(WallFillColor, wallLight, wrT, wgT, wbT, correctedDist);
                int shadedMortar = Lighting.ShadeArgb(MortarLineColor, wallLight, wrT, wgT, wbT, correctedDist);

                if (CurrentTexture == WallTexture.None)
                {
                    for (int sy = yTop; sy < yBot; sy++)
                        _frameBuffer.Bits[sy * w + x] = shadedFill;
                }
                else
                {
                    double u = hit.U;
                    int slicePix = Math.Max(1, yBot - yTop);

                    for (int sy = yTop; sy < yBot; sy++)
                    {
                        double v = (sy - yTop) / (double)slicePix;
                        bool drawLine = false;

                        double u2 = Wrap01(u + TexPhaseU);
                        double v2 = Wrap01(v + TexPhaseV);

                        switch (CurrentTexture)
                        {
                            case WallTexture.BrickBW: drawLine = IsBrickMortar(u2, v2); break;
                            case WallTexture.GridBW: drawLine = IsGridLine(u2, v2); break;
                            case WallTexture.PlanksBW: drawLine = IsPlankGap(v2); break;
                        }

                        _frameBuffer.Bits[sy * w + x] = drawLine ? shadedMortar : shadedFill;
                    }
                }

                WallFace face;
                if (hit.Side == HitSide.Vertical)
                    face = (hit.StepX > 0) ? WallFace.West : WallFace.East;
                else
                    face = (hit.StepY > 0) ? WallFace.North : WallFace.South;

                int sk = SpanKey(hit.CellX, hit.CellY, face);
                if (wallSpans.TryGetValue(sk, out var s))
                {
                    if (x < s.MinX) s.MinX = x;
                    if (x > s.MaxX) s.MaxX = x;
                    if (yTop < s.YTop) s.YTop = yTop;
                    if (yBot > s.YBot) s.YBot = yBot;
                    wallSpans[sk] = s;
                }
                else
                {
                    wallSpans[sk] = new WallSpan
                    {
                        CellX = hit.CellX,
                        CellY = hit.CellY,
                        Face = face,
                        MinX = x,
                        MaxX = x,
                        YTop = yTop,
                        YBot = yBot
                    };
                }
            }

            // Blit rendered 3D scene (walls + floors + lighting)
            g.DrawImageUnscaled(_frameBuffer.Bitmap, 0, 0);

            // 4) VECTOR OVERLAYS (Wireframe outlines, Torches, Items, Horizon)
            if (WireframeBW)
            {
                int prevYTop = -1, prevYBot = -1;
                int prevCellX = -999, prevCellY = -999;
                bool prevHadHit = false;

                for (int x = 0; x < w; x++)
                {
                    if (!sliceHadHit[x])
                    {
                        prevHadHit = false;
                        prevCellX = prevCellY = -999;
                        prevYTop = prevYBot = -1;
                        continue;
                    }

                    int yTop = sliceTops[x];
                    int yBot = sliceBots[x];
                    int cellX = sliceCellsX[x];
                    int cellY = sliceCellsY[x];

                    int wireArgb = Lighting.ShadeArgb(WireframeColor, sliceLights[x], 1f, 1f, 1f, depthBuf[x]);
                    using (var wirePen = new Pen(Color.FromArgb(wireArgb), 1f))
                    {
                        bool cellChanged = (cellX != prevCellX) || (cellY != prevCellY) || !prevHadHit;
                        bool heightJump = Math.Abs(yTop - prevYTop) > 1 || Math.Abs(yBot - prevYBot) > 1;
                        if (cellChanged || heightJump || x == 0)
                            g.DrawLine(wirePen, x, yTop, x, yBot);

                        if (prevHadHit)
                        {
                            g.DrawLine(wirePen, x - 1, prevYTop, x, yTop);
                            g.DrawLine(wirePen, x - 1, prevYBot, x, yBot);
                        }
                    }

                    prevHadHit = true;
                    prevCellX = cellX;
                    prevCellY = cellY;
                    prevYTop = yTop;
                    prevYBot = yBot;
                }
            }

            // 5) TORCHES on walls - only drawn if an actual sconce is mounted on this visible face!
            if (ShowTorchesOnWalls && _sconces.Count > 0)
            {
                foreach (var sc in _sconces)
                {
                    int sk = SpanKey(sc.CellX, sc.CellY, sc.Face);
                    if (wallSpans.TryGetValue(sk, out var s))
                    {
                        int xCenter = (s.MinX + s.MaxX) / 2;
                        int sliceH = Math.Max(1, s.YBot - s.YTop);

                        float torchH = (float)(sliceH * 0.20f);
                        float torchW = torchH * 0.70f;
                        float drawX = xCenter - torchW * 0.5f;
                        float drawY = s.YTop + (sliceH * 0.08f);

                        DrawTorchSprite(g, drawX, drawY, torchW, torchH);
                    }
                }
            }

            // 6) ITEMS with occlusion
            RenderItemsWithOcclusion(g, size, projDist, depthBuf);

            // 7) Horizon line (only drawn across open columns with no wall in front)
            if (WireframeBW)
            {
                int horizonArgb = Lighting.ShadeArgb(WireframeColor, 0f, 1f, 1f, 1f, Lighting.MaxViewDepth);
                using (var wirePen = new Pen(Color.FromArgb(horizonArgb), 1f))
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!sliceHadHit[x])
                            g.DrawLine(wirePen, x, horizon, x + 1, horizon);
                    }
                }
            }
        }

        //private void DrawHudOverlay(Graphics g, Size size)
        //{
        //    int w = size.Width;
        //    int h = size.Height;
        //    int hudH = Math.Max(60, HudHeightPx);

        //    // HUD rectangle at the bottom
        //    Rectangle hud = new Rectangle(0, h - hudH, w, hudH);

        //    // Panel background (light bezel look)
        //    using (var bg = new SolidBrush(Color.FromArgb(236, 236, 236)))
        //    using (var border = new Pen(Color.Black, 2f))
        //    using (var darkLine = new Pen(Color.FromArgb(180, 180, 180), 1f))
        //    using (var lightLine = new Pen(Color.White, 1f))
        //    {
        //        g.FillRectangle(bg, hud);
        //        g.DrawRectangle(border, hud);

        //        // Bevel lines
        //        g.DrawLine(lightLine, hud.Left + 1, hud.Top + 1, hud.Right - 2, hud.Top + 1);
        //        g.DrawLine(lightLine, hud.Left + 1, hud.Top + 1, hud.Left + 1, hud.Bottom - 2);
        //        g.DrawLine(darkLine, hud.Left + 1, hud.Bottom - 2, hud.Right - 2, hud.Bottom - 2);
        //        g.DrawLine(darkLine, hud.Right - 2, hud.Top + 1, hud.Right - 2, hud.Bottom - 2);
        //    }

        //    // Safe inner area
        //    var inner = Rectangle.Inflate(hud, -HudPadding, -HudPadding);

        //    // Layout: [HP/MP left] [center free] [compass right]
        //    int colGap = 16;
        //    int compBoxSize = Math.Min(inner.Height, Math.Max(72, hud.Height - HudPadding * 2));
        //    var compassBox = new Rectangle(
        //        inner.Right - compBoxSize,
        //        inner.Top + (inner.Height - compBoxSize) / 2,
        //        compBoxSize, compBoxSize);

        //    var leftBox = new Rectangle(
        //        inner.Left,
        //        inner.Top,
        //        Math.Max(180, inner.Width / 4),
        //        inner.Height);

        //    // Draw HP/MP bars
        //    DrawStatBar(g, leftBox, "HP", HP, HPMax, Color.Black);
        //    // Stack MP under HP
        //    var mpBox = new Rectangle(leftBox.Left, leftBox.Top + leftBox.Height / 2, leftBox.Width, leftBox.Height / 2 - 2);
        //    DrawStatBar(g, mpBox, "MP", MP, MPMax, Color.Black);

        //    // Draw round compass in the right box (using your engine angle & cardinal snap)
        //    DrawHudCompass(g, compassBox, PlayerAngle);
        //}

        //private void DrawStatBar(Graphics g, Rectangle box, string label, int value, int max, Color ink)
        //{
        //    value = Math.Max(0, Math.Min(max, value));
        //    float pct = (max > 0) ? (value / (float)max) : 0f;

        //    // Split label on the left, bar on the right
        //    int labelW = Math.Max(40, box.Width / 5);
        //    var labelRect = new Rectangle(box.Left, box.Top, labelW, box.Height);
        //    var barRect = new Rectangle(box.Left + labelW + 8, box.Top + box.Height / 4,
        //                                  box.Width - labelW - 8, box.Height / 2);

        //    using (var f = new Font(FontFamily.GenericSansSerif, Math.Max(9, box.Height * 0.28f), FontStyle.Bold))
        //    using (var b = new SolidBrush(ink))
        //        g.DrawString(label, f, b, labelRect);

        //    // Bar background
        //    using (var bg = new SolidBrush(Color.White))
        //    using (var border = new Pen(ink, 1.5f))
        //    {
        //        g.FillRectangle(bg, barRect);
        //        g.DrawRectangle(border, barRect);
        //    }

        //    // Fill amount
        //    int fillW = (int)Math.Round(barRect.Width * pct);
        //    if (fillW > 0)
        //    {
        //        using (var fill = new SolidBrush(Color.FromArgb(200, 200, 200)))
        //            g.FillRectangle(fill, new Rectangle(barRect.Left, barRect.Top, fillW, barRect.Height));
        //    }

        //    // Numeric text on right
        //    string s = $"{value}/{max}";
        //    var sSize = TextRenderer.MeasureText(s, SystemFonts.DefaultFont);
        //    var sPt = new Point(barRect.Right - sSize.Width, barRect.Top - sSize.Height);
        //    using (var b = new SolidBrush(ink))
        //        g.DrawString(s, SystemFonts.DefaultFont, b, sPt);
        //}

        //private void DrawHudCompass(Graphics g, Rectangle box, double directionRadians)
        //{
        //    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        //    // Center the round compass in the box
        //    float cx = box.Left + box.Width * 0.5f;
        //    float cy = box.Top + box.Height * 0.52f; // a hair lower to leave room for the 'N'
        //    float bezelR = Math.Min(box.Width, box.Height) * 0.5f * HudCompassScaleK;

        //    // Title “N”
        //    using (var f = new Font(FontFamily.GenericSansSerif, Math.Max(8, box.Height * 0.14f), FontStyle.Bold))
        //    using (var b = new SolidBrush(Color.Black))
        //    {
        //        const string north = "N";
        //        var sz = TextRenderer.MeasureText(north, f);
        //        g.DrawString(north, f, b, cx - sz.Width / 2f + 2f, box.Top);
        //    }

        //    // Bezel
        //    using (var ring = new Pen(Color.Black, 1.5f))
        //        g.DrawEllipse(ring, cx - bezelR, cy - bezelR, bezelR * 2, bezelR * 2);

        //    // Snap to NSEW
        //    double q = Math.Round(directionRadians / (Math.PI / 2.0)) * (Math.PI / 2.0);
        //    float ux = (float)Math.Cos(q);
        //    float uy = (float)Math.Sin(q);
        //    float px = -uy, py = ux;

        //    // Margin so the arrow tip doesn’t touch the ring
        //    float margin = Math.Max(0f, HudCompassArrowMarginFrac) * bezelR;
        //    float arrowR = Math.Max(4f, bezelR - margin);

        //    // Arrow geometry
        //    float lineW = Math.Max(2f, bezelR * HudCompassArrowLineFrac);
        //    float tailLen = arrowR * 0.50f;
        //    float headLen = arrowR * HudCompassArrowHeadFrac;
        //    float headW = headLen * HudCompassArrowHeadWidthFrac;

        //    var tipX = cx + ux * arrowR;
        //    var tipY = cy + uy * arrowR;
        //    var headBaseX = cx + ux * (arrowR - headLen);
        //    var headBaseY = cy + uy * (arrowR - headLen);
        //    var tailX = cx - ux * tailLen;
        //    var tailY = cy - uy * tailLen;

        //    // Tail line
        //    using (var pen = new Pen(Color.Black, lineW))
        //        g.DrawLine(pen, tailX, tailY, headBaseX, headBaseY);

        //    // Arrow head
        //    float halfW = headW * 0.5f;
        //    var leftX = headBaseX + px * (-halfW);
        //    var leftY = headBaseY + py * (-halfW);
        //    var rightX = headBaseX + px * (halfW);
        //    var rightY = headBaseY + py * (halfW);

        //    using (var fill = new SolidBrush(Color.Black))
        //    using (var outline = new Pen(Color.Black, 1f))
        //    {
        //        var tri = new PointF[]
        //        {
        //    new PointF(tipX, tipY),
        //    new PointF(rightX, rightY),
        //    new PointF(leftX,  leftY)
        //        };
        //        g.FillPolygon(fill, tri);
        //        g.DrawPolygon(outline, tri);
        //    }
        //}

        public enum WallFace { North = 0, South = 1, West = 2, East = 3 }

        public sealed class WallSconce
        {
            public int CellX;
            public int CellY;
            public WallFace Face;
            public PointLight Light;
        }

        // Per-wall visible span we’ll collect during the column pass
        private struct WallSpan
        {
            public int CellX, CellY; // map cell coords of the wall
            public WallFace Face;
            public int MinX, MaxX;   // screen-space x span covered by this wall
            public int YTop, YBot;   // vertical extent (clamped to screen)
        }
        // Neighbor checks used to decide “exposed” walls only
        public bool IsWallCell(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _map.GetLength(1) || y >= _map.GetLength(0)) return true;
            return _map[y, x] == 1;
        }
        private bool HasExposedFace(int cx, int cy)
        {
            // If any 4-neighbor is NOT a wall, this wall shows at least one open face.
            return !IsWallCell(cx + 1, cy) || !IsWallCell(cx - 1, cy) ||
                   !IsWallCell(cx, cy + 1) || !IsWallCell(cx, cy - 1);
        }


        // ======= Texture evaluators (pure math) =======

        // Staggered bricks: draw mortar lines only (black). Brick interiors remain white.
        private bool IsBrickMortar(double u, double v)
        {
            int rows = Math.Max(1, BricksPerY);
            int cols = Math.Max(1, BricksPerX);

            double vScaled = v * rows;
            int row = (int)Math.Floor(vScaled);
            double vFrac = vScaled - row;

            // stagger odd rows by half a brick
            double uScaled = (u * cols) + ((row % 2 == 1) ? 0.5 : 0.0);
            double uFrac = uScaled - Math.Floor(uScaled); // wrap [0,1)

            double mtY = Clamp01(MortarFracY);
            double mtX = Clamp01(MortarFracX);

            if (vFrac <= mtY || vFrac >= (1.0 - mtY)) return true; // horizontal mortar
            if (uFrac <= mtX || uFrac >= (1.0 - mtX)) return true; // vertical mortar
            return false;
        }

        private bool IsGridLine(double u, double v)
        {
            int rows = Math.Max(1, GridTilesY);
            int cols = Math.Max(1, GridTilesX);

            double vScaled = v * rows;
            double uScaled = u * cols;

            double vFrac = vScaled - Math.Floor(vScaled);
            double uFrac = uScaled - Math.Floor(uScaled);

            double line = Clamp01(GridLineFrac);
            return (vFrac <= line) || (vFrac >= 1.0 - line) ||
                   (uFrac <= line) || (uFrac >= 1.0 - line);
        }

        private bool IsPlankGap(double v)
        {
            int rows = Math.Max(1, PlanksPerTile);
            double vScaled = v * rows;
            double vFrac = vScaled - Math.Floor(vScaled);
            double gap = Clamp01(PlankGapFrac);
            return vFrac <= gap || vFrac >= 1.0 - gap;
        }

        private static double Clamp01(double x)
        {
            if (x < 0) return 0;
            if (x > 1) return 1;
            return x;
        }

        // ======= Raycasting core =======

        // inside RaycasterCanvas
        private enum HitSide { Horizontal, Vertical }

        private struct RayHit
        {
            public bool Hit;
            public double Distance;
            public HitSide Side;
            public int CellX;
            public int CellY;
            public double U;   // 0..1 across face
            public int StepX;  // NEW: step direction used by DDA
            public int StepY;  // NEW
        }


        private void CastRayDDA(double px, double py, double angle, out RayHit result)
        {
            result = new RayHit { Hit = false, Distance = MaxRayDistance, Side = HitSide.Vertical, CellX = -1, CellY = -1, U = 0 };

            double dirX = Math.Cos(angle);
            double dirY = Math.Sin(angle);

            int mapX = (int)Math.Floor(px);
            int mapY = (int)Math.Floor(py);

            double deltaDistX = (dirX == 0) ? 1e30 : Math.Abs(1.0 / dirX);
            double deltaDistY = (dirY == 0) ? 1e30 : Math.Abs(1.0 / dirY);

            int stepX, stepY;
            double sideDistX, sideDistY;

            if (dirX < 0) { stepX = -1; sideDistX = (px - mapX) * deltaDistX; }
            else { stepX = 1; sideDistX = (mapX + 1.0 - px) * deltaDistX; }

            if (dirY < 0) { stepY = -1; sideDistY = (py - mapY) * deltaDistY; }
            else { stepY = 1; sideDistY = (mapY + 1.0 - py) * deltaDistY; }

            int maxSteps = 1024;
            for (int i = 0; i < maxSteps; i++)
            {
                HitSide side;
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = HitSide.Vertical;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = HitSide.Horizontal;
                }

                if (mapX < 0 || mapY < 0 || mapX >= _map.GetLength(1) || mapY >= _map.GetLength(0))
                    break;

                if (_map[mapY, mapX] != 0)
                {
                    double dist = (side == HitSide.Vertical)
                        ? (mapX - px + (1 - stepX) * 0.5) / (dirX == 0 ? 1e-9 : dirX)
                        : (mapY - py + (1 - stepY) * 0.5) / (dirY == 0 ? 1e-9 : dirY);

                    if (dist < 0.0001) dist = 0.0001;

                    // Impact point in world space
                    double hitX = px + dist * dirX;
                    double hitY = py + dist * dirY;

                    // Compute u = [0..1] across wall face
                    double u = (side == HitSide.Vertical)
                        ? (hitY - Math.Floor(hitY)) // vertical wall: use fractional Y
                        : (hitX - Math.Floor(hitX)); // horizontal wall: use fractional X

                    // Flip to keep pattern orientation consistent when stepping "backwards"
                    if ((side == HitSide.Vertical && stepX < 0) ||
                        (side == HitSide.Horizontal && stepY > 0))
                        u = 1.0 - u;

                    // after you computed 'u' and before 'return;'
                    result.Hit = true;
                    result.Distance = Math.Min(dist, MaxRayDistance);
                    result.Side = side;
                    result.CellX = mapX;
                    result.CellY = mapY;
                    result.U = u;
                    result.StepX = stepX;    // NEW
                    result.StepY = stepY;    // NEW
                    return;

                }

                if (Math.Min(sideDistX, sideDistY) > MaxRayDistance + 1.0)
                    break;
            }
        }
       
        //// inside RaycasterCanvas
        //private void MarkPreWallFloorVisible(int pcx, int pcy, RayHit hit)
        //{
        //    // Cell just before the wall along the ray
        //    int fx = hit.CellX;
        //    int fy = hit.CellY;
        //    if (hit.Side == HitSide.Vertical)
        //        fx = hit.CellX - (hit.StepX > 0 ? 1 : -1);
        //    else
        //        fy = hit.CellY - (hit.StepY > 0 ? 1 : -1);

        //    if (fx >= 0 && fy >= 0 && fx < MapWidth && fy < MapHeight)
        //        _seen[fy, fx] = true;

        //    // Optional: also reveal the straight line from player to that floor cell
        //    int pcx0 = (int)Math.Floor(PlayerX);
        //    int pcy0 = (int)Math.Floor(PlayerY);
        //    MarkLineVisibleLimited(pcx0, pcy0, fx, fy, MapVisionTiles);
        //}

        //// Bresenham line to mark cells from (x0,y0) to (x1,y1) as seen
        //private void MarkLineVisible(int x0, int y0, int x1, int y1)
        //{
        //    EnsureSeenSize();

        //    int dx = Math.Abs(x1 - x0);
        //    int dy = Math.Abs(y1 - y0);
        //    int sx = x0 < x1 ? 1 : -1;
        //    int sy = y0 < y1 ? 1 : -1;
        //    int err = dx - dy;

        //    int x = x0, y = y0;

        //    int w = _map.GetLength(1);
        //    int h = _map.GetLength(0);

        //    while (true)
        //    {
        //        if (x >= 0 && y >= 0 && x < w && y < h)
        //            _seen[y, x] = true;

        //        if (x == x1 && y == y1) break;

        //        int e2 = 2 * err;
        //        if (e2 > -dy) { err -= dy; x += sx; }
        //        if (e2 < dx) { err += dx; y += sy; }
        //    }
        //}

        // Mark a line but stop after at most 'maxLen' cell steps (Chebyshev length)
        //private void MarkLineVisibleLimited(int x0, int y0, int x1, int y1, int maxLen)
        //{
        //    EnsureSeenSize();

        //    int dx = Math.Abs(x1 - x0);
        //    int dy = Math.Abs(y1 - y0);
        //    int sx = x0 < x1 ? 1 : -1;
        //    int sy = y0 < y1 ? 1 : -1;
        //    int err = dx - dy;

        //    int x = x0, y = y0;
        //    int steps = 0;
        //    int w = _map.GetLength(1), h = _map.GetLength(0);

        //    while (true)
        //    {
        //        if (x >= 0 && y >= 0 && x < w && y < h)
        //            _seen[y, x] = true;

        //        if (x == x1 && y == y1) break;

        //        // stop if we’ve taken maxLen cell steps
        //        if (steps >= maxLen) break;
        //        steps++;

        //        int e2 = 2 * err;
        //        if (e2 > -dy) { err -= dy; x += sx; }
        //        if (e2 < dx) { err += dx; y += sy; }
        //    }
        //}

        private static double NormalizeAngle(double a)
        {
            while (a <= -Math.PI) a += 2.0 * Math.PI;
            while (a > +Math.PI) a -= 2.0 * Math.PI;
            return a;
        }

        private bool IsBlocked(double x, double y)
        {
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);
            if (xi < 0 || yi < 0 || xi >= _map.GetLength(1) || yi >= _map.GetLength(0))
                return true; // OOB = blocked

            int v = _map[yi, xi];
            return v == TILE_WALL; // SPAWN (2) is NOT blocked
        }

        public void MoveRelative(double forward, double strafe, double dt)
        {
            double dx = Math.Cos(PlayerAngle) * forward - Math.Sin(PlayerAngle) * strafe;
            double dy = Math.Sin(PlayerAngle) * forward + Math.Cos(PlayerAngle) * strafe;

            double nx = PlayerX + dx * dt;
            double ny = PlayerY + dy * dt;

            if (!EnableCollision)
            {
                PlayerX = nx; PlayerY = ny; return;
            }

            if (!Collides(nx, ny))
            {
                PlayerX = nx; PlayerY = ny;
            }
            else
            {
                if (!Collides(nx, PlayerY)) PlayerX = nx;
                if (!Collides(PlayerX, ny)) PlayerY = ny;
                // otherwise blocked
            }
        }

        // Snap the view by ±90° (quarter-turn). Negative = left, positive = right.
        // +1 = clockwise (Right/E), -1 = counter-clockwise (Left/Q)
        public void SnapTurnQuarter(int stepsCW)
        {
            // Keep it to -inf..+inf, then mod 4
            int f = ((int)_facing + stepsCW) % 4;
            if (f < 0) f += 4;
            _facing = (Cardinal)f;

            // Set exact angle and keep the lock centered
            PlayerAngle = AngleOfFacing(_facing);
            RotationCenter = PlayerAngle;
        }



        public void Turn(double radians)
        {
            double target = PlayerAngle + radians;
            if (LockRotation)
                PlayerAngle = ClampAngleAround(target, RotationCenter, RotationLimit);
            else
                PlayerAngle = NormalizeAngle(target);
        }


        private static double AngleOfFacing(Cardinal f)
        {
            switch (f)
            {
                case Cardinal.East: return 0.0;
                case Cardinal.South: return Math.PI / 2.0;
                case Cardinal.West: return Math.PI;
                default: return 3.0 * Math.PI / 2.0; // North
            }
        }

        private static Cardinal FacingFromAngle(double angle)
        {
            // Normalize to [0, 2π)
            while (angle < 0) angle += 2.0 * Math.PI;
            while (angle >= 2.0 * Math.PI) angle -= 2.0 * Math.PI;

            // Quantize to nearest quadrant: 0=E, 1=S, 2=W, 3=N
            int q = (int)Math.Round(angle / (Math.PI / 2.0)) & 3;
            return (Cardinal)q;
        }

        private static void FacingOffsets(Cardinal f, out int dx, out int dy)
        {
            switch (f)
            {
                case Cardinal.East: dx = +1; dy = 0; break;
                case Cardinal.South: dx = 0; dy = +1; break;
                case Cardinal.West: dx = -1; dy = 0; break;
                default: dx = 0; dy = -1; break; // North
            }
        }


        // ==== Visibility tracking (fog-of-war style) ====
        private bool[,] _seen = new bool[1, 1];  // sized to map
        public bool[,] GetSeenSnapshot()
        {
            // return a copy so drawing code can’t mutate our state
            int h = _seen.GetLength(0), w = _seen.GetLength(1);
            var copy = new bool[h, w];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    copy[y, x] = _seen[y, x];
            return copy;
        }
        // Optional: clear entire seen grid (call if you ever want to reset discovery)
        private void ClearSeen()
        {
            if (_seen == null) return;
            Array.Clear(_seen, 0, _seen.Length);
        }

        // Ensure _seen matches current map size
        private void EnsureSeenSize()
        {
            int h = _map.GetLength(0);
            int w = _map.GetLength(1);
            if (_seen == null || _seen.GetLength(0) != h || _seen.GetLength(1) != w)
                _seen = new bool[h, w];
        }

        // Chebyshev distance (tile steps) between two cells
        private static int ChebyshevDist(int x0, int y0, int x1, int y1)
            => Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));

        // Mark a cell as seen if on-map
        private void See(int cx, int cy)
        {
            if (cx >= 0 && cy >= 0 && cx < MapWidth && cy < MapHeight)
                _seen[cy, cx] = true;
        }

        /*
         * Reveal along a ray from player cell (pcx,pcy) toward the DDA hit.
         * We reveal:
         *  - player cell
         *  - floor cells along the ray up to MapVisionTiles (Chebyshev steps)
         *  - wall cell only if within MapVisionTiles
         * Implementation: Bresenham from (pcx,pcy) to the "pre-wall floor" cell,
         * but stop after 'MapVisionTiles' steps. Then, if the hit wall is within range,
         * mark it too.
         */
        private void RevealAlongRayLimited(int pcx, int pcy, in RayHit hit)
        {
            EnsureSeenSize();

            // Always see the player’s tile
            See(pcx, pcy);

            // Compute the floor cell just before the wall hit
            int fx = hit.CellX;
            int fy = hit.CellY;
            if (hit.Side == HitSide.Vertical)
                fx = hit.CellX - (hit.StepX > 0 ? 1 : -1);
            else
                fy = hit.CellY - (hit.StepY > 0 ? 1 : -1);

            // Bresenham toward (fx,fy) but clamp to MapVisionTiles steps
            int x0 = pcx, y0 = pcy, x1 = fx, y1 = fy;
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int steps = 0;
            int x = x0, y = y0;

            while (true)
            {
                // reveal floor path cell
                See(x, y);

                if (x == x1 && y == y1) break;
                if (steps >= MapVisionTiles) break;
                steps++;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }

            // Only mark the hit wall if it’s within MapVisionTiles of the player cell
            if (ChebyshevDist(pcx, pcy, hit.CellX, hit.CellY) <= MapVisionTiles)
                See(hit.CellX, hit.CellY);
        }

        // ===== Map loading & spawn helpers =====

        // Optional: expose current map size
        public int MapWidth => _map.GetLength(1);
        public int MapHeight => _map.GetLength(0);

        // Set a new map (0=floor, 1=wall, optionally 2=spawn). Keeps player where he is unless you call a spawn helper.
        public void SetMap(int[,] newMap)
        {
            if (newMap == null) throw new ArgumentNullException(nameof(newMap));
            if (newMap.GetLength(0) < 1 || newMap.GetLength(1) < 1)
                throw new ArgumentException("Map must be at least 1x1.");
            _map = newMap;
            EnsureSeenSize();
            ClearSeen();
            RebuildWallLanterns();
            Invalidate();
        }

        /// <summary>
        /// Scans the map to place lanterns on exposed walls with proper spacing (~1/3 density)
        /// and bakes their 2D shadow maps.
        /// </summary>
        public void RebuildWallLanterns()
        {
            Lighting.ClearLights();
            _sconces.Clear();
            if (_map == null) return;
            int h = _map.GetLength(0);
            int w = _map.GetLength(1);

            var placedCoords = new System.Collections.Generic.List<(int x, int y)>();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (_map[y, x] != 1) continue;
                    if (!HasExposedFace(x, y)) continue;

                    // Unlit dark hallway: completely remove all lanterns to showcase the darkness transition
                    if (x >= 7 && y >= 2 && y <= 6) continue;

                    // Spacing check: ensure at least 4.8 units away from any other sconce
                    // Reduces count from 27 down to ~9 (1/3 of previous density)
                    bool tooClose = false;
                    foreach (var p in placedCoords)
                    {
                        int dx = x - p.x;
                        int dy = y - p.y;
                        if (dx * dx + dy * dy < 23) // sqrt(23) ~= 4.8 units
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    // Determine the best exposed face facing open floor
                    WallFace face;
                    double lx = x + 0.5;
                    double ly = y + 0.5;

                    if (y + 1 < h && _map[y + 1, x] == 0) // South face
                    {
                        face = WallFace.South;
                        ly = y + 1.05;
                    }
                    else if (y > 0 && _map[y - 1, x] == 0) // North face
                    {
                        face = WallFace.North;
                        ly = y - 0.05;
                    }
                    else if (x + 1 < w && _map[y, x + 1] == 0) // East face
                    {
                        face = WallFace.East;
                        lx = x + 1.05;
                    }
                    else if (x > 0 && _map[y, x - 1] == 0) // West face
                    {
                        face = WallFace.West;
                        lx = x - 0.05;
                    }
                    else continue;

                    var light = new PointLight(lx, ly, LightInnerRadius, LightOuterRadius)
                    {
                        LightColor = LanternColor
                    };
                    Lighting.AddLight(light);

                    _sconces.Add(new WallSconce
                    {
                        CellX = x,
                        CellY = y,
                        Face = face,
                        Light = light
                    });

                    placedCoords.Add((x, y));
                }
            }

            Lighting.RebakeAllShadows(_map);
        }


        // Put the player at the center of a given cell with an angle (degrees)
        public void SetPlayerStart(int cellX, int cellY, double angleDegrees = 0)
        {
            if (cellX < 0 || cellY < 0 || cellX >= MapWidth || cellY >= MapHeight)
                throw new ArgumentOutOfRangeException("Start cell outside map.");
            PlayerX = cellX + 0.5;
            PlayerY = cellY + 0.5;
            PlayerAngle = angleDegrees * Math.PI / 180.0;
        }

        // Find the first cell that matches a tile value (e.g., 2 = spawn). Returns true/false.
        public bool TryFindTile(int value, out int cellX, out int cellY)
        {
            for (int y = 0; y < MapHeight; y++)
                for (int x = 0; x < MapWidth; x++)
                    if (_map[y, x] == value) { cellX = x; cellY = y; return true; }
            cellX = cellY = -1;
            return false;
        }

        // Spawn at the first 'value' (default 2). If not found, fallback to first open floor (0).
        public void SpawnFromMap(int spawnTileValue = 2, double angleDegrees = 0)
        {
            int sx, sy;
            if (TryFindTile(spawnTileValue, out sx, out sy))
            {
                SetPlayerStart(sx, sy, angleDegrees);
                _map[sy, sx] = 0; // clear spawn to floor
            }
            else if (TryFindTile(0, out sx, out sy))
            {
                SetPlayerStart(sx, sy, angleDegrees);
            }
            else
            {
                SetPlayerStart(Math.Min(1, MapWidth - 1), Math.Min(1, MapHeight - 1), angleDegrees);
            }

            // Set discrete facing from the angle, and lock rotation center to exact cardinal
            _facing = FacingFromAngle(PlayerAngle);
            PlayerAngle = AngleOfFacing(_facing);
            RotationCenter = PlayerAngle;
        }


        // Convenience: load an ASCII map. '#'=wall, '.'=floor, 'S'=spawn (2). Spaces are treated as floor.
        // Lines may have different lengths; shorter lines are padded as floor.
        public static int[,] ParseAsciiMap(string[] lines, char wall = '#', char floor = '.', char spawn = 'S')
        {
            if (lines == null || lines.Length == 0) throw new ArgumentException("No lines.");
            int h = lines.Length;
            int w = 0;
            for (int i = 0; i < lines.Length; i++) w = Math.Max(w, lines[i] == null ? 0 : lines[i].Length);

            var map = new int[h, w];
            for (int y = 0; y < h; y++)
            {
                var row = lines[y] ?? string.Empty;
                for (int x = 0; x < w; x++)
                {
                    char c = (x < row.Length) ? row[x] : floor;
                    if (c == wall) map[y, x] = 1;
                    else if (c == spawn) map[y, x] = 2;
                    else map[y, x] = 0; // treat anything else as floor
                }
            }
            return map;
        }


        // Return true if the player's circle at (x,y) overlaps any solid cell or out-of-bounds.
        private bool Collides(double x, double y)
        {
            double r = PlayerRadius;
            // 8-direction samples around the circle
            double k = r / Math.Sqrt(2.0);
            // N, NE, E, SE, S, SW, W, NW
            double[] sx = { 0, k, r, k, 0, -k, -r, -k };
            double[] sy = { -r, -k, 0, k, r, k, 0, -k };

            // Also guard against being exactly on OOB
            if (IsBlocked(x, y)) return true;

            for (int i = 0; i < 8; i++)
            {
                if (IsBlocked(x + sx[i], y + sy[i])) return true;
            }
            return false;
        }


        // Clamp an angle to be within [center - limit, center + limit] using wrap-aware math.
        private double ClampAngleAround(double angle, double center, double limit)
        {
            // diff in (-π, π]
            double diff = NormalizeAngle(angle - center);
            if (diff > limit) diff = limit;
            if (diff < -limit) diff = -limit;
            return NormalizeAngle(center + diff);
        }

        //Movement
        // Current cell index of the player
        private void GetPlayerCell(out int cx, out int cy)
        {
            cx = (int)Math.Floor(PlayerX);
            cy = (int)Math.Floor(PlayerY);
        }

        private bool IsCellBlocked(int cx, int cy)
        {
            if (cx < 0 || cy < 0 || cx >= _map.GetLength(1) || cy >= _map.GetLength(0))
                return true;
            int v = _map[cy, cx];
            return v == TILE_WALL; // spawn/floor are walkable
        }

        private void VisibleFrontQuads(System.Collections.Generic.List<Quadrant> outList)
        {
            outList.Clear();
            // Q layout (map up = North):
            // [Q1 Q2]
            // [Q3 Q4]
            switch (_facing)
            {
                case Cardinal.East: outList.Add(Quadrant.Q2); outList.Add(Quadrant.Q4); break;
                case Cardinal.West: outList.Add(Quadrant.Q1); outList.Add(Quadrant.Q3); break;
                case Cardinal.North: outList.Add(Quadrant.Q1); outList.Add(Quadrant.Q2); break;
                default: outList.Add(Quadrant.Q3); outList.Add(Quadrant.Q4); break; // South
            }
        }
        private static void QuadOffset(Quadrant q, out double ox, out double oy)
        {
            switch (q)
            {
                case Quadrant.Q1: ox = -0.25; oy = -0.25; break;
                case Quadrant.Q2: ox = +0.25; oy = -0.25; break;
                case Quadrant.Q3: ox = -0.25; oy = +0.25; break;
                default: ox = +0.25; oy = +0.25; break; // Q4
            }
        }

        // DrawItemSprite with lighting and view depth attenuation
        private void DrawItemSprite(Graphics g, float x, float y, float w, float h, ItemKind kind, Color fillCol, Color outlineCol)
        {
            using (var fill = new SolidBrush(fillCol))
            using (var pen = new Pen(outlineCol, ItemOutlineWidth))
            {
                if (kind == ItemKind.Rock)
                {
                    g.FillEllipse(fill, x, y, w, h);
                    g.DrawEllipse(pen, x, y, w, h);
                }
                else if (kind == ItemKind.Potion)
                {
                    float bulbH = h * 0.72f;
                    float neckH = h - bulbH;
                    float neckW = w * 0.35f;
                    float neckX = x + (w - neckW) * 0.5f;

                    g.FillEllipse(fill, x, y + neckH, w, bulbH);
                    g.DrawEllipse(pen, x, y + neckH, w, bulbH);

                    g.FillRectangle(fill, neckX, y, neckW, neckH);
                    g.DrawRectangle(pen, neckX, y, neckW, neckH);
                }
                else if (kind == ItemKind.Chest)
                {
                    float baseH = h * 0.60f;
                    float lidH = h * 0.40f;

                    // Chest base
                    g.FillRectangle(fill, x, y + lidH, w, baseH);
                    g.DrawRectangle(pen, x, y + lidH, w, baseH);

                    // Arched lid
                    g.FillEllipse(fill, x, y, w, lidH * 2f);
                    g.DrawArc(pen, x, y, w, lidH * 2f, 180f, 180f);

                    // Gold lock / latch
                    float latchW = Math.Max(2f, w * 0.22f);
                    float latchH = Math.Max(3f, h * 0.25f);
                    using (var latchBrush = new SolidBrush(Color.Gold))
                    using (var latchPen = new Pen(Color.Black, 1.2f))
                    {
                        g.FillRectangle(latchBrush, x + (w - latchW) * 0.5f, y + lidH - latchH * 0.25f, latchW, latchH);
                        g.DrawRectangle(latchPen, x + (w - latchW) * 0.5f, y + lidH - latchH * 0.25f, latchW, latchH);
                    }
                }
            }
        }

        private void RenderItemsWithOcclusion(Graphics g, Size size, double projDist, double[] depthBuf)
        {
            int w = size.Width, h = size.Height, horizon = h / 2;

            // Precompute to camera rotation terms
            double cosA = Math.Cos(PlayerAngle), sinA = Math.Sin(PlayerAngle);

            // Which quads are visible *from this facing* (keeps your Q1/Q2 etc rule)
            var want = new System.Collections.Generic.List<Quadrant>(2);
            VisibleFrontQuads(want);

            // We’ll draw far -> near so nearer sprites naturally overlap
            var renderables = new System.Collections.Generic.List<(double ycam, Action draw)>(_items.Count + 16);

            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];

                // Optional max distance gate by tiles ahead (keep your 2-3 tiles vibe)
                double dxTiles = it.TileX + 0.5 - PlayerX;
                double dyTiles = it.TileY + 0.5 - PlayerY;
                double approxAhead = (Math.Cos(PlayerAngle) * dxTiles + Math.Sin(PlayerAngle) * dyTiles);
                if (approxAhead <= 0) continue; // behind you

                // crude gate by tiles forward if you want to keep it: 
                if (approxAhead > MaxItemTilesAhead + 0.5) continue;

                // Only draw items in the two visible quadrants of THEIR tile
                if (it.Quad != want[0] && it.Quad != want[1]) continue;

                // Sub-tile offset
                QuadOffset(it.Quad, out double offx, out double offy);
                double wx = it.TileX + 0.5 + offx;
                double wy = it.TileY + 0.5 + offy;

                // --- Proximity push: nudge items deeper into their tile when they are close ---
                // Player's forward unit vector in world space
                double fx = Math.Cos(PlayerAngle);
                double fy = Math.Sin(PlayerAngle);

                // How many tiles ahead (aligned with facing)?
                int stepAhead = 0;
                int pcx = (int)Math.Floor(PlayerX);
                int pcy = (int)Math.Floor(PlayerY);

                // Facing-aligned check: item tile shares the "across" coordinate and lies ahead
                if (Math.Abs(fx) >= Math.Abs(fy))
                {
                    if (it.TileY == pcy)
                    {
                        int delta = it.TileX - pcx;
                        stepAhead = (int)Math.Sign(fx) * delta;
                    }
                }
                else
                {
                    if (it.TileX == pcx)
                    {
                        int delta = it.TileY - pcy;
                        stepAhead = (int)Math.Sign(fy) * delta;
                    }
                }

                // Define push amounts per step
                double desiredPush = 0.0;
                if (stepAhead == 1) desiredPush = 0.22;
                else if (stepAhead == 2) desiredPush = 0.12;
                else if (stepAhead == 3) desiredPush = 0.06;

                if (desiredPush > 0.0)
                    NudgeTowardFarEdgeInTile(it.TileX, it.TileY, fx, fy, desiredPush, ref wx, ref wy);

                // Camera-space (rotate world delta by -PlayerAngle)
                double dxw = wx - PlayerX;
                double dyw = wy - PlayerY;
                double x_cam = -sinA * dxw + cosA * dyw;   // horizontal offset on screen
                double y_cam = cosA * dxw + sinA * dyw;   // forward depth
                if (y_cam <= 0.01) continue;

                // Project: align to ground (bottom of wall slice at this depth)
                double wallH = (TileSize / y_cam) * projDist * VerticalScale;
                double itemH = wallH * ItemSpriteScale;
                if (itemH < 3.0) itemH = 3.0; // don’t collapse to 0 px

                double screenX = (w * 0.5) + (x_cam / y_cam) * projDist;
                double yBotWall = horizon + (wallH * 0.5);
                double yTopItem = yBotWall - itemH;

                int x0 = (int)Math.Floor(screenX - itemH * 0.5);
                int x1 = (int)Math.Ceiling(screenX + itemH * 0.5);
                if (x1 < 0 || x0 >= w) continue; // off-screen

                // Depth test against wall columns in the sprite span
                int cx0 = Math.Max(0, x0);
                int cx1 = Math.Min(w - 1, x1);
                double minWallDepth = double.PositiveInfinity;
                for (int cx = cx0; cx <= cx1; cx++)
                    if (depthBuf[cx] < minWallDepth) minWallDepth = depthBuf[cx];

                // Only draw if sprite is *in front of* the walls in its span
                if (y_cam >= minWallDepth - 1e-6) continue;

                float sx = (float)(screenX - itemH * 0.5);
                float sy = (float)(yTopItem);
                float sw = (float)(itemH);
                float sh = (float)(itemH);

                float itemLight = Lighting.SampleLight(it.TileX + 0.5, it.TileY + 0.5, out float ir, out float ig, out float ib);
                int itemFillArgb = Lighting.ShadeArgb(it.TintColor, itemLight, ir, ig, ib, y_cam);
                int itemOutlineArgb = Lighting.ShadeArgb(Color.Black, itemLight, ir, ig, ib, y_cam);
                var kind = it.Kind;
                var fillC = Color.FromArgb(itemFillArgb);
                var outC = Color.FromArgb(itemOutlineArgb);

                renderables.Add((y_cam, () => DrawItemSprite(g, sx, sy, sw, sh, kind, fillC, outC)));
            }

            // Large Objects (props, fixtures, trees, pillars, stairs)
            if (CurrentLevel != null && CurrentLevel.LargeObjects.Count > 0)
            {
                for (int i = 0; i < CurrentLevel.LargeObjects.Count; i++)
                {
                    var obj = CurrentLevel.LargeObjects[i];
                    double wx = obj.TileX + 0.5;
                    double wy = obj.TileY + 0.5;

                    double dxTiles = wx - PlayerX;
                    double dyTiles = wy - PlayerY;
                    double approxAhead = (cosA * dxTiles + sinA * dyTiles);
                    if (approxAhead <= 0) continue;
                    if (approxAhead > MaxItemTilesAhead + 3.0) continue;

                    double dxw = wx - PlayerX;
                    double dyw = wy - PlayerY;
                    double x_cam = -sinA * dxw + cosA * dyw;
                    double y_cam = cosA * dxw + sinA * dyw;
                    if (y_cam <= 0.05) continue;

                    double wallH = (TileSize / y_cam) * projDist * VerticalScale;
                    float scale = obj.CustomVectorSprite != null ? obj.CustomVectorSprite.BaseScale : 1.0f;
                    double objH = wallH * scale;
                    double objW = objH;

                    double screenX = (w * 0.5) + (x_cam / y_cam) * projDist;
                    double yBotWall = horizon + (wallH * 0.5);
                    double yTopObj = yBotWall - objH;

                    int x0 = (int)Math.Floor(screenX - objW * 0.5);
                    int x1 = (int)Math.Ceiling(screenX + objW * 0.5);
                    if (x1 < 0 || x0 >= w) continue;

                    int cx0 = Math.Max(0, x0);
                    int cx1 = Math.Min(w - 1, x1);
                    double minWallDepth = double.PositiveInfinity;
                    for (int cx = cx0; cx <= cx1; cx++)
                        if (depthBuf[cx] < minWallDepth) minWallDepth = depthBuf[cx];

                    if (y_cam >= minWallDepth - 1e-6) continue;

                    float sx = (float)(screenX - objW * 0.5);
                    float sy = (float)(yTopObj);
                    float sw = (float)(objW);
                    float sh = (float)(objH);

                    float objLight = Lighting.SampleLight(wx, wy, out float or, out float og, out float ob);
                    var vSprite = obj.CustomVectorSprite;

                    renderables.Add((y_cam, () =>
                    {
                        if (vSprite != null)
                        {
                            Func<Color, Color> shader = baseC =>
                            {
                                int argb = Lighting.ShadeArgb(baseC, objLight, or, og, ob, y_cam);
                                return Color.FromArgb(baseC.A, Color.FromArgb(argb));
                            };
                            vSprite.Draw(g, sx, sy, sw, sh, shader);
                        }
                    }));
                }
            }

            // Sort far -> near and render
            renderables.Sort((a, b) => b.ycam.CompareTo(a.ycam));
            for (int i = 0; i < renderables.Count; i++)
            {
                renderables[i].draw();
            }
        }

        // Nudge the point (wx,wy) along forward direction (fx,fy) by up to maxPush,
        // but clamp so the result remains strictly inside [tx,tx+1]x[ty,ty+1] (with a small margin).
        private static void NudgeTowardFarEdgeInTile(
            int tx, int ty,
            double fx, double fy,
            double maxPush,
            ref double wx, ref double wy)
        {
            const double eps = 0.001; // margin so we never land exactly on an edge

            // Compute the largest t so that wx+fx*t, wy+fy*t stays in the tile box.
            double tMax = double.PositiveInfinity;

            if (Math.Abs(fx) > 1e-9)
            {
                if (fx > 0)
                    tMax = Math.Min(tMax, ((tx + 1 - eps) - wx) / fx);
                else
                    tMax = Math.Min(tMax, ((tx + eps) - wx) / fx);
            }
            // else fx == 0 → no x constraint

            if (Math.Abs(fy) > 1e-9)
            {
                if (fy > 0)
                    tMax = Math.Min(tMax, ((ty + 1 - eps) - wy) / fy);
                else
                    tMax = Math.Min(tMax, ((ty + eps) - wy) / fy);
            }
            // else fy == 0 → no y constraint

            if (double.IsInfinity(tMax) || tMax <= 0) return;

            double t = Math.Max(0.0, Math.Min(maxPush, tMax));
            wx += fx * t;
            wy += fy * t;
        }

        private bool CooldownReady()
        {
            return DateTime.UtcNow >= _nextMoveTime;
        }
        private void StartCooldown()
        {
            _nextMoveTime = DateTime.UtcNow.AddSeconds(StepCooldownSeconds);
        }

        private static double Wrap01(double x) { x -= Math.Floor(x); return x; }

        // Core step: attempt to move to (cx+dx, cy+dy). If free, place at center.
        public bool TryStepByCells(int dx, int dy)
        {
            if (!CooldownReady()) return false;

            GetPlayerCell(out int cx, out int cy);
            int nx = cx + dx;
            int ny = cy + dy;

            // Check transition in destination tile
            if (CurrentLevel != null && nx >= 0 && ny >= 0 && nx < CurrentLevel.Width && ny < CurrentLevel.Height)
            {
                var trans = CurrentLevel.Tiles[ny, nx].Transition;
                if (trans != null)
                {
                    if (trans.BlocksMovement)
                    {
                        string lockHint = trans.IsLocked ? " (Locked)" : "";
                        OnGameMessage?.Invoke($"{trans.Name} is closed{lockHint}. Press Space to interact.");
                        return false;
                    }
                    else if (trans.AutoTransitionOnStep)
                    {
                        PlayerX = nx + 0.5;
                        PlayerY = ny + 0.5;
                        StartCooldown();
                        InteractWithTransition(trans);
                        return true;
                    }
                }
            }

            // block if a wall OR a non-pass-thru item is in that tile
            if (IsCellBlocked(nx, ny) || HasBlockingItemAt(nx, ny)) return false;

            PlayerX = nx + 0.5;
            PlayerY = ny + 0.5;

            if (AnimateTextureOnStep)
            {
                TexPhaseU = Wrap01(TexPhaseU + PhaseStepU);
                TexPhaseV = Wrap01(TexPhaseV + PhaseStepV);
            }


            StartCooldown();
            return true;
        }

        // shortest signed delta in (-π..π]
        private static double ShortestAngleDelta(double from, double to)
        {
            double d = NormalizeAngle(to - from);
            if (d > Math.PI) d -= 2.0 * Math.PI;
            return d;
        }

        public void BeginQuarterTurn(int stepsCW)
        {
            if (_isTurning || stepsCW == 0) return;

            // Target facing
            int f = ((int)_facing + stepsCW) % 4;
            if (f < 0) f += 4;
            var facingTarget = (Cardinal)f;

            _turnStart = PlayerAngle;                         // current (wrapped)
                                                              // Compute the shortest signed delta, then store an *unwrapped* absolute target
            double delta = ShortestAngleDelta(_turnStart, AngleOfFacing(facingTarget));
            _turnTarget = _turnStart + delta;                  // unwrapped target (no wrap jumps)
            _turnElapsed = 0.0;
            _isTurning = true;
        }
        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        public void AdvanceTurnTween(double dt)
        {
            if (!_isTurning) return;

            _turnElapsed += dt;
            double t = _turnElapsed / _turnDuration;
            if (t >= 1.0)
            {
                PlayerAngle = NormalizeAngle(_turnTarget);
                _isTurning = false;
                _facing = FacingFromAngle(PlayerAngle);
                RotationCenter = PlayerAngle;
                return;
            }

            // Smooth easing (choose one)
            // 1) Smoothstep:
            // t = t * t * (3 - 2 * t);

            // 2) Sine ease in/out (usually feels a touch smoother for long tweens):
            t = 0.5 - 0.5 * Math.Cos(Math.PI * t);

            // Interpolate between unwrapped start/target, then wrap once
            double a = Lerp(_turnStart, _turnTarget, t);
            PlayerAngle = NormalizeAngle(a);
        }


        // Convenience relative to facing
        public bool TryStepForward()
        {
            int dx, dy; FacingOffsets(_facing, out dx, out dy);
            return TryStepByCells(dx, dy);
        }
        public bool TryStepBackward()
        {
            int dx, dy; FacingOffsets(_facing, out dx, out dy);
            return TryStepByCells(-dx, -dy);
        }
        public bool TryStrafeLeft()
        {
            int dx, dy; FacingOffsets(_facing, out dx, out dy);
            // Left is 90° CCW from facing: (dx,dy) -> (dy, -dx)
            return TryStepByCells(dy, -dx);
        }

        public bool TryStrafeRight()
        {
            int dx, dy; FacingOffsets(_facing, out dx, out dy);
            // Right is 90° CW from facing: (dx,dy) -> (-dy, dx)
            return TryStepByCells(-dy, dx);
        }

        private static bool IsAdjacent(int dx, int dy) => (Math.Abs(dx) + Math.Abs(dy)) == 1;

        // For an adjacent tile at (dx,dy) relative to player cell, return the two “near” quadrants.
        private static bool IsNearQuadrantForAdj(int dx, int dy, Quadrant q)
        {
            // Map coords: Q1=TL, Q2=TR, Q3=BL, Q4=BR (y grows downward)
            if (dx == +1 && dy == 0)  // tile is East → near = left side of that tile
                return q == Quadrant.Q1 || q == Quadrant.Q3;
            if (dx == -1 && dy == 0)  // West → near = right side
                return q == Quadrant.Q2 || q == Quadrant.Q4;
            if (dx == 0 && dy == -1)  // North → near = bottom side
                return q == Quadrant.Q3 || q == Quadrant.Q4;
            if (dx == 0 && dy == +1)  // South → near = top side
                return q == Quadrant.Q1 || q == Quadrant.Q2;
            return false;
        }

        // Core proximity rule: same tile → ok; adjacent tile → only near quadrants; else false.
        private bool IsInteractInRange(InanimateItem it)
        {
            int pcx = (int)Math.Floor(PlayerX);
            int pcy = (int)Math.Floor(PlayerY);
            int dx = it.TileX - pcx;
            int dy = it.TileY - pcy;

            if (dx == 0 && dy == 0) return true;                    // same tile: all quadrants
            if (!IsAdjacent(dx, dy)) return false;                  // only 4-neighbors allowed
            return IsNearQuadrantForAdj(Math.Sign(dx), Math.Sign(dy), it.Quad);
        }

        private void BuildDepthBuffer(Size size, double[] depthBuf, out double projDist)
        {
            int w = size.Width;
            projDist = (w * 0.5) / Math.Tan(FOV * 0.5);

            for (int x = 0; x < w; x++)
            {
                double cameraX = ((x + 0.5) / (double)w) * 2.0 - 1.0;
                double rayAngle = PlayerAngle + Math.Atan(cameraX * Math.Tan(FOV * 0.5));

                RayHit hit;
                CastRayDDA(PlayerX, PlayerY, rayAngle, out hit);
                if (!hit.Hit) { depthBuf[x] = double.PositiveInfinity; continue; }

                double delta = NormalizeAngle(rayAngle - PlayerAngle);
                double correctedDist = hit.Distance * Math.Cos(delta);
                if (correctedDist < 1e-4) correctedDist = 1e-4;

                depthBuf[x] = correctedDist;
            }
        }

        private bool ProjectItemAndTestOcclusion(
    InanimateItem it, Size size, double projDist, double[] depthBuf,
    out RectangleF spriteRect, out double y_cam_out)
        {
            int w = size.Width, h = size.Height;
            int horizon = h / 2;

            // Sub-tile world position
            QuadOffset(it.Quad, out double offx, out double offy);
            double wx = it.TileX + 0.5 + offx;
            double wy = it.TileY + 0.5 + offy;

            // (Optional) reuse your proximity nudge if you added it:
            // double fx = Math.Cos(PlayerAngle), fy = Math.Sin(PlayerAngle);
            // NudgeTowardFarEdgeInTile(it.TileX, it.TileY, fx, fy, desiredPush, ref wx, ref wy);

            // Camera space
            double cosA = Math.Cos(PlayerAngle), sinA = Math.Sin(PlayerAngle);
            double dxw = wx - PlayerX, dyw = wy - PlayerY;
            double x_cam = -sinA * dxw + cosA * dyw;   // horizontal on screen
            double y_cam = cosA * dxw + sinA * dyw;   // forward depth
            y_cam_out = y_cam;

            if (y_cam <= 0.01) { spriteRect = RectangleF.Empty; return false; }

            // Size & ground alignment
            double wallH = (TileSize / y_cam) * projDist * VerticalScale;
            double itemH = Math.Max(3.0, wallH * ItemSpriteScale);

            double screenX = (w * 0.5) + (x_cam / y_cam) * projDist;
            double yBotWall = horizon + (wallH * 0.5);
            double yTopItem = yBotWall - itemH;

            float sx = (float)(screenX - itemH * 0.5);
            float sy = (float)(yTopItem);
            float sw = (float)(itemH);
            float sh = (float)(itemH);
            spriteRect = new RectangleF(sx, sy, sw, sh);

            // Coarse occlusion vs wall depths across the sprite span
            int x0 = (int)Math.Floor(sx), x1 = (int)Math.Ceiling(sx + sw);
            int wmin = Math.Max(0, Math.Min(w - 1, x0));
            int wmax = Math.Max(0, Math.Min(w - 1, x1));
            if (wmax < wmin) return false;

            int visibleCols = 0, totalCols = Math.Max(1, wmax - wmin + 1);
            for (int cx = wmin; cx <= wmax; cx++)
                if (y_cam < depthBuf[cx] - 1e-3) visibleCols++;

            // Require some fraction visible to be clickable (40% works well)
            //return visibleCols >= Math.Max(1, (int)Math.Ceiling(totalCols * 0.4));

            // For clicking, a single visible column is enough
            return visibleCols >= 1;

        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            TryPickItemAt(e.Location);
        }

        public event Action<string> OnGameMessage;

        private void InteractWithItem(InanimateItem item)
        {
            if (item == null) return;

            var inst = item.SourceInstance;
            if (inst != null)
            {
                // 1. Locked check
                if (inst.IsLocked)
                {
                    int keyIdx = PlayerInventory.FindIndex(inv => string.Equals(inv.Definition.Id, inst.RequiredKey, StringComparison.OrdinalIgnoreCase));
                    if (keyIdx >= 0)
                    {
                        inst.IsLocked = false;
                        OnGameMessage?.Invoke($"Unlocked {inst.Definition.Name} with {PlayerInventory[keyIdx].Definition.Name}!");
                    }
                    else
                    {
                        OnGameMessage?.Invoke($"{inst.Definition.Name} is locked! Requires {inst.RequiredKey}.");
                        return;
                    }
                }

                // 2. Trapped check
                if (inst.IsTrapped)
                {
                    int dmg = 15;
                    HP = Math.Max(0, HP - dmg);
                    OnGameMessage?.Invoke($"TRAP TRIGGERED! {inst.TrapType} sprang, dealing {dmg} damage!");
                    inst.IsTrapped = false;
                }

                // 3. Container vs Collectable
                if (inst.IsContainer)
                {
                    OnGameMessage?.Invoke($"Opened {inst.Definition.Name}. (Container contents: {inst.ContainerContents.Count} items)");
                    return;
                }

                if (inst.Definition.Collectable)
                {
                    PlayerInventory.Add(inst);
                    if (CurrentLevel != null)
                    {
                        int qIdx = (int)item.Quad - 1;
                        CurrentLevel.Tiles[item.TileY, item.TileX].TakeItem(qIdx);
                    }

                    _items.Remove(item);
                    _occupancy.Remove(OccKey(item.TileX, item.TileY, item.Quad));
                    OnGameMessage?.Invoke($"Picked up {inst.Definition.Name}. Inventory count: {PlayerInventory.Count}");
                }
                else
                {
                    OnGameMessage?.Invoke($"{inst.Definition.Name} cannot be picked up.");
                }
            }
            else
            {
                // Simple placeholder item
                _items.Remove(item);
                _occupancy.Remove(OccKey(item.TileX, item.TileY, item.Quad));
                OnGameMessage?.Invoke($"Picked up {item.Kind}.");
            }
            Invalidate();
        }

        public event Action<TransitionObject> OnTransitionRequested;

        public void SetPlayerFacing(Cardinal facing)
        {
            _facing = facing;
            PlayerAngle = AngleOfFacing(_facing);
            RotationCenter = PlayerAngle;
            Invalidate();
        }

        public void InteractWithTransition(TransitionObject trans)
        {
            if (trans == null) return;
            var res = trans.TryInteract(PlayerInventory);
            if (!string.IsNullOrEmpty(res.Message))
            {
                OnGameMessage?.Invoke(res.Message);
            }

            if (res.TransitionTriggered)
            {
                OnTransitionRequested?.Invoke(trans);
            }
            Invalidate();
        }

        public bool TryInteractWithFacingItem()
        {
            // 1. Check facing tile for a TransitionObject (e.g. Door, Gate, Stairs)
            GetPlayerCell(out int pcx, out int pcy);
            FacingOffsets(_facing, out int fdx, out int fdy);
            int fx = pcx + fdx;
            int fy = pcy + fdy;

            if (CurrentLevel != null && fx >= 0 && fy >= 0 && fx < CurrentLevel.Width && fy < CurrentLevel.Height)
            {
                var trans = CurrentLevel.Tiles[fy, fx].Transition;
                if (trans != null)
                {
                    InteractWithTransition(trans);
                    return true;
                }
            }

            // 2. Check current tile for standing TransitionObject (e.g. Stairs, Pit)
            if (CurrentLevel != null && pcx >= 0 && pcy >= 0 && pcx < CurrentLevel.Width && pcy < CurrentLevel.Height)
            {
                var curTrans = CurrentLevel.Tiles[pcy, pcx].Transition;
                if (curTrans != null && curTrans.HasDestination())
                {
                    InteractWithTransition(curTrans);
                    return true;
                }
            }

            // 3. Inanimate Items (loot, chests, potions, keys)
            // Build depth buffer for current frame
            int w = Math.Max(1, this.ClientSize.Width);
            var depth = new double[w];
            BuildDepthBuffer(this.ClientSize, depth, out double projDist);

            InanimateItem bestItem = null;
            double bestDepth = double.MaxValue;

            foreach (var it in _items)
            {
                if (!IsInteractInRange(it)) continue;
                if (ProjectItemAndTestOcclusion(it, this.ClientSize, projDist, depth, out var rect, out var y_cam))
                {
                    if (y_cam < bestDepth)
                    {
                        bestDepth = y_cam;
                        bestItem = it;
                    }
                }
            }

            if (bestItem != null)
            {
                InteractWithItem(bestItem);
                return true;
            }
            return false;
        }

        private void TryPickItemAt(Point p)
        {
            // Build depth buffer for current frame
            int w = Math.Max(1, this.ClientSize.Width);
            var depth = new double[w];
            BuildDepthBuffer(this.ClientSize, depth, out double projDist);

            InanimateItem bestItem = null;
            double bestDepth = double.MaxValue;

            // Scan all items, keep the nearest one that: (a) is interactable by your rule,
            // (b) is visible (passes occlusion), and (c) the click hits its sprite rect.
            foreach (var it in _items)
            {
                if (!IsInteractInRange(it)) continue;

                if (ProjectItemAndTestOcclusion(it, this.ClientSize, projDist, depth, out var rect, out var y_cam))
                {
                    if (rect.Contains(p) && y_cam < bestDepth)
                    {
                        bestDepth = y_cam;
                        bestItem = it;
                    }
                }
            }

            if (bestItem != null)
            {
                InteractWithItem(bestItem);
            }
        }


        //WALLS
        // Toggle if you want to hide/show torches
        public bool ShowTorchesOnWalls = true;

        // Draw a tiny torch/sconce sprite in screen space.
        // (x,y) is the *top-left* of the sprite rectangle.
        private void DrawTorchSprite(Graphics g, float x, float y, float w, float h)
        {
            // Clamp minimum size so it stays visible
            if (w < 6f) w = 6f;
            if (h < 10f) h = 10f;

            // Layout proportions
            float flameH = h * 0.55f;
            float stemH = h - flameH;
            float stemW = w * 0.22f;
            float bracket = w * 0.60f; // horizontal bracket width at top of stem

            float cx = x + w * 0.5f;

            using (var pen = new Pen(Color.Black, 2f))
            using (var fillFlame = new SolidBrush(Color.White)) // FromArgb(210, 210, 210))) // light gray
            using (var fillStem = new SolidBrush(Color.FromArgb(180, 180, 180))) // darker gray
            {
                // --- Flame (jagged triangle-ish polygon) ---
                float tipX = cx;
                float tipY = y;
                float baseY = y + flameH;
                float halfW = w * 0.32f; // horizontal spread of flame

                // Slight anti-alias just for the flame; restore after
                var oldSmooth = g.SmoothingMode;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Build a closed polygon that has small serrations along the sides
                PointF[] flame = new PointF[]
                {
                    new PointF(tipX,                       tipY),                   // tip
                    new PointF(tipX + halfW * 0.55f,       y + flameH * 0.22f),     // right upper
                    new PointF(tipX + halfW * 0.25f,       y + flameH * 0.42f),     // right indent
                    new PointF(tipX + halfW * 0.85f,       y + flameH * 0.62f),     // right bulge
                    new PointF(tipX + halfW,               baseY),                  // right base
                    new PointF(tipX,                       baseY - flameH * 0.06f), // base slight dip
                    new PointF(tipX - halfW,               baseY),                  // left base
                    new PointF(tipX - halfW * 0.85f,       y + flameH * 0.62f),     // left bulge
                    new PointF(tipX - halfW * 0.25f,       y + flameH * 0.42f),     // left indent
                    new PointF(tipX - halfW * 0.55f,       y + flameH * 0.22f),     // left upper
                                                                                    // back to tip is implied by Fill/DrawPolygon
                };

                g.FillPolygon(fillFlame, flame);
                g.DrawPolygon(pen, flame);

                // restore smoothing mode
                g.SmoothingMode = oldSmooth;


                // --- Stem (rectangle) ---
                float sx = cx - stemW * 0.5f;
                float sy = y + flameH;
                float sw = stemW;
                float sh = stemH * 0.80f;
                g.FillRectangle(fillStem, sx, sy, sw, sh);
                g.DrawRectangle(pen, sx, sy, sw, sh);

                // --- Bracket at top of stem (little horizontal bar) ---
                float bx = cx - bracket * 0.5f;
                float by = sy;
                g.DrawLine(pen, bx, by, bx + bracket, by);

                // --- Small base cap at bottom ---
                float capW = sw * 1.3f;
                float capH = Math.Max(2f, sh * 0.18f);
                float cx2 = cx - capW * 0.5f;
                float cy2 = sy + sh - capH;
                g.FillRectangle(fillStem, cx2, cy2, capW, capH);
                g.DrawRectangle(pen, cx2, cy2, capW, capH);
            }
        }


    }
}
