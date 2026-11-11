using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GridLocked;

namespace GridLockedEditor
{
    public enum PainterTool
    {
        Select,
        Wall,
        Floor,
        Spawn,
        Door,
        StairsDown,
        StairsUp,
        Void,
        CustomObject,
        ArchetypeBrush
    }

    public sealed class MapPainterControl : UserControl
    {
        public DungeonLevel Level { get; private set; }
        public GameProject Project { get; set; }
        public PainterTool CurrentTool { get; set; } = PainterTool.Select;
        public string ActiveObjectId { get; set; } = "tree_oak";
        public TileArchetype ActiveArchetype { get; set; }

        public int CellSize { get; set; } = 32;
        public int HoverX { get; private set; } = -1;
        public int HoverY { get; private set; } = -1;
        public int SelectedX { get; private set; } = -1;
        public int SelectedY { get; private set; } = -1;

        public event Action<int, int, DungeonTile> OnTileSelected;
        public event Action OnMapModified;
        public event Action OnUndoStateChanged;

        private bool _isMouseDown;
        private MapUndoStep _currentStroke;
        private readonly System.Collections.Generic.Stack<MapUndoStep> _undoStack = new System.Collections.Generic.Stack<MapUndoStep>();
        private readonly System.Collections.Generic.Stack<MapUndoStep> _redoStack = new System.Collections.Generic.Stack<MapUndoStep>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public MapPainterControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.FromArgb(32, 32, 32);
        }

