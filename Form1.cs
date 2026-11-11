using System;
using System.Drawing;
using System.Windows.Forms;
using static GridLocked.RaycasterCanvas;

namespace GridLocked
{
    // Designer-friendly: matches Form1.Designer.cs partial
    public partial class Form1 : Form
    {
        private RaycasterCanvas _view;
        private Timer _logicTimer;

        // HUD you already have:
        private HudCanvas _hud;

        // ✨ NEW: map overlay field
        private MapOverlayCanvas _mapOverlay;

        // Input state
        private bool _up, _down, _left, _right, _turnL, _turnR;
        private DateTime _last = DateTime.UtcNow;

        // Tiny toolbar
        private Panel _toolbar;
        private Button _btnWireframe;
        private Button _btnTexture;
        private Button _btnBrickPreset;
        private Label _lblStatus;
        private Label _lblFov;
        private NumericUpDown _nudFov;
        // HUD layout

        private CompassControl _compass;
        private GameProject _project;

        private readonly string _modulePath;

        public Form1(string modulePath = null)
        {
            _modulePath = modulePath;
            InitializeComponent();          // keep designer wiring
            BuildRuntimeUi();               // add our controls in code
            HookInput();                    // keyboard handlers
            HookTick();                     // update loop
        }

        private void BuildRuntimeUi()
        {
            this.KeyPreview = true;

            // Raycaster (no internal HUD)
            _view = new RaycasterCanvas
            {
                Left = 10,
                Top = 0,
                Width = 816,
                Height = 489,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.None,
                ShowHud = false // IMPORTANT: HUD now drawn in separate control
            };
            Controls.Add(_view);

            // Form1.BuildRuntimeUi(), right after Controls.Add(_view);
            _mapOverlay = new MapOverlayCanvas(_view)
            {
                Visible = false,     // start hidden; toggle with Tab
                TabStop = false
            };
            Controls.Add(_mapOverlay);
            _mapOverlay.BringToFront();   // sits above the camera (and below HUD if you bring HUD to front later)



            // Drawn HUD control at bottom
            _hud = new HudCanvas
            {
                Height = 120,   // match your desired HUD height
                //Dock = DockStyle.Bottom
            };
            // After you create _hud and add it:
            _hud.TabStop = false;
            _hud.BringToFront();
            _view.TabStop = false;

            Controls.Add(_hud);

            // Layout on show/resize
            this.Shown += (s, e) => LayoutViewportWithAspect();
            this.Resize += (s, e) => LayoutViewportWithAspect();
        
        // Toolbar (top strip)
        _toolbar = new Panel
            {
                Left = 0,
                Top = 0,
                Width = this.ClientSize.Width,
                Height = 40,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = SystemColors.Control
            };
            //Controls.Add(_toolbar);

            // Hook these **here**, not inside another Shown handler:
            this.Shown += (s, e) => LayoutViewportWithAspect();
            this.Resize += (s, e) => LayoutViewportWithAspect();

            _btnWireframe = new Button { Text = "Wireframe (F)", Left = 10, Top = 7, Width = 110, Height = 26 };
            _btnWireframe.Click += (s, e) => ToggleWireframe();
            _toolbar.Controls.Add(_btnWireframe);

            _btnTexture = new Button { Text = "Texture: Brick (T)", Left = _btnWireframe.Right + 8, Top = 7, Width = 130, Height = 26 };
            _btnTexture.Click += (s, e) => CycleTexture();
            _toolbar.Controls.Add(_btnTexture);

            _btnBrickPreset = new Button { Text = "Apply Brick Preset (B)", Left = _btnTexture.Right + 8, Top = 7, Width = 155, Height = 26 };
            _btnBrickPreset.Click += (s, e) => ApplyBrickPreset();
            _toolbar.Controls.Add(_btnBrickPreset);

            _lblStatus = new Label { Left = _btnBrickPreset.Right + 12, Top = 11, AutoSize = true, Text = "" };
            _toolbar.Controls.Add(_lblStatus);

            _lblFov = new Label { Text = "FOV:", Left = _lblStatus.Right + 20, Top = 11, AutoSize = true };
            _toolbar.Controls.Add(_lblFov);

            _nudFov = new NumericUpDown
            {
                Left = _lblFov.Right + 6,
                Top = 8,
                Width = 60,
                Minimum = 30,
                Maximum = 120,
                Increment = 1,
                DecimalPlaces = 0
            };
            _nudFov.ValueChanged += (s, e) => SetFovDegrees((double)_nudFov.Value);
            _toolbar.Controls.Add(_nudFov);

            // Larger initial size (~20% bigger than 920x600)
            if (ClientSize.Width < 1104 || ClientSize.Height < 720)
                ClientSize = new Size(1104, 720);

            // Maintain aspect and lay out regions
            this.Resize += (s, e) => LayoutViewportWithAspect();


            // Init map etc on show
            this.Shown += (s, e) =>
            {
                _view.OnGameMessage += msg =>
                {
                    _lblStatus.Text = msg;
                };
                _view.OnTransitionRequested += HandleTransition;

                if (string.IsNullOrEmpty(_modulePath))
                {
                    MessageBox.Show("No game module was specified.\nPlease launch GridLocked to select an adventure module.", "No Module", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                    return;
                }

                try
                {
                    var project = GameProject.Load(_modulePath);
                    if (project != null && project.CurrentLevel != null)
                    {
                        _project = project;
                        _view.LoadLevel(project.CurrentLevel);
                        Text = $"GridLocked - {project.Title}";
                        _view.EnableCollision = true;
                        _view.LockRotation = true;
                        _view.RotationLimit = Math.PI / 2.0;

                        ApplyBrickPreset();
                        _view.WireframeBW = true;
                        UpdateStatus();

                        _nudFov.Value = ClampToNumericUpDownRange(_nudFov, RadToDeg(_view.FOV));
                        LayoutViewportWithAspect();
                        _view.Focus();
                        return;
                    }
                    else
                    {
                        MessageBox.Show($"Failed to load a playable level from:\n{_modulePath}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading module: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                }
            };
        }

        private void ToggleMapOverlay()
        {
            if (_mapOverlay == null) return;
            _mapOverlay.Visible = !_mapOverlay.Visible;
            _mapOverlay.BringToFront();
            _mapOverlay.Invalidate();
        }
        // Form1.cs (inside class Form1)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab)
            {
                ToggleMapOverlay();
                return true; // consume so it doesn't move focus
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static double RadToDeg(double r) => r * 180.0 / Math.PI;
        private static double DegToRad(double d) => d * Math.PI / 180.0;

        private decimal ClampToNumericUpDownRange(NumericUpDown nud, double value)
        {
            var v = (decimal)value;
            if (v < nud.Minimum) v = nud.Minimum;
            if (v > nud.Maximum) v = nud.Maximum;
            return v;
        }

        private void SetFovDegrees(double degrees)
        {
            // clamp to toolbar bounds just in case
            if (degrees < (double)_nudFov.Minimum) degrees = (double)_nudFov.Minimum;
            if (degrees > (double)_nudFov.Maximum) degrees = (double)_nudFov.Maximum;

            double r = DegToRad(degrees);

            // Update the raycaster's default-FOV memory
            _view.FovDefaultRadians = r;

            // If we're not auto-widened right now, apply it immediately
            if (!_view.AutoAdjustFovNearWall || !_view.IsFovWidened)
                _view.FOV = r;

            UpdateStatus();     // if you’re showing FOV in the status text
            _view.Invalidate();
        }



        private void HookInput()
        {
            this.KeyDown += (s, e) =>
            {
                // In HookInput KeyDown:
                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Q) { _view.BeginQuarterTurn(-1); e.Handled = true; }
                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.E) { _view.BeginQuarterTurn(+1); e.Handled = true; }

                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) { _view.TryStepForward(); e.Handled = true; }
                if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) { _view.TryStepBackward(); e.Handled = true; }
                if (e.KeyCode == Keys.A) { _view.TryStrafeLeft(); e.Handled = true; }
                if (e.KeyCode == Keys.D) { _view.TryStrafeRight(); e.Handled = true; }


                // Sprint no longer matters for grid steps; remove or repurpose if you like

                // Reset to spawn
                if (e.KeyCode == Keys.R)
                {
                    _view.SpawnFromMap(spawnTileValue: 2, angleDegrees: 0);
                    e.Handled = true;
                }

                // UI toggles
                if (e.KeyCode == Keys.F) { ToggleWireframe(); e.Handled = true; }
                if (e.KeyCode == Keys.T) { CycleTexture(); e.Handled = true; }
                if (e.KeyCode == Keys.B) { ApplyBrickPreset(); e.Handled = true; }
                if (e.KeyCode == Keys.L) { ToggleLighting(); e.Handled = true; }
                if (e.KeyCode == Keys.K) { ToggleShadows(); e.Handled = true; }
                if (e.KeyCode == Keys.G) { ToggleFloorTiles(); e.Handled = true; }
                if (e.KeyCode == Keys.I)
                {
                    _view.Lighting.InvertColorsInDarkness = !_view.Lighting.InvertColorsInDarkness;
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.D7)
                {
                    _view.Lighting.MaxViewDepth = (float)Math.Max(5.0, _view.Lighting.MaxViewDepth - 1.0);
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.D8)
                {
                    _view.Lighting.MaxViewDepth = (float)Math.Min(30.0, _view.Lighting.MaxViewDepth + 1.0);
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.D9)
                {
                    _view.Lighting.DarkMaxBrightness = (float)Math.Max(0.00, Math.Round(_view.Lighting.DarkMaxBrightness - 0.01, 2));
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.D0)
                {
                    _view.Lighting.DarkMaxBrightness = (float)Math.Min(0.30, Math.Round(_view.Lighting.DarkMaxBrightness + 0.01, 2));
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.OemOpenBrackets) // [
                {
                    _view.LightInnerRadius = Math.Max(0.5, _view.LightInnerRadius - 0.25);
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.OemCloseBrackets) // ]
                {
                    _view.LightInnerRadius = Math.Min(_view.LightOuterRadius - 0.5, _view.LightInnerRadius + 0.25);
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.OemMinus) // -
                {
                    _view.LightOuterRadius = Math.Max(_view.LightInnerRadius + 0.5, _view.LightOuterRadius - 0.5);
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.Oemplus) // =
                {
                    _view.LightOuterRadius = Math.Min(12.0, _view.LightOuterRadius + 0.5);
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.Space) // Interact / Pick up
                {
                    _view.TryInteractWithFacingItem();
                    UpdateStatus();
                    e.Handled = true;
                }
                if (e.KeyCode == Keys.P || e.KeyCode == Keys.U) // Drop item into tile slot
                {
                    _view.TryDropItemFromInventory();
                    UpdateStatus();
                    e.Handled = true;
                }
            };


            this.KeyUp += (s, e) =>
            {
                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) _up = false;
                if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) _down = false;
                if (e.KeyCode == Keys.A) _left = false;
                if (e.KeyCode == Keys.D) _right = false;

                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Q) _turnL = false;
                if (e.KeyCode == Keys.Right || e.KeyCode == Keys.E) _turnR = false;