        public void ClearUndoHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentStroke = null;
            OnUndoStateChanged?.Invoke();
        }

        public void SelectTile(int tx, int ty)
        {
            if (Level == null || tx < 0 || ty < 0 || tx >= Level.Width || ty >= Level.Height) return;
            SelectedX = tx;
            SelectedY = ty;
            OnTileSelected?.Invoke(tx, ty, Level.Tiles[ty, tx]);
            Invalidate();
        }

        public void SetLevel(DungeonLevel level)
        {
            Level = level;
            SelectedX = -1;
            SelectedY = -1;
            HoverX = -1;
            HoverY = -1;
            ClearUndoHistory();
            Invalidate();
        }

        public bool Undo()
        {
            if (_undoStack.Count == 0 || Level == null) return false;
            var step = _undoStack.Pop();

            Level.SpawnX = step.SpawnXBefore;
            Level.SpawnY = step.SpawnYBefore;

            foreach (var snap in step.Before.Values)
            {
                if (snap.X >= 0 && snap.X < Level.Width && snap.Y >= 0 && snap.Y < Level.Height)
                {
                    var tile = Level.Tiles[snap.Y, snap.X];
                    snap.Restore(tile);
                    Level.LargeObjects.RemoveAll(xObj => xObj.TileX == snap.X && xObj.TileY == snap.Y);
                    if (snap.CenterOccupant is LargeObjectInstance lObj)
                    {
                        Level.LargeObjects.Add(lObj);
                    }
                }
            }

            _redoStack.Push(step);
            OnMapModified?.Invoke();
            OnUndoStateChanged?.Invoke();
            Invalidate();
            return true;
        }

        public bool Redo()
        {
            if (_redoStack.Count == 0 || Level == null) return false;
            var step = _redoStack.Pop();

            Level.SpawnX = step.SpawnXAfter;
            Level.SpawnY = step.SpawnYAfter;

            foreach (var snap in step.After.Values)
            {
                if (snap.X >= 0 && snap.X < Level.Width && snap.Y >= 0 && snap.Y < Level.Height)
                {
                    var tile = Level.Tiles[snap.Y, snap.X];
                    snap.Restore(tile);
                    Level.LargeObjects.RemoveAll(xObj => xObj.TileX == snap.X && xObj.TileY == snap.Y);
                    if (snap.CenterOccupant is LargeObjectInstance lObj)
                    {
                        Level.LargeObjects.Add(lObj);
                    }
                }
            }

            _undoStack.Push(step);
            OnMapModified?.Invoke();
            OnUndoStateChanged?.Invoke();
            Invalidate();
            return true;
        }

        public void RecordSingleTileEdit(int x, int y, Action editAction)
        {
            if (Level == null || x < 0 || y < 0 || x >= Level.Width || y >= Level.Height)
            {
                editAction?.Invoke();
                return;
            }

            var step = new MapUndoStep
            {
                SpawnXBefore = Level.SpawnX,
                SpawnYBefore = Level.SpawnY
            };
            var tile = Level.Tiles[y, x];
            long key = ((long)x << 32) | (uint)y;
            step.Before[key] = new TileSnapshot(tile);

            editAction?.Invoke();

            step.SpawnXAfter = Level.SpawnX;
            step.SpawnYAfter = Level.SpawnY;
            step.After[key] = new TileSnapshot(tile);

            _undoStack.Push(step);
            _redoStack.Clear();
            OnUndoStateChanged?.Invoke();
            OnMapModified?.Invoke();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Level == null) return;

            int tx = e.X / CellSize;
            int ty = e.Y / CellSize;

            if (tx >= 0 && tx < Level.Width && ty >= 0 && ty < Level.Height)
            {
                if (HoverX != tx || HoverY != ty)
                {
                    HoverX = tx;
                    HoverY = ty;
                    Invalidate();
                }

                if (_isMouseDown && CurrentTool != PainterTool.Select)
                {
                    ApplyToolAt(tx, ty, e.Button == MouseButtons.Right);
                }
            }
            else
            {
                if (HoverX != -1 || HoverY != -1)
                {
                    HoverX = -1;
                    HoverY = -1;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Level == null) return;

            _isMouseDown = true;
            int tx = e.X / CellSize;
            int ty = e.Y / CellSize;

            if (tx >= 0 && tx < Level.Width && ty >= 0 && ty < Level.Height)
            {
                if (CurrentTool == PainterTool.Select)
                {
                    SelectedX = tx;
                    SelectedY = ty;
                    OnTileSelected?.Invoke(tx, ty, Level.Tiles[ty, tx]);
                    Invalidate();
                    return;
                }

                _currentStroke = new MapUndoStep
                {
                    SpawnXBefore = Level.SpawnX,
                    SpawnYBefore = Level.SpawnY
                };
                ApplyToolAt(tx, ty, e.Button == MouseButtons.Right);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isMouseDown = false;
            FinishCurrentStroke();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoverX = -1;
            HoverY = -1;
            if (_isMouseDown)
            {
                _isMouseDown = false;
                FinishCurrentStroke();
            }
            Invalidate();
        }

        private void FinishCurrentStroke()
        {
            if (_currentStroke != null)
            {
                if (_currentStroke.Before.Count > 0 && Level != null)
                {
                    _currentStroke.SpawnXAfter = Level.SpawnX;
                    _currentStroke.SpawnYAfter = Level.SpawnY;
                    foreach (var kvp in _currentStroke.Before)
                    {
                        int sx = (int)(kvp.Key >> 32);
                        int sy = (int)(kvp.Key & 0xFFFFFFFF);
                        if (sx >= 0 && sx < Level.Width && sy >= 0 && sy < Level.Height)
                        {
                            _currentStroke.After[kvp.Key] = new TileSnapshot(Level.Tiles[sy, sx]);
                        }
                    }
                    _undoStack.Push(_currentStroke);
                    _redoStack.Clear();
                    OnUndoStateChanged?.Invoke();
                }
                _currentStroke = null;
            }
        }

        private void ApplyToolAt(int x, int y, bool erase)
        {
            if (Level == null || x < 0 || y < 0 || x >= Level.Width || y >= Level.Height) return;
            if (CurrentTool == PainterTool.Select) return;

            var tile = Level.Tiles[y, x];

            if (_currentStroke != null)
            {
                long key = ((long)x << 32) | (uint)y;
                if (!_currentStroke.Before.ContainsKey(key))
                {
                    _currentStroke.Before[key] = new TileSnapshot(tile);
                }
            }

            if (CurrentTool == PainterTool.CustomObject)
            {
                if (erase)
                {
                    tile.CenterOccupant = null;
                    Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                }
                else
                {
                    tile.Type = TileType.Floor;
                    var sprite = Project?.Objects?.Get(ActiveObjectId);
                    var obj = new LargeObjectInstance(ActiveObjectId, sprite?.Name ?? ActiveObjectId, ActiveObjectId, true, sprite)
                    {
                        TileX = x,
                        TileY = y
                    };
                    Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                    Level.LargeObjects.Add(obj);
                    tile.CenterOccupant = obj;
                }
                OnMapModified?.Invoke();
                Invalidate();
                return;
            }

            if (CurrentTool == PainterTool.ArchetypeBrush && ActiveArchetype != null)
            {
                tile.ApplyArchetype(ActiveArchetype);
                Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                Level.Transitions.RemoveAll(tr => tr.TileX == x && tr.TileY == y);
                tile.CenterOccupant = null;
                tile.Transition = null;

                if (!string.IsNullOrEmpty(ActiveArchetype.ObjectKind))
                {
                    var sprite = Project?.Objects?.Get(ActiveArchetype.ObjectKind);
                    var obj = new LargeObjectInstance(ActiveArchetype.ObjectKind, ActiveArchetype.Name, ActiveArchetype.ObjectKind, !ActiveArchetype.CanWalkThru, sprite)
                    {
                        TileX = x,
                        TileY = y
                    };
                    Level.LargeObjects.Add(obj);
                    tile.CenterOccupant = obj;
                }
                else if (ActiveArchetype.CanBeOpened && ActiveArchetype.BaseType == TileType.Door)
                {
                    var clSprite = Project?.Objects?.Get("door_closed");
                    var opSprite = Project?.Objects?.Get("door_open");
                    var dt = new DoorTransition($"door_{x}_{y}", x, y, clSprite, opSprite);
                    Level.LargeObjects.Add(dt);
                    Level.Transitions.Add(dt);
                    tile.Transition = dt;
                }
                else if (ActiveArchetype.BaseType == TileType.StairsUp)
                {
                    var sprite = Project?.Objects?.Get("stairs_up");
                    var st = new StairsTransition($"stairs_up_{x}_{y}", TransitionKind.StairsUp, x, y, Math.Max(1, Level.LevelNumber - 1), sprite);
                    Level.LargeObjects.Add(st);
                    Level.Transitions.Add(st);
                    tile.Transition = st;
                }
                else if (ActiveArchetype.BaseType == TileType.StairsDown)
                {
                    var sprite = Project?.Objects?.Get("stairs_down");
                    var st = new StairsTransition($"stairs_down_{x}_{y}", TransitionKind.StairsDown, x, y, Level.LevelNumber + 1, sprite);
                    Level.LargeObjects.Add(st);
                    Level.Transitions.Add(st);
                    tile.Transition = st;
                }
                else if (ActiveArchetype.BaseType == TileType.VoidOrPit)
                {
                    var sprite = Project?.Objects?.Get("pit");
                    var pt = new PitTransition($"pit_{x}_{y}", x, y, sprite);
                    Level.LargeObjects.Add(pt);
                    Level.Transitions.Add(pt);
                    tile.Transition = pt;
                }

                OnMapModified?.Invoke();
                Invalidate();
                return;
            }

            var newType = TileType.Floor;

            if (erase)
            {
                newType = TileType.Floor;
                tile.CenterOccupant = null;
                Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
            }
            else
            {
                switch (CurrentTool)
                {
                    case PainterTool.Wall: newType = TileType.Wall; break;
                    case PainterTool.Floor: newType = TileType.Floor; break;
                    case PainterTool.Spawn:
                        newType = TileType.Spawn;
                        Level.SpawnX = x + 0.5;
                        Level.SpawnY = y + 0.5;
                        break;
                    case PainterTool.Door: newType = TileType.Door; break;
                    case PainterTool.StairsDown: newType = TileType.StairsDown; break;
                    case PainterTool.StairsUp: newType = TileType.StairsUp; break;
                    case PainterTool.Void: newType = TileType.VoidOrPit; break;
                    default: return;
                }
            }

            if (tile.Type != newType)
            {
                tile.Type = newType;
                char defaultKey = newType == TileType.Wall ? '#' :
                                 newType == TileType.Door ? 'D' :
                                 newType == TileType.Spawn ? '@' :
                                 newType == TileType.StairsUp ? 'S' :
                                 newType == TileType.StairsDown ? 's' :
                                 newType == TileType.VoidOrPit ? '~' : '.';
                if (Project != null && Project.Legend != null)
                {
                    tile.ApplyArchetype(Project.Legend.GetArchetype(defaultKey));
                }

                if (newType == TileType.Door)
                {
                    var clSprite = Project?.Objects?.Get("door_closed");
                    var opSprite = Project?.Objects?.Get("door_open");
                    var dt = new DoorTransition($"door_{x}_{y}", x, y, clSprite, opSprite);
                    Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                    Level.Transitions.RemoveAll(tr => tr.TileX == x && tr.TileY == y);
                    Level.LargeObjects.Add(dt);
                    Level.Transitions.Add(dt);
                    tile.Transition = dt;
                }
                else if (newType == TileType.StairsUp)
                {
                    var sprite = Project?.Objects?.Get("stairs_up");
                    var st = new StairsTransition($"stairs_up_{x}_{y}", TransitionKind.StairsUp, x, y, Math.Max(1, Level.LevelNumber - 1), sprite);
                    Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                    Level.Transitions.RemoveAll(tr => tr.TileX == x && tr.TileY == y);
                    Level.LargeObjects.Add(st);
                    Level.Transitions.Add(st);
                    tile.Transition = st;
                }
                else if (newType == TileType.StairsDown)
                {
                    var sprite = Project?.Objects?.Get("stairs_down");
                    var st = new StairsTransition($"stairs_down_{x}_{y}", TransitionKind.StairsDown, x, y, Level.LevelNumber + 1, sprite);
                    Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                    Level.Transitions.RemoveAll(tr => tr.TileX == x && tr.TileY == y);
                    Level.LargeObjects.Add(st);
                    Level.Transitions.Add(st);
                    tile.Transition = st;
                }
                else if (newType == TileType.VoidOrPit)
                {
                    var sprite = Project?.Objects?.Get("pit");
                    var pt = new PitTransition($"pit_{x}_{y}", x, y, sprite);
                    Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                    Level.Transitions.RemoveAll(tr => tr.TileX == x && tr.TileY == y);
                    Level.LargeObjects.Add(pt);
                    Level.Transitions.Add(pt);
                    tile.Transition = pt;
                }
                else if (newType == TileType.Wall || newType == TileType.Floor)
                {
                    tile.CenterOccupant = null;
                    tile.Transition = null;
                    Level.LargeObjects.RemoveAll(lo => lo.TileX == x && lo.TileY == y);
                    Level.Transitions.RemoveAll(tr => tr.TileX == x && tr.TileY == y);
                }

                OnMapModified?.Invoke();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (Level == null)
            {
                using (var b = new SolidBrush(Color.Gray))
                    g.DrawString("No level loaded. Open or create a project.", Font, b, 20, 20);
                return;
            }

            int w = Level.Width;
            int h = Level.Height;
            int cs = CellSize;

            using (var wallBrush = new SolidBrush(Color.FromArgb(70, 70, 75)))
            using (var floorBrush = new SolidBrush(Color.FromArgb(215, 210, 195)))
            using (var doorBrush = new SolidBrush(Color.FromArgb(140, 85, 45)))
            using (var stairsDownBrush = new SolidBrush(Color.FromArgb(40, 90, 160)))
            using (var stairsUpBrush = new SolidBrush(Color.FromArgb(40, 160, 90)))
            using (var voidBrush = new SolidBrush(Color.FromArgb(10, 10, 10)))
            using (var spawnBrush = new SolidBrush(Color.FromArgb(50, 180, 210)))
            using (var gridPen = new Pen(Color.FromArgb(40, Color.Black), 1f))
            using (var subSquarePen = new Pen(Color.FromArgb(40, Color.Gray), 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
            using (var font = new Font(FontFamily.GenericSansSerif, 7f))
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var tile = Level.Tiles[y, x];
                        int px = x * cs;
                        int py = y * cs;

                        // 1. Base Tile Background
                        Brush b = floorBrush;
                        switch (tile.Type)
                        {
                            case TileType.Wall: b = wallBrush; break;
                            case TileType.Door: b = doorBrush; break;
                            case TileType.StairsDown: b = stairsDownBrush; break;
                            case TileType.StairsUp: b = stairsUpBrush; break;
                            case TileType.VoidOrPit: b = voidBrush; break;
                            case TileType.Spawn: b = spawnBrush; break;
                        }
                        g.FillRectangle(b, px, py, cs, cs);
                        g.DrawRectangle(gridPen, px, py, cs, cs);

                        // 2. Sub-square divider lines for floors
                        if (tile.Type != TileType.Wall && tile.Type != TileType.VoidOrPit)
                        {
                            g.DrawLine(subSquarePen, px + cs / 2, py, px + cs / 2, py + cs);
                            g.DrawLine(subSquarePen, px, py + cs / 2, px + cs, py + cs / 2);

                            // 3. Draw Sub-Square Items
                            for (int s = 0; s < 4; s++)
                            {
                                var item = tile.Slots[s];
                                if (item != null)
                                {
                                    int qx = px + ((s % 2 == 1) ? cs / 2 : 0);
                                    int qy = py + ((s >= 2) ? cs / 2 : 0);
                                    int itemSize = cs / 2 - 4;

                                    using (var itemBrush = new SolidBrush(item.Definition.Color))
                                    using (var itemPen = new Pen(Color.Black, 1f))
                                    {
                                        if (item.Definition.SpriteKind.Equals("potion", StringComparison.OrdinalIgnoreCase))
                                        {
                                            g.FillEllipse(itemBrush, qx + 2, qy + 2, itemSize, itemSize);
                                            g.DrawEllipse(itemPen, qx + 2, qy + 2, itemSize, itemSize);
                                        }
                                        else if (item.Definition.SpriteKind.Equals("chest", StringComparison.OrdinalIgnoreCase))
                                        {
                                            g.FillRectangle(itemBrush, qx + 2, qy + 2, itemSize, itemSize);
                                            g.DrawRectangle(itemPen, qx + 2, qy + 2, itemSize, itemSize);
                                        }
                                        else
                                        {
                                            g.FillEllipse(itemBrush, qx + 2, qy + 2, itemSize, itemSize);
                                            g.DrawEllipse(itemPen, qx + 2, qy + 2, itemSize, itemSize);
                                        }
                                    }
                                }
                            }

                            // 4. Center Occupant (Monster / NPC / Large Object)
                            if (tile.CenterOccupant != null)
                            {
                                int occSize = cs / 2;
                                int ox = px + (cs - occSize) / 2;
                                int oy = py + (cs - occSize) / 2;

                                if (tile.CenterOccupant is MonsterInstance m)
                                {
                                    Color baseCol = m.IsUnique ? Color.Crimson : Color.OrangeRed;
                                    using (var mBrush = new SolidBrush(baseCol))
                                    using (var mPen = new Pen(m.IsUnique ? Color.Gold : Color.Black, m.IsUnique ? 2.5f : 1.5f))
                                    {
                                        g.FillEllipse(mBrush, ox, oy, occSize, occSize);
                                        g.DrawEllipse(mPen, ox, oy, occSize, occSize);
                                    }

                                    string badge = m.IsUnique ? $"★{m.Level}" : $"{m.Level}";
                                    using (var txtBrush = new SolidBrush(Color.White))
                                    using (var bFont = new Font(FontFamily.GenericSansSerif, 6.5f, FontStyle.Bold))
                                    {
                                        var sz = g.MeasureString(badge, bFont);
                                        g.DrawString(badge, bFont, txtBrush, ox + (occSize - sz.Width) / 2f, oy + (occSize - sz.Height) / 2f);
                                    }
                                }
                                else if (tile.CenterOccupant is LargeObjectInstance obj)
                                {
                                    using (var objBrush = new SolidBrush(Color.Goldenrod))
                                    using (var objPen = new Pen(Color.Black, 1.5f))
                                    {
                                        g.FillRectangle(objBrush, ox, oy, occSize, occSize);
                                        g.DrawRectangle(objPen, ox, oy, occSize, occSize);
                                    }
                                    char keyChar = Project?.Legend?.FindKeyFor(obj.ObjectKind, obj.ObjectKind.Length > 0 ? char.ToUpper(obj.ObjectKind[0]) : '?') ?? '?';
                                    using (var textBrush = new SolidBrush(Color.Black))
                                    using (var bFont = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold))
                                    {
                                        g.DrawString(keyChar.ToString(), bFont, textBrush, ox + 1, oy);
                                    }
                                }
                            }
                        }

                        // Tile Label if spawn, stairs, or door
                        if (tile.Type == TileType.Spawn)
                        {
                            using (var textBrush = new SolidBrush(Color.Black))
                            using (var bFont = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold))
                                g.DrawString("@", bFont, textBrush, px + 2, py + 2);
                        }
                        else if (tile.Type == TileType.StairsUp)
                        {
                            using (var textBrush = new SolidBrush(Color.White))
                            using (var bFont = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold))
                                g.DrawString("S", bFont, textBrush, px + 2, py + 2);
                        }
                        else if (tile.Type == TileType.StairsDown)
                        {
                            using (var textBrush = new SolidBrush(Color.White))
                            using (var bFont = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold))
                                g.DrawString("s", bFont, textBrush, px + 2, py + 2);
                        }
                        else if (tile.Type == TileType.Door)
                        {
                            using (var textBrush = new SolidBrush(Color.White))
                            using (var bFont = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold))
                                g.DrawString("D", bFont, textBrush, px + 2, py + 2);
                        }
                    }
                }

                // 4.5. Render Patrol Route / Wander Boundaries for Selected Monster
                MonsterInstance activeMon = null;
                if (SelectedX >= 0 && SelectedY >= 0 && SelectedX < w && SelectedY < h)
                {
                    activeMon = Level.Tiles[SelectedY, SelectedX].CenterOccupant as MonsterInstance;
                }

                if (activeMon != null && activeMon.MovementMode == EntityMovementMode.PatrolRoute && activeMon.PatrolWaypoints.Count > 0)
                {
                    var allPoints = new List<Point> { new Point(activeMon.TileX, activeMon.TileY) };
                    allPoints.AddRange(activeMon.PatrolWaypoints);

                    using (var pathPen = new Pen(Color.FromArgb(240, 255, 140, 0), 2.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    {
                        for (int i = 0; i < allPoints.Count - 1; i++)
                        {
                            var p1 = new PointF(allPoints[i].X * cs + cs / 2f, allPoints[i].Y * cs + cs / 2f);
                            var p2 = new PointF(allPoints[i + 1].X * cs + cs / 2f, allPoints[i + 1].Y * cs + cs / 2f);
                            g.DrawLine(pathPen, p1, p2);
                        }

                        if (activeMon.CircuitStyle == PatrolCircuitStyle.Loop && allPoints.Count > 2)
                        {
                            var pStart = new PointF(allPoints[0].X * cs + cs / 2f, allPoints[0].Y * cs + cs / 2f);
                            var pEnd = new PointF(allPoints[allPoints.Count - 1].X * cs + cs / 2f, allPoints[allPoints.Count - 1].Y * cs + cs / 2f);
                            g.DrawLine(pathPen, pEnd, pStart);
                        }
                    }

                    // Draw numbered waypoint badges
                    for (int i = 1; i < allPoints.Count; i++)
                    {
                        var pt = allPoints[i];
                        int wx = pt.X * cs + (cs - 16) / 2;
                        int wy = pt.Y * cs + (cs - 16) / 2;
                        using (var wpBrush = new SolidBrush(Color.FromArgb(240, 255, 140, 0)))
                        using (var wpPen = new Pen(Color.Black, 1.5f))
                        using (var txtBrush = new SolidBrush(Color.Black))
                        using (var wpFont = new Font(FontFamily.GenericSansSerif, 7f, FontStyle.Bold))
                        {
                            g.FillEllipse(wpBrush, wx, wy, 16, 16);
                            g.DrawEllipse(wpPen, wx, wy, 16, 16);
                            string num = i.ToString();
                            var sz = g.MeasureString(num, wpFont);
                            g.DrawString(num, wpFont, txtBrush, wx + (16 - sz.Width) / 2f, wy + (16 - sz.Height) / 2f);
                        }
                    }
                }
                else if (activeMon != null && activeMon.MovementMode == EntityMovementMode.Wander)
                {
                    if (activeMon.WanderAreaTiles.Count > 0)
                    {
                        using (var areaBrush = new SolidBrush(Color.FromArgb(45, 0, 206, 209)))
                        using (var areaPen = new Pen(Color.DarkTurquoise, 1.5f))
                        {
                            foreach (var pt in activeMon.WanderAreaTiles)
                            {
                                if (pt.X >= 0 && pt.X < w && pt.Y >= 0 && pt.Y < h)
                                {
                                    g.FillRectangle(areaBrush, pt.X * cs, pt.Y * cs, cs, cs);
                                    g.DrawRectangle(areaPen, pt.X * cs + 1, pt.Y * cs + 1, cs - 2, cs - 2);
                                }
                            }
                        }
                    }
                    else if (activeMon.WanderRadius > 0)
                    {
                        int r = activeMon.WanderRadius;
                        int bx = Math.Max(0, activeMon.TileX - r);
                        int by = Math.Max(0, activeMon.TileY - r);
                        int bw = Math.Min(w - 1, activeMon.TileX + r) - bx + 1;
                        int bh = Math.Min(h - 1, activeMon.TileY + r) - by + 1;

                        using (var fillBrush = new SolidBrush(Color.FromArgb(30, 0, 191, 255)))
                        using (var leashPen = new Pen(Color.DeepSkyBlue, 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                        {
                            g.FillRectangle(fillBrush, bx * cs, by * cs, bw * cs, bh * cs);
                            g.DrawRectangle(leashPen, bx * cs, by * cs, bw * cs, bh * cs);
                        }
                    }

                    // Render active path route if pathfinding is underway
                    if (activeMon.CurrentPath.Count > 0)
                    {
                        var pathPts = new List<Point> { new Point(activeMon.TileX, activeMon.TileY) };
                        pathPts.AddRange(activeMon.CurrentPath);

                        using (var wanderPathPen = new Pen(Color.Cyan, 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                        {
                            for (int i = 0; i < pathPts.Count - 1; i++)
                            {
                                var p1 = new PointF(pathPts[i].X * cs + cs / 2f, pathPts[i].Y * cs + cs / 2f);
                                var p2 = new PointF(pathPts[i + 1].X * cs + cs / 2f, pathPts[i + 1].Y * cs + cs / 2f);
                                g.DrawLine(wanderPathPen, p1, p2);
                            }
                        }

                        // Target destination badge
                        var target = pathPts[pathPts.Count - 1];
                        int tx = target.X * cs + (cs - 14) / 2;
                        int ty = target.Y * cs + (cs - 14) / 2;
                        using (var tBrush = new SolidBrush(Color.Gold))
                        using (var tPen = new Pen(Color.DarkGoldenrod, 1.5f))
                        using (var txtBrush = new SolidBrush(Color.Black))
                        using (var tFont = new Font(FontFamily.GenericSansSerif, 6.5f, FontStyle.Bold))
                        {
                            g.FillEllipse(tBrush, tx, ty, 14, 14);
                            g.DrawEllipse(tPen, tx, ty, 14, 14);
                            g.DrawString("★", tFont, txtBrush, tx + 1, ty + 1);
                        }
                    }

                    // Idle cycle indicator badge if creature is currently idling
                    if (activeMon.IdleCyclesRemaining > 0)
                    {
                        int mx = activeMon.TileX * cs + 2;
                        int my = activeMon.TileY * cs + 2;
                        using (var idleBrush = new SolidBrush(Color.FromArgb(200, 75, 0, 130)))
                        using (var idleFont = new Font(FontFamily.GenericSansSerif, 6.5f, FontStyle.Bold))
                        using (var txtBrush = new SolidBrush(Color.White))
                        {
                            g.FillRectangle(idleBrush, mx, my, 26, 12);
                            g.DrawString($"💤{activeMon.IdleCyclesRemaining}", idleFont, txtBrush, mx + 1, my);
                        }
                    }
                }

                // 5. Highlight Selected Tile
                if (SelectedX >= 0 && SelectedY >= 0 && SelectedX < w && SelectedY < h)
                {
                    using (var selPen = new Pen(Color.Cyan, 2.5f))
                        g.DrawRectangle(selPen, SelectedX * cs, SelectedY * cs, cs, cs);
                }

                // 6. Highlight Hovered Tile
                if (HoverX >= 0 && HoverY >= 0 && HoverX < w && HoverY < h)
                {
                    using (var hovPen = new Pen(Color.Yellow, 1.5f))
                        g.DrawRectangle(hovPen, HoverX * cs, HoverY * cs, cs, cs);
                }
            }
        }
    }

    /// <summary>
    /// Captures the complete state of a dungeon tile for undo/redo operations.
    /// </summary>
    public sealed class TileSnapshot
    {
        public int X { get; }
        public int Y { get; }
        public TileType Type { get; }
        public CenterEntity CenterOccupant { get; }
        public ItemInstance[] Slots { get; }

        public TileSnapshot(DungeonTile tile)
        {
            X = tile.X;
            Y = tile.Y;
            Type = tile.Type;
            CenterOccupant = tile.CenterOccupant;
            Slots = new ItemInstance[4];
            for (int i = 0; i < 4; i++) Slots[i] = tile.Slots[i];
        }

        public void Restore(DungeonTile tile)
        {
            tile.Type = Type;
            tile.CenterOccupant = CenterOccupant;
            for (int i = 0; i < 4; i++) tile.Slots[i] = Slots[i];
        }
    }

    /// <summary>
    /// Represents a single continuous paint stroke or inspector edit step.
    /// </summary>
    public sealed class MapUndoStep
    {
        public double SpawnXBefore { get; set; }
        public double SpawnYBefore { get; set; }
        public double SpawnXAfter { get; set; }
        public double SpawnYAfter { get; set; }

        public System.Collections.Generic.Dictionary<long, TileSnapshot> Before { get; } =
            new System.Collections.Generic.Dictionary<long, TileSnapshot>();
        public System.Collections.Generic.Dictionary<long, TileSnapshot> After { get; } =
            new System.Collections.Generic.Dictionary<long, TileSnapshot>();
    }
}