                if (e.KeyCode == Keys.ShiftKey) _view.MoveSpeed = 3.0; // restore default
            };
        }

        private void HookTick()
        {
            _logicTimer = new Timer { Interval = 16 };
            _logicTimer.Tick += LogicTick;
            _logicTimer.Start();

            this.FormClosed += (s, e) =>
            {
                _logicTimer?.Stop();
                _logicTimer?.Dispose();
                _logicTimer = null;
            };
        }

        private void LogicTick(object sender, EventArgs e)
        {
            // We’re not integrating velocity anymore; just ensure redraws happen.
            _view.Invalidate();

            if (_compass != null)
            {
                _compass.DirectionRadians = _view.PlayerAngle;
                _compass.Invalidate();

            }

            // Sync HUD from the view
            if (_hud != null && _view != null)
            {
                _hud.DirectionRadians = _view.PlayerAngle;
                _hud.HP = _view.HP;
                _hud.HPMax = _view.HPMax;
                _hud.MP = _view.MP;
                _hud.MPMax = _view.MPMax;
                _hud.Invalidate();
            }

            if (_mapOverlay != null && _mapOverlay.Visible)
                _mapOverlay.Invalidate();


            //movement
            var now = DateTime.UtcNow;
            double dt = (now - _last).TotalSeconds;
            _last = now;

            // Always advance rotation tween if active
            _view.AdvanceTurnTween(dt);

            // While tweening a turn, skip movement to preserve grid feel
            if (_view.IsTurning) return;

            double fwd = 0.0, strafe = 0.0;

            // Your grid-step movement (TryStepForward/Backward/Strafe...) OR your analog MoveRelative
            // If you're using the discrete step API:
            if (_up) _view.TryStepForward();
            if (_down) _view.TryStepBackward();
            if (_left) _view.TryStrafeLeft();
            if (_right) _view.TryStrafeRight();
        }


        // ===== UI commands =====

        private void ToggleWireframe()
        {
            _view.WireframeBW = !_view.WireframeBW;
            UpdateStatus();
        }

        private void CycleTexture()
        {
            // Cycle: None -> Brick -> Grid -> Planks -> None ...
            switch (_view.CurrentTexture)
            {
                case WallTexture.None:
                    _view.CurrentTexture = WallTexture.BrickBW;
                    _btnTexture.Text = "Texture: Brick (T)";
                    break;
                case WallTexture.BrickBW:
                    _view.CurrentTexture = WallTexture.GridBW;
                    _btnTexture.Text = "Texture: Grid (T)";
                    break;
                case WallTexture.GridBW:
                    _view.CurrentTexture = WallTexture.PlanksBW;
                    _btnTexture.Text = "Texture: Planks (T)";
                    break;
                default:
                    _view.CurrentTexture = WallTexture.None;
                    _btnTexture.Text = "Texture: None (T)";
                    break;
            }
            UpdateStatus();
        }

        private void ApplyBrickPreset()
        {
            _view.CurrentTexture = WallTexture.BrickBW;
            _view.BricksPerX = 5;
            _view.BricksPerY = 14;
            _view.MortarFracX = 0.05;
            _view.MortarFracY = 0.05;
            _btnTexture.Text = "Texture: Brick (T)";
            UpdateStatus();
        }

        // Example layout that anchors camera to the top and leaves HUD space
        // Form1.cs
        // Form1.cs (inside class Form1)
        private void LayoutViewportWithAspect()
        {
            if (_view == null) return;

            // Fixed camera aspect you liked (~816x489)
            const float aspect = 816f / 489f;

            // Toolbar height if present
            int toolbarH = (_toolbar != null) ? _toolbar.Height : 0;

            // Padding between elements
            int pad = 10;

            // Desired HUD height (will shrink if it can’t fit)
            int hudHDesired = 120;
            int hudH = hudHDesired;

            // Available area for the CAMERA ONLY (we leave room for HUD below it)
            int availW = Math.Max(1, ClientSize.Width - pad * 2);
            int availH = Math.Max(1, ClientSize.Height - toolbarH - pad * 3 - hudH);

            // If the window is very small, temporarily borrow space from HUD to keep camera visible
            if (availH < 1)
            {
                // Try reducing HUD first
                int reclaim = 1 - availH;
                hudH = Math.Max(40, hudH - reclaim); // never smaller than 40px
                availH = Math.Max(1, ClientSize.Height - toolbarH - pad * 3 - hudH);

                // If still impossible, clamp to at least 1
                if (availH < 1) availH = 1;
            }

            // Fit camera to aspect inside availW x availH
            int fitW = availW;
            int fitH = (int)(fitW / aspect);
            if (fitH > availH)
            {
                fitH = availH;
                fitW = (int)(fitH * aspect);
            }

            // Camera position: centered horizontally, right below toolbar
            int viewX = (ClientSize.Width - fitW) / 2;
            int viewY = toolbarH + pad;

            // Place camera
            _view.SetBounds(viewX, viewY, fitW, fitH);

            // Map overlay tracks camera exactly
            if (_mapOverlay != null)
                _mapOverlay.SetBounds(viewX, viewY, fitW, fitH);

            // HUD: same width as camera, directly below it
            int hudX = viewX;
            int hudY = viewY + fitH + pad;

            // If HUD would fall below the window, shrink it to fit (down to 40px)
            int hudBottom = hudY + hudH;
            int maxBottom = ClientSize.Height - pad;
            if (hudBottom > maxBottom)
                hudH = Math.Max(40, maxBottom - hudY);

            if (_hud != null)
                _hud.SetBounds(hudX, hudY, fitW, Math.Max(0, hudH));
        }


        private void ToggleLighting()
        {
            _view.Lighting.EnableLighting = !_view.Lighting.EnableLighting;
            UpdateStatus();
            _view.Invalidate();
        }

        private void ToggleShadows()
        {
            _view.Lighting.EnableShadows = !_view.Lighting.EnableShadows;
            UpdateStatus();
            _view.Invalidate();
        }

        private void ToggleFloorTiles()
        {
            _view.Lighting.ShowFloorTiles = !_view.Lighting.ShowFloorTiles;
            UpdateStatus();
            _view.Invalidate();
        }

        private void UpdateStatus()
        {
            string lvlName = _view.CurrentLevel != null ? _view.CurrentLevel.LevelName : "Default";
            int invCount = _view.PlayerInventory.Count;

            _lblStatus.Text =
                $"[{lvlName}] Inv:{invCount} | " +
                $"Light: {(_view.Lighting.EnableLighting ? "ON" : "OFF")} | " +
                $"InvertDark: {(_view.Lighting.InvertColorsInDarkness ? "ON" : "OFF")} | " +
                $"DarkMax: {(_view.Lighting.DarkMaxBrightness * 100):0}% | " +
                $"ViewDepth: {_view.Lighting.MaxViewDepth:0.0} | " +
                $"InnerR: {_view.LightInnerRadius:0.0} | " +
                $"OuterR: {_view.LightOuterRadius:0.0} | " +
                $"FOV={RadToDeg(_view.FOV):0}°";
        }

        private void HandleTransition(TransitionObject trans)
        {
            if (trans == null) return;

            string targetDungeonId = string.IsNullOrEmpty(trans.TargetDungeon)
                ? (_project != null ? _project.StartDungeonId : "dungeon_01")
                : trans.TargetDungeon;

            int targetLvl = trans.TargetLevel;

            // 1. Level or Dungeon Transition
            if (targetLvl != -1 && (_view.CurrentLevel == null || targetLvl != _view.CurrentLevel.LevelNumber || (!string.IsNullOrEmpty(trans.TargetDungeon) && !string.Equals(trans.TargetDungeon, _project?.StartDungeonId, StringComparison.OrdinalIgnoreCase))))
            {
                if (_project != null && _project.Dungeons.TryGetValue(targetDungeonId, out var pkg))
                {
                    var nextLvl = pkg.Levels.Find(l => l.LevelNumber == targetLvl);
                    if (nextLvl != null)
                    {
                        _view.LoadLevel(nextLvl);
                        _project.CurrentLevel = nextLvl;

                        if (trans.TargetX >= 0 && trans.TargetY >= 0)
                        {
                            double angle = FacingToDegrees(trans.TargetFacing);
                            _view.SetPlayerStart(trans.TargetX, trans.TargetY, angle);
                        }

                        string msg = !string.IsNullOrEmpty(trans.TransitionMessage)
                            ? trans.TransitionMessage
                            : $"Traveled to {nextLvl.LevelName} ({pkg.Name})";
                        _lblStatus.Text = msg;
                        UpdateStatus();
                        return;
                    }
                }
            }

            // 2. Intra-level Room-to-Room Teleport / Passage (same level)
            if (trans.TargetX >= 0 && trans.TargetY >= 0)
            {
                _view.PlayerX = trans.TargetX + 0.5;
                _view.PlayerY = trans.TargetY + 0.5;
                if (!string.Equals(trans.TargetFacing, "Keep", StringComparison.OrdinalIgnoreCase))
                {
                    _view.SetPlayerFacing(FacingToCardinal(trans.TargetFacing));
                }

                string msg = !string.IsNullOrEmpty(trans.TransitionMessage)
                    ? trans.TransitionMessage
                    : $"Moved to ({trans.TargetX}, {trans.TargetY})";
                _lblStatus.Text = msg;
                UpdateStatus();
            }
        }

        private static double FacingToDegrees(string facing)
        {
            if (string.Equals(facing, "North", StringComparison.OrdinalIgnoreCase)) return 270.0;
            if (string.Equals(facing, "East", StringComparison.OrdinalIgnoreCase)) return 0.0;
            if (string.Equals(facing, "South", StringComparison.OrdinalIgnoreCase)) return 90.0;
            if (string.Equals(facing, "West", StringComparison.OrdinalIgnoreCase)) return 180.0;
            return 0.0;
        }

        private static Cardinal FacingToCardinal(string facing)
        {
            if (string.Equals(facing, "North", StringComparison.OrdinalIgnoreCase)) return Cardinal.North;
            if (string.Equals(facing, "East", StringComparison.OrdinalIgnoreCase)) return Cardinal.East;
            if (string.Equals(facing, "South", StringComparison.OrdinalIgnoreCase)) return Cardinal.South;
            if (string.Equals(facing, "West", StringComparison.OrdinalIgnoreCase)) return Cardinal.West;
            return Cardinal.East;
        }

    }
}
