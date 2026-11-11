using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GridLocked;

namespace GridLockedEditor
{
    public sealed class EditorForm : Form
    {
        private GameProject _project;
        private DungeonLevel _currentLevel;
        private string _projectDir;

        // Visual Studio-Style Solution / Project Explorer
        private SplitContainer _mainSplit;
        private ProjectExplorerControl _projectExplorer;
        private ToolStripMenuItem _menuItemToggleExplorer;

        // Level & Dungeon Selection (in Map Painter tab)
        private ComboBox _cbDungeons;
        private ComboBox _cbLevels;

        // Tab Control
        private TabControl _tabs;

        // Tab 1: Map Painter
        private MapPainterControl _painter;
        private Panel _mapScrollPanel;
        private RadioButton _rbToolSelect;
        private RadioButton _rbToolWall;
        private RadioButton _rbToolFloor;
        private RadioButton _rbToolSpawn;
        private RadioButton _rbToolDoor;
        private RadioButton _rbToolStairsDown;
        private RadioButton _rbToolStairsUp;
        private RadioButton _rbToolVoid;
        private RadioButton _rbToolProp;
        private ComboBox _cbToolProp;
        private Button _btnToolUndo;
        private Button _btnToolRedo;
        private RadioButton _rbToolArchetype;
        private ComboBox _cbToolArchetype;

        // Tile Inspector Cards & Panels
        private Panel _inspectorPanel;
        private Label _lblSelectedCoords;
        private Label _lblTileArchetype;
        private ComboBox _cbTileType;
        private CheckBox _chkTileCanWalkThru;
        private CheckBox _chkTileBlocksVision;
        private CheckBox _chkTileBreakable;
        private NumericUpDown _nudTileBreakHP;
        private CheckBox _chkTileMoveable;
        private CheckBox _chkTileCanBeOpened;
        private CheckBox _chkTileIsOpen;
        private CheckBox _chkTileHazardous;
        private NumericUpDown _nudTileHazardDamage;
        private Button _btnSpawnEntityHere;
        private GroupBox _grpTileBase;

        // Spawned Entity Card
        private GroupBox _grpEntityCard;
        private Button _btnRemoveEntity;
        private ComboBox _cbEntityDef;
        private ComboBox _cbEntityFacing;
        private NumericUpDown _nudEntityLevel;
        private Label _lblEntityStatsPreview;
        private CheckBox _chkEntityUnique;
        private TextBox _txtEntityTitle;
        private CheckBox _chkEntityArchetypeLoot;
        private CheckBox _chkEntityZoneLoot;
        private ComboBox _cbEntityCustomLoot;
        private ComboBox _cbGuaranteedItem;
        private Button _btnAddGuaranteedDrop;
        private ListBox _lbGuaranteedDrops;
        private Button _btnRemoveGuaranteedDrop;

        // Movement & Patrol Route Card
        private GroupBox _grpMovementCard;
        private ComboBox _cbMovementMode;
        private Label _lblCircuit;
        private ComboBox _cbCircuitStyle;
        private Label _lblPause;
        private NumericUpDown _nudWaypointPause;
        private Label _lblWanderInt;
        private NumericUpDown _nudWanderInterval;
        private ListBox _lbWaypoints;
        private Button _btnRecordRoute;
        private Button _btnClearRoute;
        private bool _isRecordingRoute;

        // Painted Wander Territory & Combat Disengage Leash
        private Button _btnPaintWanderArea;
        private Button _btnClearWanderArea;
        private Label _lblWanderTilesCount;
        private Label _lblMaxChase;
        private NumericUpDown _nudMaxChase;
        private CheckBox _chkReturnPreCombat;
        private Label _lblNavStatus;
        private Button _btnSimulateWanderStep;
        private bool _isPaintingWanderArea;

        // Dungeon & Level Specifics & Loot Table Inspector
        private GroupBox _grpDungeonInspector;
        private Label _lblDungeonHeader;
        private Label _lblDungeonDetails;
        private Button _btnBackToTile;
        private ComboBox _cbDungeonLootTables;
        private Button _btnNewLootTable;
        private Button _btnDeleteLootTable;
        private TextBox _txtLootTableName;
        private NumericUpDown _nudLootMinGold;
        private NumericUpDown _nudLootMaxGold;
        private NumericUpDown _nudLootRolls;
        private DataGridView _dgvLootEntries;
        private ComboBox _cbLootAddItem;
        private Button _btnLootAddItem;
        private Button _btnLootRemoveItem;
        private Button _btnTestRollTable;
        private LootTable _selectedLootTable;
        private string _activeDungeonInspectorId;
        private int _activeDungeonInspectorLevel = -1;
        private bool _isPopulatingLootInspector;

        // Ground Item Slots Card
        private GroupBox _grpSubSquareItems;
        private ComboBox[] _cbSlotItems = new ComboBox[4];
        private Button[] _btnClearSlots = new Button[4];
        private bool _isPopulatingInspector;

        // Transition & Security Inspector
        private GroupBox _grpTransition;
        private ComboBox _cbTransTargetDungeon;
        private NumericUpDown _nudTransTargetLevel;
        private NumericUpDown _nudTransTargetX;
        private NumericUpDown _nudTransTargetY;
        private ComboBox _cbTransTargetFacing;
        private CheckBox _chkTransLocked;
        private ComboBox _cbTransKey;
        private CheckBox _chkTransTrapped;
        private ComboBox _cbTransTrapType;
        private NumericUpDown _nudTransTrapDamage;
        private CheckBox _chkTransRecurring;
        private NumericUpDown _nudTransJiggleChance;
        private NumericUpDown _nudTransOpenChance;
        private CheckBox _chkTransOpen;
        private CheckBox _chkTransAuto;

        // Navigation Menu
        private MenuStrip _mainMenu;
        private ToolStripMenuItem _menuItemSave;
        private ToolStripMenuItem _menuItemClose;
        private ToolStripMenuItem _menuItemPlaytest;
        private ToolStripMenuItem _menuItemReload;
        private ToolStripMenuItem _menuItemUndo;
        private ToolStripMenuItem _menuItemRedo;

        // Tab 4: Project Settings & Key Legend
        private TextBox _txtSettingsTitle;
        private Label _lblSettingsFolder;
        private ComboBox _cbSettingsStartDungeon;
        private NumericUpDown _nudSettingsStartLevel;
        private DataGridView _dgvLegend;

        // Tab 2: Item & Object Creator
        private ListBox _lbItems;
        private ComboBox _cbCategoryFilter;
        private TextBox _txtItemSearch;
        private Label _lblItemCount;
        private TextBox _txtItemId;
        private TextBox _txtItemName;
        private ComboBox _cbCategory;
        private ComboBox _cbSubCategory;
        private ComboBox _cbItemInherit;
        private TextBox _txtDescription;
        private CheckBox _chkIsTemplate;

        // Physical & Economy
        private NumericUpDown _nudWeight;
        private NumericUpDown _nudValue;
        private CheckBox _chkCollectable;
        private CheckBox _chkPassThru;
        private ComboBox _cbSpriteKind;
        private Panel _pnlItemColorSwatch;
        private Color _currentItemColor = Color.Gray;

        // Weapons & Combat
        private GroupBox _grpWeapon;
        private RadioButton _rbAttackMelee;
        private RadioButton _rbAttackRanged;
        private RadioButton _rbAttackCast;
        private ComboBox _cbHands;
        private NumericUpDown _nudMinDamage;
        private NumericUpDown _nudMaxDamage;
        private NumericUpDown _nudRange;
        private TextBox _txtAmmoType;

        // Affects & Modifiers (Genre-Agnostic: spells, plasma coils, tech mods)
        private GroupBox _grpEffects;
        private CheckBox _chkHasEffect;
        private ComboBox _cbEffectType;
        private TextBox _txtProcEffect;
        private NumericUpDown _nudResourceCost;
        private NumericUpDown _nudEffectPower;

        // Armor & Defense
        private GroupBox _grpArmor;
        private ComboBox _cbEquipSlot;
        private NumericUpDown _nudArmorRating;
        private NumericUpDown _nudEnergyResistance;
        private ComboBox _cbResistType;

        // Consumables
        private GroupBox _grpConsumable;
        private ComboBox _cbConsumableType;
        private NumericUpDown _nudEffectAmount;
        private NumericUpDown _nudDuration;

        // Containers & Security
        private GroupBox _grpContainer;
        private CheckBox _chkContainer;
        private NumericUpDown _nudCapacity;
        private TextBox _txtTrapped;
        private TextBox _txtLocked;
        private CheckBox _chkCursed;
        private TextBox _txtCurseType;

        // Hazards & Terrain
        private GroupBox _grpTerrain;
        private CheckBox _chkDifficult;
        private NumericUpDown _nudSpeedPenalty;
        private NumericUpDown _nudHazardDamage;

        // Custom properties table
        private GroupBox _grpCustomProps;
        private Panel _pnlCustomPropsList;

        private ItemDefinition _selectedItemDef;
        private readonly List<ItemDefinition> _filteredDefsList = new List<ItemDefinition>();

        // Tab 3: Zone Lighting
        private TrackBar _tbDarkMax;
        private Label _lblDarkMaxVal;
        private NumericUpDown _nudMaxViewDepth;
        private NumericUpDown _nudMinViewDepth;
        private NumericUpDown _nudInnerRadius;
        private NumericUpDown _nudOuterRadius;
        private CheckBox _chkInvertDark;
        private Panel _pnlLanternColor;
        private Color _lanternColor = Color.FromArgb(255, 235, 195);
        private Panel _pnlFloorColor;
        private Color _floorColor = Color.FromArgb(220, 210, 195);
        private Panel _pnlCeilingColor;
        private Color _ceilingColor = Color.FromArgb(90, 90, 90);
        private CheckBox _chkShowFloorGrid;

        public EditorForm()
        {
            Text = "GridLocked Editor - Dungeon Crawler Studio";
            Width = 1260;
            Height = 820;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Z)
                {
                    _painter.Undo();
                    e.Handled = true;
                }
                else if ((e.Control && e.KeyCode == Keys.Y) || (e.Control && e.Shift && e.KeyCode == Keys.Z))
                {
                    _painter.Redo();
                    e.Handled = true;
                }
            };

            Shown += (s, e) =>
            {
                if (_mainSplit != null && !_mainSplit.IsDisposed)
                {
                    try { _mainSplit.SplitterDistance = 260; } catch { }
                }
            };

            BuildUi();
            LoadDefaultProject();
        }

        private void BuildUi()
        {
            BuildNavigationMenu();

            // Main Split: Left = Project Explorer (VS Solution Explorer), Right = Workspaces
            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 180,
                SplitterDistance = 260,
                SplitterWidth = 5,
                FixedPanel = FixedPanel.Panel1
            };
            Controls.Add(_mainSplit);
            _mainSplit.BringToFront();

            // Left: Project Hierarchy Viewer
            _projectExplorer = new ProjectExplorerControl { Dock = DockStyle.Fill };
            _projectExplorer.NodeActivated += HandleProjectNodeActivated;
            _projectExplorer.RequestRefresh += ReloadProject;
            _projectExplorer.RequestAddLevel += PromptAddNewLevel;
            _projectExplorer.RequestAddDungeon += PromptAddNewDungeon;
            _mainSplit.Panel1.Controls.Add(_projectExplorer);

            // Right: Workspace Tabs
            _tabs = new TabControl { Dock = DockStyle.Fill };
            _mainSplit.Panel2.Controls.Add(_tabs);

            BuildTabMapPainter();
            BuildTabItemCreator();
            BuildTabZoneLighting();
            BuildTabProjectSettings();
        }

        private void BuildNavigationMenu()
        {
            _mainMenu = new MenuStrip
            {
                RenderMode = ToolStripRenderMode.System
            };
            MainMenuStrip = _mainMenu;

            // 1. File Menu
            var menuFile = new ToolStripMenuItem("&File");
            var itemNew = new ToolStripMenuItem("&New Project...", null, (s, e) => PromptNewProject(), Keys.Control | Keys.N);
            var itemOpen = new ToolStripMenuItem("&Open Project...", null, (s, e) => BrowseOpenProject(), Keys.Control | Keys.O);
            _menuItemSave = new ToolStripMenuItem("&Save All / Project", null, (s, e) => SaveCurrentLevelAndProject(), Keys.Control | Keys.S);
            var itemExportGlpFile = new ToolStripMenuItem("Export to &Module (.glp)...", null, (s, e) => ExportProjectToGlp(), Keys.Control | Keys.E);
            _menuItemClose = new ToolStripMenuItem("&Close Project", null, (s, e) => CloseProject());
            var itemExit = new ToolStripMenuItem("E&xit", null, (s, e) => Close(), Keys.Alt | Keys.F4);

            menuFile.DropDownItems.Add(itemNew);
            menuFile.DropDownItems.Add(itemOpen);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(_menuItemSave);
            menuFile.DropDownItems.Add(itemExportGlpFile);
            menuFile.DropDownItems.Add(_menuItemClose);
            menuFile.DropDownItems.Add(new ToolStripSeparator());
            menuFile.DropDownItems.Add(itemExit);

            // 2. Edit Menu
            var menuEdit = new ToolStripMenuItem("&Edit");
            _menuItemUndo = new ToolStripMenuItem("&Undo", null, (s, e) => _painter.Undo(), Keys.Control | Keys.Z);
            _menuItemRedo = new ToolStripMenuItem("&Redo", null, (s, e) => _painter.Redo(), Keys.Control | Keys.Y);
            menuEdit.DropDownItems.Add(_menuItemUndo);
            menuEdit.DropDownItems.Add(_menuItemRedo);

            // 3. View Menu
            var menuView = new ToolStripMenuItem("&View");
            _menuItemToggleExplorer = new ToolStripMenuItem("📁 &Project Explorer", null, (s, e) => ToggleProjectExplorer(), Keys.Control | Keys.Alt | Keys.L) { Checked = true };
            var itemTab0 = new ToolStripMenuItem("1. &Map Painter & Placer", null, (s, e) => SelectTab(0), Keys.Control | Keys.D1);
            var itemTab1 = new ToolStripMenuItem("2. &Items && Objects", null, (s, e) => SelectTab(1), Keys.Control | Keys.D2);
            var itemTab2 = new ToolStripMenuItem("3. &Zone Lighting", null, (s, e) => SelectTab(2), Keys.Control | Keys.D3);
            var itemTab3 = new ToolStripMenuItem("4. &Project Settings && Legend", null, (s, e) => SelectTab(3), Keys.Control | Keys.D4);
            menuView.DropDownItems.Add(_menuItemToggleExplorer);
            menuView.DropDownItems.Add(new ToolStripSeparator());
            menuView.DropDownItems.Add(itemTab0);
            menuView.DropDownItems.Add(itemTab1);
            menuView.DropDownItems.Add(itemTab2);
            menuView.DropDownItems.Add(itemTab3);

            // 4. Project Menu
            var menuProj = new ToolStripMenuItem("&Project");
            _menuItemPlaytest = new ToolStripMenuItem("▶ &Playtest Game", null, (s, e) => LaunchPlaytest(), Keys.F5);
            _menuItemReload = new ToolStripMenuItem("🔄 &Reload Project", null, (s, e) => ReloadProject(), Keys.F6);
            var itemAddLvl = new ToolStripMenuItem("➕ Add &New Level...", null, (s, e) => PromptAddNewLevel());
            var itemAddDung = new ToolStripMenuItem("🏰 Add &New Dungeon...", null, (s, e) => PromptAddNewDungeon());
            var itemExportGlp = new ToolStripMenuItem("📦 &Export to Module (.glp)...", null, (s, e) => ExportProjectToGlp());
            var itemProjSettings = new ToolStripMenuItem("⚙ Project &Settings...", null, (s, e) => SelectTab(3));
            var itemExplore = new ToolStripMenuItem("📁 Open in &Windows Explorer", null, (s, e) => OpenProjectInExplorer());

            menuProj.DropDownItems.Add(_menuItemPlaytest);
            menuProj.DropDownItems.Add(_menuItemReload);
            menuProj.DropDownItems.Add(new ToolStripSeparator());
            menuProj.DropDownItems.Add(itemAddLvl);
            menuProj.DropDownItems.Add(itemAddDung);
            menuProj.DropDownItems.Add(new ToolStripSeparator());
            menuProj.DropDownItems.Add(itemExportGlp);
            menuProj.DropDownItems.Add(itemProjSettings);
            menuProj.DropDownItems.Add(itemExplore);

            // 5. Help Menu
            var menuHelp = new ToolStripMenuItem("&Help");
            var itemHelpLegend = new ToolStripMenuItem("ASCII Map Legend Help", null, (s, e) => SelectTab(3));
            var itemAbout = new ToolStripMenuItem("&About GridLocked Studio", null, (s, e) => ShowAboutDialog());
            menuHelp.DropDownItems.Add(itemHelpLegend);
            menuHelp.DropDownItems.Add(itemAbout);

            _mainMenu.Items.AddRange(new ToolStripItem[] { menuFile, menuEdit, menuView, menuProj, menuHelp });
            Controls.Add(_mainMenu);
        }

        private void BuildTabMapPainter()
        {
            var tab = new TabPage("ASCII Map Painter & Placer");
            _tabs.TabPages.Add(tab);

            // Top Map Header Bar (Dungeon & Level navigation)
            var mapHeader = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.FromArgb(235, 235, 238), Padding = new Padding(8, 4, 8, 4) };
            tab.Controls.Add(mapHeader);

            var lblD = new Label { Text = "Dungeon:", Left = 12, Top = 9, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50) };
            mapHeader.Controls.Add(lblD);

            _cbDungeons = new ComboBox { Left = lblD.Right + 6, Top = 6, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbDungeons.SelectedIndexChanged += (s, e) => OnDungeonSelected();
            mapHeader.Controls.Add(_cbDungeons);

            var lblL = new Label { Text = "Level:", Left = _cbDungeons.Right + 15, Top = 9, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50) };
            mapHeader.Controls.Add(lblL);

            _cbLevels = new ComboBox { Left = lblL.Right + 6, Top = 6, Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbLevels.SelectedIndexChanged += (s, e) => OnLevelSelected();
            mapHeader.Controls.Add(_cbLevels);

            // Left Toolbar (Tools)
            var toolPanel = new Panel { Dock = DockStyle.Left, Width = 160, BackColor = Color.FromArgb(240, 240, 240) };
            tab.Controls.Add(toolPanel);

            var lblTools = new Label { Text = "Drawing Tools", Left = 10, Top = 10, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold) };
            toolPanel.Controls.Add(lblTools);

            _rbToolSelect = new RadioButton { Text = "Inspect Tile", Left = 12, Top = 35, AutoSize = true, Checked = true };
            _rbToolSelect.CheckedChanged += (s, e) => { if (_rbToolSelect.Checked) _painter.CurrentTool = PainterTool.Select; };
            toolPanel.Controls.Add(_rbToolSelect);

            _rbToolWall = new RadioButton { Text = "Wall (#)", Left = 12, Top = 62, AutoSize = true };
            _rbToolWall.CheckedChanged += (s, e) => { if (_rbToolWall.Checked) _painter.CurrentTool = PainterTool.Wall; };
            toolPanel.Controls.Add(_rbToolWall);

            _rbToolFloor = new RadioButton { Text = "Floor (.)", Left = 12, Top = 89, AutoSize = true };
            _rbToolFloor.CheckedChanged += (s, e) => { if (_rbToolFloor.Checked) _painter.CurrentTool = PainterTool.Floor; };
            toolPanel.Controls.Add(_rbToolFloor);

            _rbToolSpawn = new RadioButton { Text = "Spawn (@)", Left = 12, Top = 116, AutoSize = true };
            _rbToolSpawn.CheckedChanged += (s, e) => { if (_rbToolSpawn.Checked) _painter.CurrentTool = PainterTool.Spawn; };
            toolPanel.Controls.Add(_rbToolSpawn);

            _rbToolDoor = new RadioButton { Text = "Door (D)", Left = 12, Top = 143, AutoSize = true };
            _rbToolDoor.CheckedChanged += (s, e) => { if (_rbToolDoor.Checked) _painter.CurrentTool = PainterTool.Door; };
            toolPanel.Controls.Add(_rbToolDoor);

            _rbToolStairsUp = new RadioButton { Text = "Stairs Up (S)", Left = 12, Top = 170, AutoSize = true };
            _rbToolStairsUp.CheckedChanged += (s, e) => { if (_rbToolStairsUp.Checked) _painter.CurrentTool = PainterTool.StairsUp; };
            toolPanel.Controls.Add(_rbToolStairsUp);

            _rbToolStairsDown = new RadioButton { Text = "Stairs Down (s)", Left = 12, Top = 197, AutoSize = true };
            _rbToolStairsDown.CheckedChanged += (s, e) => { if (_rbToolStairsDown.Checked) _painter.CurrentTool = PainterTool.StairsDown; };
            toolPanel.Controls.Add(_rbToolStairsDown);

            _rbToolVoid = new RadioButton { Text = "Void/Pit (~)", Left = 12, Top = 224, AutoSize = true };
            _rbToolVoid.CheckedChanged += (s, e) => { if (_rbToolVoid.Checked) _painter.CurrentTool = PainterTool.Void; };
            toolPanel.Controls.Add(_rbToolVoid);

            _rbToolProp = new RadioButton { Text = "Object / Prop:", Left = 12, Top = 251, AutoSize = true };
            _rbToolProp.CheckedChanged += (s, e) =>
            {
                if (_rbToolProp.Checked)
                {
                    _painter.CurrentTool = PainterTool.CustomObject;
                    _painter.ActiveObjectId = _cbToolProp.SelectedItem?.ToString() ?? "tree_oak";
                }
            };
            toolPanel.Controls.Add(_rbToolProp);

            _cbToolProp = new ComboBox { Left = 12, Top = 275, Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbToolProp.SelectedIndexChanged += (s, e) =>
            {
                if (_rbToolProp.Checked)
                    _painter.ActiveObjectId = _cbToolProp.SelectedItem?.ToString() ?? "tree_oak";
            };
            toolPanel.Controls.Add(_cbToolProp);

            _rbToolArchetype = new RadioButton { Text = "Legend Archetype:", Left = 12, Top = 304, AutoSize = true };
            _rbToolArchetype.CheckedChanged += (s, e) =>
            {
                if (_rbToolArchetype.Checked)
                {
                    _painter.CurrentTool = PainterTool.ArchetypeBrush;
                    UpdateActiveArchetypeBrush();
                }
            };
            toolPanel.Controls.Add(_rbToolArchetype);

            _cbToolArchetype = new ComboBox { Left = 12, Top = 328, Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbToolArchetype.SelectedIndexChanged += (s, e) =>
            {
                if (_rbToolArchetype.Checked)
                    UpdateActiveArchetypeBrush();
            };
            toolPanel.Controls.Add(_cbToolArchetype);

            var lblHist = new Label { Text = "History / Undo", Left = 12, Top = 360, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold) };
            toolPanel.Controls.Add(lblHist);

            _btnToolUndo = new Button { Text = "Undo (Ctrl+Z)", Left = 12, Top = 382, Width = 135, Height = 26, Enabled = false };
            _btnToolUndo.Click += (s, e) => _painter.Undo();
            toolPanel.Controls.Add(_btnToolUndo);

            _btnToolRedo = new Button { Text = "Redo (Ctrl+Y)", Left = 12, Top = 412, Width = 135, Height = 26, Enabled = false };
            _btnToolRedo.Click += (s, e) => _painter.Redo();
            toolPanel.Controls.Add(_btnToolRedo);

            // Right Panel (Tile & Card-Based Inspector)
            _inspectorPanel = new Panel { Dock = DockStyle.Right, Width = 315, BackColor = Color.FromArgb(245, 245, 245), AutoScroll = true };
            tab.Controls.Add(_inspectorPanel);

            // --- Card 1: Tile Base Properties & Flags ---
            _grpTileBase = new GroupBox { Text = "📍 Tile Properties & Flags", Left = 10, Top = 10, Width = 295, Height = 215, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold) };
            _inspectorPanel.Controls.Add(_grpTileBase);

            _lblSelectedCoords = new Label { Text = "Select a tile to inspect", Left = 10, Top = 20, AutoSize = true, ForeColor = Color.DarkSlateGray, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _grpTileBase.Controls.Add(_lblSelectedCoords);

            _lblTileArchetype = new Label { Text = "Archetype: Floor (.)", Left = 130, Top = 20, AutoSize = true, ForeColor = Color.DarkBlue, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _grpTileBase.Controls.Add(_lblTileArchetype);

            var lblTType = new Label { Text = "Base Type:", Left = 10, Top = 42, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _grpTileBase.Controls.Add(lblTType);

            _cbTileType = new ComboBox { Left = 75, Top = 39, Width = 210, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _cbTileType.Items.AddRange(Enum.GetNames(typeof(TileType)));
            _cbTileType.SelectedIndexChanged += (s, e) => OnInspectorTileTypeChanged();
            _grpTileBase.Controls.Add(_cbTileType);

            // Interaction Flags (CanWalkThru, BlocksVision, Breakable, Moveable, CanBeOpened, Hazardous)
            _chkTileCanWalkThru = new CheckBox { Text = "Can Walk Thru / Over", Left = 10, Top = 68, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkTileCanWalkThru.CheckedChanged += (s, e) => OnTileFlagChanged();
            _grpTileBase.Controls.Add(_chkTileCanWalkThru);

            _chkTileBlocksVision = new CheckBox { Text = "Blocks Vision (Opaque)", Left = 150, Top = 68, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkTileBlocksVision.CheckedChanged += (s, e) => OnTileFlagChanged();
            _grpTileBase.Controls.Add(_chkTileBlocksVision);

            _chkTileBreakable = new CheckBox { Text = "Breakable", Left = 10, Top = 92, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkTileBreakable.CheckedChanged += (s, e) => { _nudTileBreakHP.Enabled = _chkTileBreakable.Checked; OnTileFlagChanged(); };
            _grpTileBase.Controls.Add(_chkTileBreakable);

            var lblBHP = new Label { Text = "HP:", Left = 86, Top = 93, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _grpTileBase.Controls.Add(lblBHP);

            _nudTileBreakHP = new NumericUpDown { Left = 108, Top = 90, Width = 45, Minimum = 1, Maximum = 1000, Value = 50, Enabled = false, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _nudTileBreakHP.ValueChanged += (s, e) => OnTileFlagChanged();
            _grpTileBase.Controls.Add(_nudTileBreakHP);

            _chkTileMoveable = new CheckBox { Text = "Moveable (Push)", Left = 160, Top = 92, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkTileMoveable.CheckedChanged += (s, e) => OnTileFlagChanged();
            _grpTileBase.Controls.Add(_chkTileMoveable);

            _chkTileCanBeOpened = new CheckBox { Text = "Can Be Opened", Left = 10, Top = 116, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkTileCanBeOpened.CheckedChanged += (s, e) => { _chkTileIsOpen.Enabled = _chkTileCanBeOpened.Checked; OnTileFlagChanged(); };
            _grpTileBase.Controls.Add(_chkTileCanBeOpened);

            _chkTileIsOpen = new CheckBox { Text = "Is Open", Left = 115, Top = 116, AutoSize = true, Enabled = false, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkTileIsOpen.CheckedChanged += (s, e) => OnTileFlagChanged();
            _grpTileBase.Controls.Add(_chkTileIsOpen);

            _chkTileHazardous = new CheckBox { Text = "Hazardous", Left = 10, Top = 140, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkTileHazardous.CheckedChanged += (s, e) => { _nudTileHazardDamage.Enabled = _chkTileHazardous.Checked; OnTileFlagChanged(); };
            _grpTileBase.Controls.Add(_chkTileHazardous);

            var lblHaz = new Label { Text = "Dmg:", Left = 86, Top = 141, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _grpTileBase.Controls.Add(lblHaz);

            _nudTileHazardDamage = new NumericUpDown { Left = 118, Top = 138, Width = 45, Minimum = 0, Maximum = 999, Value = 10, Enabled = false, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _nudTileHazardDamage.ValueChanged += (s, e) => OnTileFlagChanged();
            _grpTileBase.Controls.Add(_nudTileHazardDamage);

            _btnSpawnEntityHere = new Button { Text = "+ Spawn Entity / Creature Here", Left = 10, Top = 172, Width = 275, Height = 28, BackColor = Color.FromArgb(46, 139, 87), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold), Visible = false };
            _btnSpawnEntityHere.Click += (s, e) => OnSpawnEntityClicked();
            _grpTileBase.Controls.Add(_btnSpawnEntityHere);

            // --- Card 2: Spawned Entity / Creature Card ---
            _grpEntityCard = new GroupBox { Text = "👾 Spawned Entity Details", Left = 10, Top = 130, Width = 295, Height = 350, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold), Visible = false };
            _inspectorPanel.Controls.Add(_grpEntityCard);

            _btnRemoveEntity = new Button { Text = "✕ Despawn", Left = 185, Top = 14, Width = 100, Height = 22, BackColor = Color.FromArgb(190, 50, 50), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _btnRemoveEntity.Click += (s, e) => OnRemoveEntityClicked();
            _grpEntityCard.Controls.Add(_btnRemoveEntity);

            var lblEType = new Label { Text = "Archetype & Facing:", Left = 10, Top = 25, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _grpEntityCard.Controls.Add(lblEType);

            _cbEntityDef = new ComboBox { Left = 10, Top = 43, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular) };
            _cbEntityDef.SelectedIndexChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_cbEntityDef);

            _cbEntityFacing = new ComboBox { Left = 195, Top = 43, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular) };
            _cbEntityFacing.Items.AddRange(new object[] { "North", "East", "South", "West" });
            _cbEntityFacing.SelectedIndex = 2; // South
            _cbEntityFacing.SelectedIndexChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_cbEntityFacing);

            var lblELvl = new Label { Text = "Level (1-20):", Left = 10, Top = 72, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _grpEntityCard.Controls.Add(lblELvl);

            _nudEntityLevel = new NumericUpDown { Left = 85, Top = 70, Width = 55, Minimum = 1, Maximum = 20, Value = 1, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular) };
            _nudEntityLevel.ValueChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_nudEntityLevel);

            _lblEntityStatsPreview = new Label { Text = "HP:30 MP:0 Dmg:3-6 Arm:0 Spd:10", Left = 10, Top = 96, Width = 275, Height = 18, ForeColor = Color.DarkSlateBlue, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _grpEntityCard.Controls.Add(_lblEntityStatsPreview);

            _chkEntityUnique = new CheckBox { Text = "★ Unique / Boss Creature", Left = 10, Top = 118, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold) };
            _chkEntityUnique.CheckedChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_chkEntityUnique);

            var lblETitle = new Label { Text = "Boss Title:", Left = 10, Top = 142, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _grpEntityCard.Controls.Add(lblETitle);

            _txtEntityTitle = new TextBox { Left = 80, Top = 139, Width = 205, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular), Enabled = false };
            _txtEntityTitle.TextChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_txtEntityTitle);

            _chkEntityArchetypeLoot = new CheckBox { Text = "Drop Archetype Loot (e.g. bones, scrap)", Left = 10, Top = 168, AutoSize = true, Checked = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkEntityArchetypeLoot.CheckedChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_chkEntityArchetypeLoot);

            _chkEntityZoneLoot = new CheckBox { Text = "Drop Zone / Floor Master Loot Table", Left = 10, Top = 188, AutoSize = true, Checked = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _chkEntityZoneLoot.CheckedChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_chkEntityZoneLoot);

            var lblCLoot = new Label { Text = "Custom Loot:", Left = 10, Top = 211, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _grpEntityCard.Controls.Add(lblCLoot);

            _cbEntityCustomLoot = new ComboBox { Left = 85, Top = 208, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _cbEntityCustomLoot.SelectedIndexChanged += (s, e) => OnEntityPropertyChanged();
            _grpEntityCard.Controls.Add(_cbEntityCustomLoot);

            var lblGDrop = new Label { Text = "Guaranteed Drops (100% on defeat):", Left = 10, Top = 236, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 100, 40) };
            _grpEntityCard.Controls.Add(lblGDrop);

            _cbGuaranteedItem = new ComboBox { Left = 10, Top = 254, Width = 185, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _grpEntityCard.Controls.Add(_cbGuaranteedItem);

            _btnAddGuaranteedDrop = new Button { Text = "+ Add Drop", Left = 200, Top = 253, Width = 85, Height = 24, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _btnAddGuaranteedDrop.Click += (s, e) => OnAddGuaranteedDrop();
            _grpEntityCard.Controls.Add(_btnAddGuaranteedDrop);

            _lbGuaranteedDrops = new ListBox { Left = 10, Top = 282, Width = 195, Height = 56, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _grpEntityCard.Controls.Add(_lbGuaranteedDrops);

            _btnRemoveGuaranteedDrop = new Button { Text = "Remove", Left = 210, Top = 282, Width = 75, Height = 24, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _btnRemoveGuaranteedDrop.Click += (s, e) => OnRemoveGuaranteedDrop();
            _grpEntityCard.Controls.Add(_btnRemoveGuaranteedDrop);

            // --- Card 2B: Movement & Patrol Route ---
            _grpMovementCard = new GroupBox { Text = "🧭 Movement & Route Behavior", Left = 10, Top = 490, Width = 295, Height = 230, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold), Visible = false };
            _inspectorPanel.Controls.Add(_grpMovementCard);

            var lblMM = new Label { Text = "Movement Style:", Left = 10, Top = 22, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _grpMovementCard.Controls.Add(lblMM);

            _cbMovementMode = new ComboBox { Left = 105, Top = 18, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _cbMovementMode.Items.AddRange(new object[] { "Static (No Movement)", "Patrol Route (Specific Path)", "Wander (Area Leash)" });
            _cbMovementMode.SelectedIndex = 0;
            _cbMovementMode.SelectedIndexChanged += (s, e) => OnMovementModeChanged();
            _grpMovementCard.Controls.Add(_cbMovementMode);

            // Patrol Route specific controls:
            _lblCircuit = new Label { Text = "Circuit:", Left = 10, Top = 48, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _grpMovementCard.Controls.Add(_lblCircuit);

            _cbCircuitStyle = new ComboBox { Left = 58, Top = 45, Width = 105, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular), Visible = false };
            _cbCircuitStyle.Items.AddRange(new object[] { "Loop", "PingPong", "Once" });
            _cbCircuitStyle.SelectedIndex = 0;
            _cbCircuitStyle.SelectedIndexChanged += (s, e) => OnMovementPropertyChanged();
            _grpMovementCard.Controls.Add(_cbCircuitStyle);

            _lblPause = new Label { Text = "Wait (s):", Left = 170, Top = 48, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _grpMovementCard.Controls.Add(_lblPause);

            _nudWaypointPause = new NumericUpDown { Left = 220, Top = 45, Width = 65, DecimalPlaces = 1, Increment = 0.5m, Minimum = 0, Maximum = 60, Value = 2, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular), Visible = false };
            _nudWaypointPause.ValueChanged += (s, e) => OnMovementPropertyChanged();
            _grpMovementCard.Controls.Add(_nudWaypointPause);

            _btnRecordRoute = new Button { Text = "✏️ Record Path (Click Tiles)", Left = 10, Top = 72, Width = 195, Height = 25, BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold), Visible = false };
            _btnRecordRoute.Click += (s, e) => ToggleRecordRoute();
            _grpMovementCard.Controls.Add(_btnRecordRoute);

            _btnClearRoute = new Button { Text = "Clear", Left = 212, Top = 72, Width = 73, Height = 25, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _btnClearRoute.Click += (s, e) => ClearPatrolWaypoints();
            _grpMovementCard.Controls.Add(_btnClearRoute);

            _lbWaypoints = new ListBox { Left = 10, Top = 102, Width = 275, Height = 115, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _grpMovementCard.Controls.Add(_lbWaypoints);

            // Wander specific controls:
            // Wander specific controls (Painted Territory & Leash):
            _btnPaintWanderArea = new Button { Text = "🖌️ Paint Territory (Click Tiles)", Left = 10, Top = 48, Width = 195, Height = 25, BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold), Visible = false };
            _btnPaintWanderArea.Click += (s, e) => TogglePaintWanderArea();
            _grpMovementCard.Controls.Add(_btnPaintWanderArea);

            _btnClearWanderArea = new Button { Text = "Clear", Left = 212, Top = 48, Width = 73, Height = 25, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _btnClearWanderArea.Click += (s, e) => ClearWanderArea();
            _grpMovementCard.Controls.Add(_btnClearWanderArea);

            _lblWanderTilesCount = new Label { Text = "Territory: (0 painted tiles)", Left = 10, Top = 77, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Italic), ForeColor = Color.DarkSlateBlue, Visible = false };
            _grpMovementCard.Controls.Add(_lblWanderTilesCount);

            _lblMaxChase = new Label { Text = "Kite / Disengage Dist (tiles):", Left = 10, Top = 98, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _grpMovementCard.Controls.Add(_lblMaxChase);

            _nudMaxChase = new NumericUpDown { Left = 190, Top = 95, Width = 65, Minimum = 1, Maximum = 50, Value = 5, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular), Visible = false };
            _nudMaxChase.ValueChanged += (s, e) => OnMovementPropertyChanged();
            _grpMovementCard.Controls.Add(_nudMaxChase);

            _chkReturnPreCombat = new CheckBox { Text = "Return to pre-combat square on disengage", Left = 10, Top = 122, AutoSize = true, Checked = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _chkReturnPreCombat.CheckedChanged += (s, e) => OnMovementPropertyChanged();
            _grpMovementCard.Controls.Add(_chkReturnPreCombat);

            _lblWanderInt = new Label { Text = "Idle Time between wanders (s):", Left = 10, Top = 146, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), Visible = false };
            _grpMovementCard.Controls.Add(_lblWanderInt);

            _nudWanderInterval = new NumericUpDown { Left = 190, Top = 144, Width = 65, DecimalPlaces = 1, Increment = 0.5m, Minimum = 0.5m, Maximum = 60, Value = 3, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular), Visible = false };
            _nudWanderInterval.ValueChanged += (s, e) => OnMovementPropertyChanged();
            _grpMovementCard.Controls.Add(_nudWanderInterval);

            _lblNavStatus = new Label { Text = "Status: Ready", Left = 10, Top = 172, Width = 275, Height = 28, Font = new Font(FontFamily.GenericSansSerif, 7f, FontStyle.Italic), ForeColor = Color.DarkSlateBlue, Visible = false };
            _grpMovementCard.Controls.Add(_lblNavStatus);

            _btnSimulateWanderStep = new Button { Text = "🎲 Step Wander Cycle (Test Move)", Left = 10, Top = 204, Width = 275, Height = 26, BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold), Visible = false };
            _btnSimulateWanderStep.Click += (s, e) => OnStepWanderClicked();
            _grpMovementCard.Controls.Add(_btnSimulateWanderStep);

            // --- Card 3: Transition & Security Settings ---
            _grpTransition = new GroupBox { Text = "🚪 Transition & Security Settings", Left = 10, Top = 490, Width = 295, Height = 340, Visible = false, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold) };
            _inspectorPanel.Controls.Add(_grpTransition);

            var lblTDest = new Label { Text = "Target Dungeon / Level:", Left = 10, Top = 20, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold) };
            _grpTransition.Controls.Add(lblTDest);

            _cbTransTargetDungeon = new ComboBox { Left = 10, Top = 38, Width = 165, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8f) };
            _cbTransTargetDungeon.SelectedIndexChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_cbTransTargetDungeon);

            _nudTransTargetLevel = new NumericUpDown { Left = 182, Top = 38, Width = 95, Minimum = -1, Maximum = 99, Value = -1, Font = new Font("Segoe UI", 8f) };
            _nudTransTargetLevel.ValueChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_nudTransTargetLevel);

            var lblTCoords = new Label { Text = "Target X, Y & Facing:", Left = 10, Top = 65, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold) };
            _grpTransition.Controls.Add(lblTCoords);

            _nudTransTargetX = new NumericUpDown { Left = 10, Top = 83, Width = 55, Minimum = -1, Maximum = 200, Value = -1, Font = new Font("Segoe UI", 8f) };
            _nudTransTargetX.ValueChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_nudTransTargetX);

            _nudTransTargetY = new NumericUpDown { Left = 70, Top = 83, Width = 55, Minimum = -1, Maximum = 200, Value = -1, Font = new Font("Segoe UI", 8f) };
            _nudTransTargetY.ValueChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_nudTransTargetY);

            _cbTransTargetFacing = new ComboBox { Left = 132, Top = 83, Width = 145, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8f) };
            _cbTransTargetFacing.Items.AddRange(new object[] { "Keep", "North", "East", "South", "West" });
            _cbTransTargetFacing.SelectedIndex = 0;
            _cbTransTargetFacing.SelectedIndexChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_cbTransTargetFacing);

            // Locks & Keys
            _chkTransLocked = new CheckBox { Text = "Is Locked (Requires Key)", Left = 10, Top = 112, AutoSize = true, Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
            _chkTransLocked.CheckedChanged += (s, e) =>
            {
                _cbTransKey.Enabled = _chkTransLocked.Checked;
                OnTransitionPropertyChanged();
            };
            _grpTransition.Controls.Add(_chkTransLocked);

            _cbTransKey = new ComboBox { Left = 10, Top = 134, Width = 267, DropDownStyle = ComboBoxStyle.DropDown, Font = new Font("Segoe UI", 8f), Enabled = false };
            _cbTransKey.TextChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_cbTransKey);

            // Traps
            _chkTransTrapped = new CheckBox { Text = "Is Trapped", Left = 10, Top = 162, AutoSize = true, Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
            _chkTransTrapped.CheckedChanged += (s, e) =>
            {
                bool en = _chkTransTrapped.Checked;
                _cbTransTrapType.Enabled = en;
                _nudTransTrapDamage.Enabled = en;
                _chkTransRecurring.Enabled = en;
                _nudTransJiggleChance.Enabled = en;
                _nudTransOpenChance.Enabled = en;
                OnTransitionPropertyChanged();
            };
            _grpTransition.Controls.Add(_chkTransTrapped);

            _cbTransTrapType = new ComboBox { Left = 10, Top = 184, Width = 165, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8f), Enabled = false };
            _cbTransTrapType.Items.AddRange(new object[] { "PoisonDart", "Spike", "ElectricShock", "ArcaneWard", "FlameJet", "Gas", "Alarm", "Pitfall" });
            _cbTransTrapType.SelectedIndex = 0;
            _cbTransTrapType.SelectedIndexChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_cbTransTrapType);

            _nudTransTrapDamage = new NumericUpDown { Left = 182, Top = 184, Width = 95, Minimum = 0, Maximum = 500, Value = 10, Font = new Font("Segoe UI", 8f), Enabled = false };
            _nudTransTrapDamage.ValueChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_nudTransTrapDamage);

            _chkTransRecurring = new CheckBox { Text = "Recurring (Magic / Shock Ward)", Left = 10, Top = 210, AutoSize = true, Font = new Font("Segoe UI", 7.5f), Enabled = false };
            _chkTransRecurring.CheckedChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_chkTransRecurring);

            var lblChances = new Label { Text = "Jiggle / Open %:", Left = 10, Top = 232, AutoSize = true, Font = new Font("Segoe UI", 7.5f) };
            _grpTransition.Controls.Add(lblChances);

            _nudTransJiggleChance = new NumericUpDown { Left = 110, Top = 230, Width = 70, Minimum = 0, Maximum = 100, Value = 33, Font = new Font("Segoe UI", 7.5f), Enabled = false };
            _nudTransJiggleChance.ValueChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_nudTransJiggleChance);

            _nudTransOpenChance = new NumericUpDown { Left = 188, Top = 230, Width = 89, Minimum = 0, Maximum = 100, Value = 100, Font = new Font("Segoe UI", 7.5f), Enabled = false };
            _nudTransOpenChance.ValueChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_nudTransOpenChance);

            // Behavior
            _chkTransOpen = new CheckBox { Text = "Starts Open (Unblocked)", Left = 10, Top = 258, AutoSize = true, Font = new Font("Segoe UI", 8f) };
            _chkTransOpen.CheckedChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_chkTransOpen);

            _chkTransAuto = new CheckBox { Text = "Auto-Transition on Step (Stairs/Pit)", Left = 10, Top = 282, AutoSize = true, Font = new Font("Segoe UI", 8f) };
            _chkTransAuto.CheckedChanged += (s, e) => OnTransitionPropertyChanged();
            _grpTransition.Controls.Add(_chkTransAuto);

            // --- Card 4: Ground 4 Sub-Square Item Slots ---
            _grpSubSquareItems = new GroupBox { Text = "📦 4 Sub-Square Item Slots", Left = 10, Top = 840, Width = 295, Height = 215, Visible = false, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold) };
            _inspectorPanel.Controls.Add(_grpSubSquareItems);

            string[] slotNames = { "Q1 (Top-Left / NW)", "Q2 (Top-Right / NE)", "Q3 (Bottom-Left / SW)", "Q4 (Bottom-Right / SE)" };
            int curY = 20;

            for (int i = 0; i < 4; i++)
            {
                int slotIdx = i;
                var lblSlot = new Label { Text = slotNames[i], Left = 10, Top = curY, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
                _grpSubSquareItems.Controls.Add(lblSlot);

                _cbSlotItems[i] = new ComboBox { Left = 10, Top = curY + 16, Width = 210, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                _cbSlotItems[i].SelectedIndexChanged += (s, e) => OnSlotItemChanged(slotIdx);
                _grpSubSquareItems.Controls.Add(_cbSlotItems[i]);

                _btnClearSlots[i] = new Button { Text = "Clear", Left = 226, Top = curY + 15, Width = 58, Height = 24, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
                _btnClearSlots[i].Click += (s, e) => ClearSlot(slotIdx);
                _grpSubSquareItems.Controls.Add(_btnClearSlots[i]);

                curY += 46;
            }

            // --- Card 5: Dungeon & Level Specifics / Loot Table Manager ---
            _grpDungeonInspector = new GroupBox { Text = "🏰 Dungeon & Level Inspector", Left = 10, Top = 10, Width = 295, Height = 675, Visible = false, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold) };
            _inspectorPanel.Controls.Add(_grpDungeonInspector);

            _lblDungeonHeader = new Label { Text = "Dungeon: The Upper Crypts", Left = 10, Top = 20, Width = 275, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40) };
            _grpDungeonInspector.Controls.Add(_lblDungeonHeader);

            _lblDungeonDetails = new Label { Text = "Level 1 | Dimensions: 24x16", Left = 10, Top = 40, Width = 275, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular), ForeColor = Color.DarkSlateGray };
            _grpDungeonInspector.Controls.Add(_lblDungeonDetails);

            _btnBackToTile = new Button { Text = "↩ Back to Map Tile Inspector", Left = 10, Top = 62, Width = 275, Height = 25, BackColor = Color.FromArgb(70, 70, 75), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _btnBackToTile.Click += (s, e) => CloseDungeonInspector();
            _grpDungeonInspector.Controls.Add(_btnBackToTile);

            var grpLoot = new GroupBox { Text = "💰 Loot Tables & Drop Pools", Left = 8, Top = 92, Width = 279, Height = 570, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Bold) };
            _grpDungeonInspector.Controls.Add(grpLoot);

            var lblTblSel = new Label { Text = "Active Table:", Left = 10, Top = 20, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            grpLoot.Controls.Add(lblTblSel);

            _cbDungeonLootTables = new ComboBox { Left = 10, Top = 38, Width = 185, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _cbDungeonLootTables.SelectedIndexChanged += (s, e) => OnDungeonLootTableSelected();
            grpLoot.Controls.Add(_cbDungeonLootTables);

            _btnNewLootTable = new Button { Text = "+ New", Left = 200, Top = 37, Width = 69, Height = 24, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _btnNewLootTable.Click += (s, e) => PromptCreateNewLootTable();
            grpLoot.Controls.Add(_btnNewLootTable);

            var lblTName = new Label { Text = "Table Name:", Left = 10, Top = 68, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            grpLoot.Controls.Add(lblTName);

            _txtLootTableName = new TextBox { Left = 10, Top = 86, Width = 259, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _txtLootTableName.TextChanged += (s, e) => OnLootTablePropsChanged();
            grpLoot.Controls.Add(_txtLootTableName);

            var lblGold = new Label { Text = "Gold Min..Max & Rolls:", Left = 10, Top = 114, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            grpLoot.Controls.Add(lblGold);

            _nudLootMinGold = new NumericUpDown { Left = 10, Top = 132, Width = 65, Minimum = 0, Maximum = 10000, Value = 5, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _nudLootMinGold.ValueChanged += (s, e) => OnLootTablePropsChanged();
            grpLoot.Controls.Add(_nudLootMinGold);

            _nudLootMaxGold = new NumericUpDown { Left = 80, Top = 132, Width = 65, Minimum = 0, Maximum = 10000, Value = 25, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _nudLootMaxGold.ValueChanged += (s, e) => OnLootTablePropsChanged();
            grpLoot.Controls.Add(_nudLootMaxGold);

            _nudLootRolls = new NumericUpDown { Left = 180, Top = 132, Width = 55, Minimum = 1, Maximum = 10, Value = 1, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
            _nudLootRolls.ValueChanged += (s, e) => OnLootTablePropsChanged();
            grpLoot.Controls.Add(_nudLootRolls);

            var lblDropPool = new Label { Text = "Drop Items (Weight, % Chance, Min..Max):", Left = 10, Top = 160, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            grpLoot.Controls.Add(lblDropPool);

            _dgvLootEntries = new DataGridView
            {
                Left = 10,
                Top = 178,
                Width = 259,
                Height = 270,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular)
            };
            _dgvLootEntries.Columns.Add("Item", "Item ID");
            _dgvLootEntries.Columns.Add("Weight", "Wt");
            _dgvLootEntries.Columns.Add("Chance", "Ch%");
            _dgvLootEntries.Columns.Add("Min", "Min");
            _dgvLootEntries.Columns.Add("Max", "Max");
            _dgvLootEntries.CellValueChanged += (s, e) => OnLootTableGridChanged();
            grpLoot.Controls.Add(_dgvLootEntries);

            _cbLootAddItem = new ComboBox { Left = 10, Top = 456, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            grpLoot.Controls.Add(_cbLootAddItem);

            _btnLootAddItem = new Button { Text = "+ Add", Left = 165, Top = 455, Width = 50, Height = 23, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _btnLootAddItem.Click += (s, e) => AddEntryToCurrentLootTable();
            grpLoot.Controls.Add(_btnLootAddItem);

            _btnLootRemoveItem = new Button { Text = "Del", Left = 220, Top = 455, Width = 48, Height = 23, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _btnLootRemoveItem.Click += (s, e) => RemoveEntryFromCurrentLootTable();
            grpLoot.Controls.Add(_btnLootRemoveItem);

            _btnTestRollTable = new Button { Text = "🎲 Test Roll (Simulate Drops)", Left = 10, Top = 488, Width = 259, Height = 28, BackColor = Color.FromArgb(46, 139, 87), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Bold) };
            _btnTestRollTable.Click += (s, e) => SimulateTestRoll();
            grpLoot.Controls.Add(_btnTestRollTable);

            _btnDeleteLootTable = new Button { Text = "✕ Delete Table", Left = 10, Top = 522, Width = 259, Height = 24, ForeColor = Color.DarkRed, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Regular) };
            _btnDeleteLootTable.Click += (s, e) => DeleteCurrentLootTable();
            grpLoot.Controls.Add(_btnDeleteLootTable);

            // Center Panel (Map Painter Canvas)
            _mapScrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(30, 30, 30) };
            tab.Controls.Add(_mapScrollPanel);
            _mapScrollPanel.BringToFront();

            _painter = new MapPainterControl { Location = new Point(10, 10), CellSize = 36, CurrentTool = PainterTool.Select };
            _painter.OnTileSelected += (x, y, tile) =>
            {
                if (_isRecordingRoute && _currentLevel != null && _painter.SelectedX >= 0 && _painter.SelectedY >= 0)
                {
                    var baseTile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
                    if (baseTile.CenterOccupant is MonsterInstance m)
                    {
                        if (x != m.TileX || y != m.TileY)
                        {
                            m.PatrolWaypoints.Add(new Point(x, y));
                            UpdateWaypointsList(m);
                            _painter.Invalidate();
                            return;
                        }
                    }
                }
                else if (_isPaintingWanderArea && _currentLevel != null && _painter.SelectedX >= 0 && _painter.SelectedY >= 0)
                {
                    var baseTile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
                    if (baseTile.CenterOccupant is MonsterInstance m)
                    {
                        int existingIdx = m.WanderAreaTiles.FindIndex(p => p.X == x && p.Y == y);
                        if (existingIdx >= 0)
                            m.WanderAreaTiles.RemoveAt(existingIdx);
                        else
                            m.WanderAreaTiles.Add(new Point(x, y));

                        UpdateWanderLabels(m);
                        _painter.Invalidate();
                        return;
                    }
                }
                PopulateTileInspector(x, y, tile);
            };
            _painter.OnUndoStateChanged += UpdateUndoButtons;
            _rbToolSelect.Checked = true;
            _mapScrollPanel.Controls.Add(_painter);
        }

        private void BuildTabItemCreator()
        {
            var tab = new TabPage("Item & Object Creator (items.json)");
            _tabs.TabPages.Add(tab);

            var split = new SplitContainer { Dock = DockStyle.Fill };
            tab.Controls.Add(split);
            Shown += (s, e) =>
            {
                try
                {
                    split.SplitterDistance = 350;
                }
                catch { }
            };

            // ================= LEFT CATALOG PANEL =================
            var pnlLeft = new Panel { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(pnlLeft);

            var pnlLeftTop = new Panel { Dock = DockStyle.Top, Height = 84, Padding = new Padding(8, 6, 8, 4) };
            pnlLeft.Controls.Add(pnlLeftTop);

            var lblCat = new Label
            {
                Text = "Project Items & Objects Catalog",
                Dock = DockStyle.Top,
                Height = 22,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            pnlLeftTop.Controls.Add(lblCat);
            lblCat.BringToFront();

            _cbCategoryFilter = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5f)
            };
            _cbCategoryFilter.Items.AddRange(new object[] {
                "[ All Categories ]",
                "Weapons",
                "Armor & Shields",
                "Accessories",
                "Consumables & Potions",
                "Containers & Storage",
                "Props & Hazards",
                "Templates & Blueprints"
            });
            _cbCategoryFilter.SelectedIndex = 0;
            _cbCategoryFilter.SelectedIndexChanged += (s, e) => FilterAndDisplayItems();
            pnlLeftTop.Controls.Add(_cbCategoryFilter);
            _cbCategoryFilter.BringToFront();

            var pnlSearch = new Panel { Dock = DockStyle.Bottom, Height = 26 };
            _txtItemSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f)
            };
            _txtItemSearch.TextChanged += (s, e) => FilterAndDisplayItems();
            pnlSearch.Controls.Add(_txtItemSearch);

            _lblItemCount = new Label
            {
                Dock = DockStyle.Right,
                Width = 110,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.Gray,
                Text = "0 items"
            };
            pnlSearch.Controls.Add(_lblItemCount);
            pnlLeftTop.Controls.Add(pnlSearch);

            _lbItems = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                IntegralHeight = false
            };
            _lbItems.SelectedIndexChanged += (s, e) => OnItemSelectedInList();
            pnlLeft.Controls.Add(_lbItems);
            _lbItems.BringToFront();

            // Left bottom buttons
            var pnlItemBtns = new Panel { Dock = DockStyle.Bottom, Height = 74, Padding = new Padding(6) };
            pnlLeft.Controls.Add(pnlItemBtns);

            var menuPresets = new ContextMenuStrip();
            void AddPresetMenuItem(ToolStripMenuItem parent, string text, Action onClick)
            {
                var item = new ToolStripMenuItem(text);
                item.Click += (s, e) => onClick();
                parent.DropDownItems.Add(item);
            }

            // Presets grouped by category (from NCMMH seed)
            var mWeapons = new ToolStripMenuItem("Weapons");
            menuPresets.Items.Add(mWeapons);
            AddPresetMenuItem(mWeapons, "Short Sword (1H Melee)", () => CreateFromPreset("sword_short"));
            AddPresetMenuItem(mWeapons, "Steel Long Sword (1H Melee)", () => CreateFromPreset("sword_long"));
            AddPresetMenuItem(mWeapons, "Claymore Great Sword (2H Melee)", () => CreateFromPreset("sword_great"));
            AddPresetMenuItem(mWeapons, "Stiletto Dagger (1H Melee)", () => CreateFromPreset("dagger"));
            AddPresetMenuItem(mWeapons, "Composite Hunting Bow (2H Ranged)", () => CreateFromPreset("bow"));
            AddPresetMenuItem(mWeapons, "Heavy Crossbow (2H Ranged)", () => CreateFromPreset("crossbow"));
            AddPresetMenuItem(mWeapons, "Staff of the Fire Pyre (2H Magic Fire)", () => CreateFromPreset("staff_fire"));
            AddPresetMenuItem(mWeapons, "Arcane Wand of Sparks (1H Magic)", () => CreateFromPreset("wand_arcane"));

            var mArmor = new ToolStripMenuItem("Armor & Shields");
            menuPresets.Items.Add(mArmor);
            AddPresetMenuItem(mArmor, "Reinforced Tower Shield (OffHand)", () => CreateFromPreset("shield_tower"));
            AddPresetMenuItem(mArmor, "Paladin's Visor Helm (Head)", () => CreateFromPreset("helm_paladin"));
            AddPresetMenuItem(mArmor, "Crusader's Breastplate (Chest)", () => CreateFromPreset("chest_crusader"));
            AddPresetMenuItem(mArmor, "Ranger's Boots (Feet)", () => CreateFromPreset("boots_ranger"));

            var mAcc = new ToolStripMenuItem("Accessories");
            menuPresets.Items.Add(mAcc);
            AddPresetMenuItem(mAcc, "Ring of Flame Resistance", () => CreateFromPreset("ring_fire"));
            AddPresetMenuItem(mAcc, "Amulet of Divine Blessing", () => CreateFromPreset("amulet_holy"));

            var mConsumables = new ToolStripMenuItem("Consumables & Potions");
            menuPresets.Items.Add(mConsumables);
            AddPresetMenuItem(mConsumables, "Potion of Healing (50 HP)", () => CreateFromPreset("potion_hp"));
            AddPresetMenuItem(mConsumables, "Potion of Mana (40 MP)", () => CreateFromPreset("potion_mp"));
            AddPresetMenuItem(mConsumables, "Alchemical Antidote", () => CreateFromPreset("potion_antidote"));
            AddPresetMenuItem(mConsumables, "Scroll of Fireball", () => CreateFromPreset("scroll_fireball"));
            AddPresetMenuItem(mConsumables, "Scroll of Lightning", () => CreateFromPreset("scroll_lightning"));

            var mContainers = new ToolStripMenuItem("Containers & Storage");
            menuPresets.Items.Add(mContainers);
            AddPresetMenuItem(mContainers, "Wooden Chest", () => CreateFromPreset("chest_wood"));
            AddPresetMenuItem(mContainers, "Trapped Poison Chest", () => CreateFromPreset("chest_trapped"));
            AddPresetMenuItem(mContainers, "Ironbound Locked Chest", () => CreateFromPreset("chest_locked"));

            var mProps = new ToolStripMenuItem("Props & Hazards");
            menuPresets.Items.Add(mProps);
            AddPresetMenuItem(mProps, "Small Rock", () => CreateFromPreset("prop_rock"));
            AddPresetMenuItem(mProps, "Heavy Boulder (Blocking)", () => CreateFromPreset("prop_boulder"));
            AddPresetMenuItem(mProps, "Loose Rubble (Difficult)", () => CreateFromPreset("prop_rubble"));
            AddPresetMenuItem(mProps, "Floor Spikes (Hazard)", () => CreateFromPreset("prop_spikes"));
            AddPresetMenuItem(mProps, "Iron Key", () => CreateFromPreset("prop_key"));

            var btnNewPreset = new Button
            {
                Text = "★ New from Template ▼",
                Left = 4,
                Top = 4,
                Width = 175,
                Height = 30,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnNewPreset.Click += (s, e) => menuPresets.Show(btnNewPreset, new Point(0, btnNewPreset.Height));
            pnlItemBtns.Controls.Add(btnNewPreset);

            var btnNewBlank = new Button { Text = "+ Blank", Left = 184, Top = 4, Width = 70, Height = 30, Font = new Font("Segoe UI", 8.5f) };
            btnNewBlank.Click += (s, e) => CreateNewBlankItem();
            pnlItemBtns.Controls.Add(btnNewBlank);

            var btnDupItem = new Button { Text = "Clone", Left = 259, Top = 4, Width = 70, Height = 30, Font = new Font("Segoe UI", 8.5f) };
            btnDupItem.Click += (s, e) => DuplicateItem();
            pnlItemBtns.Controls.Add(btnDupItem);

            var btnDeleteItem = new Button { Text = "Delete", Left = 4, Top = 38, Width = 80, Height = 28, Font = new Font("Segoe UI", 8.5f) };
            btnDeleteItem.Click += (s, e) => DeleteSelectedItem();
            pnlItemBtns.Controls.Add(btnDeleteItem);

            var btnDesigner = new Button
            {
                Text = "Class & Schema Designer...",
                Left = 90,
                Top = 38,
                Width = 239,
                Height = 28,
                BackColor = Color.FromArgb(70, 80, 95),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnDesigner.Click += (s, e) =>
            {
                using (var dlg = new ClassTemplateDesignerDialog(_projectDir))
                {
                    dlg.ShowDialog(this);
                    PopulateItemsTab();
                }
            };
            pnlItemBtns.Controls.Add(btnDesigner);

            // ================= RIGHT DETAILS FORM PANEL =================
            var formPnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(15) };
            split.Panel2.Controls.Add(formPnl);

            int curY = 8;
            GroupBox CreateCard(string title, int height)
            {
                var grp = new GroupBox
                {
                    Text = title,
                    Left = 12,
                    Top = curY,
                    Width = 635,
                    Height = height,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 30, 30)
                };
                formPnl.Controls.Add(grp);
                curY += height + 10;
                return grp;
            }

            Label AddCardLabel(Control parent, string text, int left, int top)
            {
                var l = new Label
                {
                    Text = text,
                    Left = left,
                    Top = top,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(50, 50, 50)
                };
                parent.Controls.Add(l);
                return l;
            }

            // 1. IDENTITY & ARCHETYPE CARD
            var grpIdentity = CreateCard("Identification & Archetype", 205);
            AddCardLabel(grpIdentity, "Item ID / Tag:", 15, 22);
            _txtItemId = new TextBox { Left = 15, Top = 40, Width = 180, Font = new Font("Segoe UI", 9f) };
            grpIdentity.Controls.Add(_txtItemId);

            AddCardLabel(grpIdentity, "Display Name:", 205, 22);
            _txtItemName = new TextBox { Left = 205, Top = 40, Width = 410, Font = new Font("Segoe UI", 9f) };
            grpIdentity.Controls.Add(_txtItemName);

            AddCardLabel(grpIdentity, "Category:", 15, 68);
            _cbCategory = new ComboBox { Left = 15, Top = 86, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            _cbCategory.Items.AddRange(new object[] { "Weapon", "Armor", "Accessory", "Consumable", "Container", "Prop" });
            _cbCategory.SelectedIndexChanged += (s, e) => UpdateCategoryPanelsVisibility();
            grpIdentity.Controls.Add(_cbCategory);

            AddCardLabel(grpIdentity, "Sub-Type:", 175, 68);
            _cbSubCategory = new ComboBox { Left = 175, Top = 86, Width = 150, DropDownStyle = ComboBoxStyle.DropDown, Font = new Font("Segoe UI", 8.5f) };
            _cbSubCategory.Items.AddRange(new object[] { "Sword", "Dagger", "Bow", "Crossbow", "Staff", "Wand", "Shield", "Helmet", "Chest", "Boots", "Ring", "Neck", "Potion", "Scroll", "Chest", "Rock", "Boulder", "Rubble", "Spikes", "Key" });
            grpIdentity.Controls.Add(_cbSubCategory);

            AddCardLabel(grpIdentity, "Inherits From Archetype:", 335, 68);
            _cbItemInherit = new ComboBox { Left = 335, Top = 86, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            grpIdentity.Controls.Add(_cbItemInherit);

            AddCardLabel(grpIdentity, "Description / Lore:", 15, 114);
            _txtDescription = new TextBox { Left = 15, Top = 132, Width = 600, Height = 38, Multiline = true, Font = new Font("Segoe UI", 8.5f) };
            grpIdentity.Controls.Add(_txtDescription);

            _chkIsTemplate = new CheckBox
            {
                Text = "Is Reusable Generic Template / Blueprint (other items can inherit this)",
                Left = 15,
                Top = 176,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f)
            };
            grpIdentity.Controls.Add(_chkIsTemplate);

            // 2. PHYSICAL & ECONOMICS CARD
            var grpPhysical = CreateCard("Physical & Economics", 110);
            AddCardLabel(grpPhysical, "Weight (lbs):", 15, 24);
            _nudWeight = new NumericUpDown { Left = 15, Top = 42, Width = 85, DecimalPlaces = 1, Increment = 0.5m, Maximum = 1000m, Font = new Font("Segoe UI", 8.5f) };
            grpPhysical.Controls.Add(_nudWeight);

            AddCardLabel(grpPhysical, "Sell Value (Gold):", 115, 24);
            _nudValue = new NumericUpDown { Left = 115, Top = 42, Width = 95, Maximum = 99999m, Increment = 5m, Font = new Font("Segoe UI", 8.5f) };
            grpPhysical.Controls.Add(_nudValue);

            _chkCollectable = new CheckBox { Text = "Can Pick Up (Inventory)", Left = 225, Top = 30, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            grpPhysical.Controls.Add(_chkCollectable);

            _chkPassThru = new CheckBox { Text = "Pass-Thru (Walkable)", Left = 225, Top = 52, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            grpPhysical.Controls.Add(_chkPassThru);

            AddCardLabel(grpPhysical, "Visual Sprite:", 440, 24);
            _cbSpriteKind = new ComboBox { Left = 440, Top = 42, Width = 175, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            _cbSpriteKind.Items.AddRange(new[] { "weapon", "shield", "armor", "potion", "scroll", "chest", "key", "rock" });
            grpPhysical.Controls.Add(_cbSpriteKind);

            AddCardLabel(grpPhysical, "Color Tint:", 15, 74);
            _pnlItemColorSwatch = new Panel { Left = 90, Top = 73, Width = 35, Height = 22, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Gray };
            grpPhysical.Controls.Add(_pnlItemColorSwatch);

            var btnPickColor = new Button { Text = "Choose Tint...", Left = 135, Top = 71, Width = 110, Height = 26, Font = new Font("Segoe UI", 8.5f) };
            btnPickColor.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = _currentItemColor })
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        _currentItemColor = cd.Color;
                        _pnlItemColorSwatch.BackColor = cd.Color;
                    }
                }
            };
            grpPhysical.Controls.Add(btnPickColor);

            // 3. WEAPON & COMBAT STATS CARD
            _grpWeapon = CreateCard("Weapon & Combat Stats", 125);
            AddCardLabel(_grpWeapon, "Attack Style:", 15, 24);
            _rbAttackMelee = new RadioButton { Text = "Melee", Left = 100, Top = 23, Width = 70, Checked = true, Font = new Font("Segoe UI", 8.5f) };
            _rbAttackRanged = new RadioButton { Text = "Ranged", Left = 175, Top = 23, Width = 75, Font = new Font("Segoe UI", 8.5f) };
            _rbAttackCast = new RadioButton { Text = "Cast / Skill", Left = 255, Top = 23, Width = 90, Font = new Font("Segoe UI", 8.5f) };
            _rbAttackCast.CheckedChanged += (s, e) =>
            {
                if (_rbAttackCast.Checked) _chkHasEffect.Checked = true;
            };
            _grpWeapon.Controls.Add(_rbAttackMelee);
            _grpWeapon.Controls.Add(_rbAttackRanged);
            _grpWeapon.Controls.Add(_rbAttackCast);

            AddCardLabel(_grpWeapon, "Grip:", 360, 24);
            _cbHands = new ComboBox { Left = 405, Top = 21, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            _cbHands.Items.AddRange(new object[] { "1-Handed", "2-Handed" });
            _cbHands.SelectedIndex = 0;
            _grpWeapon.Controls.Add(_cbHands);

            AddCardLabel(_grpWeapon, "Min Damage:", 15, 62);
            _nudMinDamage = new NumericUpDown { Left = 15, Top = 82, Width = 75, Minimum = 0, Maximum = 999, Value = 3, Font = new Font("Segoe UI", 8.5f) };
            _grpWeapon.Controls.Add(_nudMinDamage);

            AddCardLabel(_grpWeapon, "Max Damage:", 105, 62);
            _nudMaxDamage = new NumericUpDown { Left = 105, Top = 82, Width = 75, Minimum = 1, Maximum = 999, Value = 7, Font = new Font("Segoe UI", 8.5f) };
            _grpWeapon.Controls.Add(_nudMaxDamage);

            AddCardLabel(_grpWeapon, "Range (tiles):", 195, 62);
            _nudRange = new NumericUpDown { Left = 195, Top = 82, Width = 70, Minimum = 1, Maximum = 20, Value = 1, Font = new Font("Segoe UI", 8.5f) };
            _grpWeapon.Controls.Add(_nudRange);

            AddCardLabel(_grpWeapon, "Ammo Required (e.g. arrow, cells):", 280, 62);
            _txtAmmoType = new TextBox { Left = 280, Top = 82, Width = 150, Font = new Font("Segoe UI", 8.5f) };
            _grpWeapon.Controls.Add(_txtAmmoType);

            // 4. AFFECTS & MODIFIERS CARD (Genre-Agnostic: enchantments, plasma coils, procs)
            _grpEffects = CreateCard("Affects & Energy Modifiers", 120);
            _chkHasEffect = new CheckBox
            {
                Text = "Imbued with Special Affect, Energy Type, or Modifier",
                Left = 15,
                Top = 22,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            _chkHasEffect.CheckedChanged += (s, e) => UpdateEffectsControlsState();
            _grpEffects.Controls.Add(_chkHasEffect);

            AddCardLabel(_grpEffects, "Energy / Damage Type:", 15, 52);
            _cbEffectType = new ComboBox { Left = 15, Top = 72, Width = 115, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            _cbEffectType.Items.AddRange(new object[] { "None", "Physical", "Fire", "Frost", "Lightning", "Acid/Toxic", "Plasma", "Laser", "Arcane", "EMP", "Sonic", "Void" });
            _cbEffectType.SelectedIndex = 0;
            _grpEffects.Controls.Add(_cbEffectType);

            AddCardLabel(_grpEffects, "Proc / Affect Name:", 140, 52);
            _txtProcEffect = new TextBox { Left = 140, Top = 72, Width = 150, Font = new Font("Segoe UI", 8.5f) };
            _grpEffects.Controls.Add(_txtProcEffect);

            AddCardLabel(_grpEffects, "Resource Cost:", 300, 52);
            _nudResourceCost = new NumericUpDown { Left = 300, Top = 72, Width = 80, Minimum = 0, Maximum = 999, Value = 0, Font = new Font("Segoe UI", 8.5f) };
            _grpEffects.Controls.Add(_nudResourceCost);

            AddCardLabel(_grpEffects, "Affect Power:", 390, 52);
            _nudEffectPower = new NumericUpDown { Left = 390, Top = 72, Width = 80, Minimum = 0, Maximum = 999, Value = 0, Font = new Font("Segoe UI", 8.5f) };
            _grpEffects.Controls.Add(_nudEffectPower);

            // 5. ARMOR & DEFENSE CARD
            _grpArmor = CreateCard("Armor & Defense", 90);
            AddCardLabel(_grpArmor, "Equip Slot:", 15, 24);
            _cbEquipSlot = new ComboBox { Left = 15, Top = 44, Width = 115, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            _cbEquipSlot.Items.AddRange(new object[] { "MainHand", "OffHand", "TwoHand", "Head", "Chest", "Legs", "Feet", "Hands", "Neck", "Ring", "Back" });
            _cbEquipSlot.SelectedIndex = 0;
            _grpArmor.Controls.Add(_cbEquipSlot);

            AddCardLabel(_grpArmor, "Armor Rating:", 140, 24);
            _nudArmorRating = new NumericUpDown { Left = 140, Top = 44, Width = 75, Minimum = 0, Maximum = 999, Value = 0, Font = new Font("Segoe UI", 8.5f) };
            _grpArmor.Controls.Add(_nudArmorRating);

            AddCardLabel(_grpArmor, "Energy Resist (%):", 225, 24);
            _nudEnergyResistance = new NumericUpDown { Left = 225, Top = 44, Width = 80, Minimum = 0, Maximum = 100, Value = 0, Font = new Font("Segoe UI", 8.5f) };
            _grpArmor.Controls.Add(_nudEnergyResistance);

            AddCardLabel(_grpArmor, "Resist Type:", 315, 24);
            _cbResistType = new ComboBox { Left = 315, Top = 44, Width = 115, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            _cbResistType.Items.AddRange(new object[] { "None", "Physical", "Fire", "Frost", "Lightning", "Acid/Toxic", "Plasma", "Laser", "Arcane", "EMP", "Sonic", "Void" });
            _cbResistType.SelectedIndex = 0;
            _grpArmor.Controls.Add(_cbResistType);

            // 6. CONSUMABLES CARD
            _grpConsumable = CreateCard("Consumable Effects", 85);
            AddCardLabel(_grpConsumable, "Consumable Type:", 15, 22);
            _cbConsumableType = new ComboBox { Left = 15, Top = 42, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 8.5f) };
            _cbConsumableType.Items.AddRange(new object[] { "None", "HealHP", "RestoreMP", "CurePoison", "Buff", "Teleport", "CastSpell", "Food" });
            _cbConsumableType.SelectedIndex = 0;
            _grpConsumable.Controls.Add(_cbConsumableType);

            AddCardLabel(_grpConsumable, "Effect Amount (HP / MP / Power):", 175, 22);
            _nudEffectAmount = new NumericUpDown { Left = 175, Top = 42, Width = 100, Minimum = 0, Maximum = 999, Value = 0, Font = new Font("Segoe UI", 8.5f) };
            _grpConsumable.Controls.Add(_nudEffectAmount);

            AddCardLabel(_grpConsumable, "Duration (Seconds / Turns):", 300, 22);
            _nudDuration = new NumericUpDown { Left = 300, Top = 42, Width = 90, Minimum = 0, Maximum = 300, Value = 0, Font = new Font("Segoe UI", 8.5f) };
            _grpConsumable.Controls.Add(_nudDuration);

            // 7. CONTAINER & SECURITY CARD
            _grpContainer = CreateCard("Container & Security", 125);
            _chkContainer = new CheckBox { Text = "Is Container (Chest, Sack, Barrel)", Left = 15, Top = 22, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            _grpContainer.Controls.Add(_chkContainer);

            AddCardLabel(_grpContainer, "Capacity Slots:", 240, 22);
            _nudCapacity = new NumericUpDown { Left = 330, Top = 20, Width = 65, Minimum = 1, Maximum = 100, Value = 12, Font = new Font("Segoe UI", 8.5f) };
            _grpContainer.Controls.Add(_nudCapacity);

            AddCardLabel(_grpContainer, "Trapped Effect (e.g. poison_dart):", 15, 55);
            _txtTrapped = new TextBox { Left = 15, Top = 75, Width = 160, Font = new Font("Segoe UI", 8.5f) };
            _grpContainer.Controls.Add(_txtTrapped);

            AddCardLabel(_grpContainer, "Locked Key ID (e.g. key_iron):", 190, 55);
            _txtLocked = new TextBox { Left = 190, Top = 75, Width = 150, Font = new Font("Segoe UI", 8.5f) };
            _grpContainer.Controls.Add(_txtLocked);

            _chkCursed = new CheckBox { Text = "Default Cursed", Left = 360, Top = 52, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            _grpContainer.Controls.Add(_chkCursed);

            AddCardLabel(_grpContainer, "Curse Effect Type:", 360, 75);
            _txtCurseType = new TextBox { Left = 360, Top = 93, Width = 160, Font = new Font("Segoe UI", 8.5f) };
            _grpContainer.Controls.Add(_txtCurseType);

            // 8. TERRAIN & HAZARDS CARD
            _grpTerrain = CreateCard("Terrain & Hazards", 85);
            _chkDifficult = new CheckBox { Text = "Difficult / Hazard Terrain", Left = 15, Top = 30, AutoSize = true, Font = new Font("Segoe UI", 8.5f) };
            _grpTerrain.Controls.Add(_chkDifficult);

            AddCardLabel(_grpTerrain, "Speed Penalty (0..1):", 200, 20);
            _nudSpeedPenalty = new NumericUpDown { Left = 200, Top = 40, Width = 70, DecimalPlaces = 2, Increment = 0.1m, Minimum = 0, Maximum = 1, Font = new Font("Segoe UI", 8.5f) };
            _grpTerrain.Controls.Add(_nudSpeedPenalty);

            AddCardLabel(_grpTerrain, "Hazard Damage:", 300, 20);
            _nudHazardDamage = new NumericUpDown { Left = 300, Top = 40, Width = 75, Minimum = 0, Maximum = 100, Font = new Font("Segoe UI", 8.5f) };
            _grpTerrain.Controls.Add(_nudHazardDamage);

            // 9. CUSTOM CLASS PROPERTIES CARD
            _grpCustomProps = CreateCard("Custom Class Schema Properties", 100);
            _pnlCustomPropsList = new Panel { Left = 15, Top = 22, Width = 600, Height = 40, AutoScroll = true };
            _grpCustomProps.Controls.Add(_pnlCustomPropsList);

            var btnAddProp = new Button { Text = "+ Add Custom Property", Left = 15, Top = 66, Width = 160, Height = 26, Font = new Font("Segoe UI", 8.5f) };
            btnAddProp.Click += (s, e) => PromptAddCustomProperty();
            _grpCustomProps.Controls.Add(btnAddProp);

            // 10. BOTTOM ACTION BAR
            var pnlActions = new Panel { Left = 12, Top = curY, Width = 635, Height = 55 };
            formPnl.Controls.Add(pnlActions);

            var btnApplyItem = new Button
            {
                Text = "Apply Changes to Item",
                Left = 0,
                Top = 6,
                Width = 200,
                Height = 36,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnApplyItem.Click += (s, e) => ApplyItemDetails();
            pnlActions.Controls.Add(btnApplyItem);

            var btnSaveItemsJson = new Button
            {
                Text = "Save items.json",
                Left = 215,
                Top = 6,
                Width = 175,
                Height = 36,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnSaveItemsJson.Click += (s, e) => SaveItemsJson();
            pnlActions.Controls.Add(btnSaveItemsJson);
        }

        private void BuildTabZoneLighting()
        {
            var tab = new TabPage("Zone & Lighting Profile");
            _tabs.TabPages.Add(tab);

            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(25) };
            tab.Controls.Add(pnl);

            int y = 20;
            void AddHead(string text)
            {
                var l = new Label { Text = text, Left = 20, Top = y, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold), ForeColor = Color.DarkSlateBlue };
                pnl.Controls.Add(l);
                y += 30;
            }

            AddHead("Lighting & Darkvision");

            var lblDark = new Label { Text = "Darkness Max Brightness (0% = Pitch Black to 30%):", Left = 20, Top = y, AutoSize = true };
            pnl.Controls.Add(lblDark);
            _tbDarkMax = new TrackBar { Left = 20, Top = y + 20, Width = 300, Minimum = 0, Maximum = 30, Value = 4 };
            _lblDarkMaxVal = new Label { Text = "4%", Left = 330, Top = y + 22, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold) };
            _tbDarkMax.ValueChanged += (s, e) => _lblDarkMaxVal.Text = $"{_tbDarkMax.Value}%";
            pnl.Controls.Add(_tbDarkMax);
            pnl.Controls.Add(_lblDarkMaxVal);
            y += 70;

            _chkInvertDark = new CheckBox { Text = "Invert Colors in Darkness (Negative Darkvision)", Left = 20, Top = y, AutoSize = true, Checked = true };
            pnl.Controls.Add(_chkInvertDark);
            y += 35;

            var lblVDepth = new Label { Text = "Max View Depth (distance where light fades to 0%):", Left = 20, Top = y + 2, AutoSize = true };
            pnl.Controls.Add(lblVDepth);
            _nudMaxViewDepth = new NumericUpDown { Left = 330, Top = y, Width = 80, DecimalPlaces = 1, Minimum = 5, Maximum = 40, Value = 14.5m };
            pnl.Controls.Add(_nudMaxViewDepth);
            y += 35;

            var lblMinDepth = new Label { Text = "Min View Depth (full brightness near limit):", Left = 20, Top = y + 2, AutoSize = true };
            pnl.Controls.Add(lblMinDepth);
            _nudMinViewDepth = new NumericUpDown { Left = 330, Top = y, Width = 80, DecimalPlaces = 1, Minimum = 1, Maximum = 10, Value = 2.5m };
            pnl.Controls.Add(_nudMinViewDepth);
            y += 45;

            AddHead("Point Lights (Lanterns / Sconces)");

            var lblInR = new Label { Text = "Lantern Inner Radius (100% illumination distance):", Left = 20, Top = y + 2, AutoSize = true };
            pnl.Controls.Add(lblInR);
            _nudInnerRadius = new NumericUpDown { Left = 330, Top = y, Width = 80, DecimalPlaces = 1, Minimum = 0.5m, Maximum = 10, Value = 1.5m };
            pnl.Controls.Add(_nudInnerRadius);
            y += 35;

            var lblOutR = new Label { Text = "Lantern Outer Radius (distance where light dims to 0):", Left = 20, Top = y + 2, AutoSize = true };
            pnl.Controls.Add(lblOutR);
            _nudOuterRadius = new NumericUpDown { Left = 330, Top = y, Width = 80, DecimalPlaces = 1, Minimum = 1m, Maximum = 20, Value = 5.5m };
            pnl.Controls.Add(_nudOuterRadius);
            y += 35;

            var lblLCol = new Label { Text = "Lantern Light Glow Color:", Left = 20, Top = y + 4, AutoSize = true };
            pnl.Controls.Add(lblLCol);
            _pnlLanternColor = new Panel { Left = 220, Top = y, Width = 40, Height = 25, BorderStyle = BorderStyle.FixedSingle, BackColor = _lanternColor };
            pnl.Controls.Add(_pnlLanternColor);
            var btnLanternPick = new Button { Text = "Change...", Left = 270, Top = y - 1, Width = 90, Height = 27 };
            btnLanternPick.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = _lanternColor })
                    if (cd.ShowDialog() == DialogResult.OK) { _lanternColor = cd.Color; _pnlLanternColor.BackColor = cd.Color; }
            };
            pnl.Controls.Add(btnLanternPick);
            y += 45;

            AddHead("Environment (Floors & Ceilings)");

            var lblFCol = new Label { Text = "Floor Color:", Left = 20, Top = y + 4, AutoSize = true };
            pnl.Controls.Add(lblFCol);
            _pnlFloorColor = new Panel { Left = 120, Top = y, Width = 40, Height = 25, BorderStyle = BorderStyle.FixedSingle, BackColor = _floorColor };
            pnl.Controls.Add(_pnlFloorColor);
            var btnFloorPick = new Button { Text = "Change...", Left = 170, Top = y - 1, Width = 90, Height = 27 };
            btnFloorPick.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = _floorColor })
                    if (cd.ShowDialog() == DialogResult.OK) { _floorColor = cd.Color; _pnlFloorColor.BackColor = cd.Color; }
            };
            pnl.Controls.Add(btnFloorPick);

            var lblCCol = new Label { Text = "Ceiling Color:", Left = 300, Top = y + 4, AutoSize = true };
            pnl.Controls.Add(lblCCol);
            _pnlCeilingColor = new Panel { Left = 400, Top = y, Width = 40, Height = 25, BorderStyle = BorderStyle.FixedSingle, BackColor = _ceilingColor };
            pnl.Controls.Add(_pnlCeilingColor);
            var btnCeilPick = new Button { Text = "Change...", Left = 450, Top = y - 1, Width = 90, Height = 27 };
            btnCeilPick.Click += (s, e) =>
            {
                using (var cd = new ColorDialog { Color = _ceilingColor })
                    if (cd.ShowDialog() == DialogResult.OK) { _ceilingColor = cd.Color; _pnlCeilingColor.BackColor = cd.Color; }
            };
            pnl.Controls.Add(btnCeilPick);
            y += 35;

            _chkShowFloorGrid = new CheckBox { Text = "Show Floor Tile Grid Lines", Left = 20, Top = y, AutoSize = true, Checked = true };
            pnl.Controls.Add(_chkShowFloorGrid);
            y += 45;

            var btnSaveZone = new Button { Text = "Save Zone Lighting Profile", Left = 20, Top = y, Width = 220, Height = 35, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
            btnSaveZone.Click += (s, e) => SaveZoneConfig();
            pnl.Controls.Add(btnSaveZone);
        }

        private void LoadDefaultProject()
        {
            string defaultPath = @"D:\Documents\projects\GridLocked\Games\Dungeon01";
            if (Directory.Exists(defaultPath))
            {
                OpenProject(defaultPath);
            }
        }

        private void BrowseOpenProject()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = @"D:\Documents\projects\GridLocked\Games";
                if (fbd.ShowDialog() == DialogResult.OK)
                    OpenProject(fbd.SelectedPath);
            }
        }

        private void PromptNewProject()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Create New GridLocked Project";
                dlg.Width = 540;
                dlg.Height = 315;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lblName = new Label { Text = "Project / Folder Name:", Left = 20, Top = 16, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold) };
                var txtName = new TextBox { Text = "NewDungeon", Left = 20, Top = 38, Width = 350, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular) };

                var lblLocation = new Label { Text = "Storage Location (D: Drive):", Left = 20, Top = 72, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold) };
                var txtLocation = new TextBox { Text = @"D:\Documents\projects\GridLocked\Games", Left = 20, Top = 94, Width = 370, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular) };
                var btnBrowse = new Button { Text = "Browse...", Left = 400, Top = 93, Width = 100, Height = 25 };

                var lblTitle = new Label { Text = "Game Title (Window / UI):", Left = 20, Top = 128, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold) };
                var txtTitle = new TextBox { Text = "New Dungeon Adventure", Left = 20, Top = 150, Width = 480, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular) };

                var lblFullPreview = new Label
                {
                    Text = $@"Full Path: {Path.Combine(txtLocation.Text, txtName.Text)}",
                    Left = 20, Top = 184, Width = 480, AutoSize = false,
                    ForeColor = Color.DimGray, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Italic)
                };

                Action updatePreview = () =>
                {
                    try
                    {
                        lblFullPreview.Text = $@"Full Path: {Path.Combine(txtLocation.Text, txtName.Text.Trim())}";
                    }
                    catch { }
                };
                txtName.TextChanged += (s, e) => updatePreview();
                txtLocation.TextChanged += (s, e) => updatePreview();

                btnBrowse.Click += (s, e) =>
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.SelectedPath = txtLocation.Text;
                        if (fbd.ShowDialog(dlg) == DialogResult.OK)
                        {
                            txtLocation.Text = fbd.SelectedPath;
                        }
                    }
                };

                var btnCreate = new Button
                {
                    Text = "✨ Create Project",
                    Left = 250, Top = 222, Width = 135, Height = 32,
                    BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White,
                    Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold),
                    DialogResult = DialogResult.OK
                };
                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Left = 395, Top = 222, Width = 90, Height = 32,
                    DialogResult = DialogResult.Cancel
                };

                dlg.Controls.AddRange(new Control[] { lblName, txtName, lblLocation, txtLocation, btnBrowse, lblTitle, txtTitle, lblFullPreview, btnCreate, btnCancel });
                dlg.AcceptButton = btnCreate;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string pName = txtName.Text.Trim();
                    string loc = txtLocation.Text.Trim();
                    string title = txtTitle.Text.Trim();

                    if (string.IsNullOrEmpty(pName))
                    {
                        MessageBox.Show("Project name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (!loc.StartsWith("D:", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Projects must be created on the D: drive to satisfy storage restrictions.", "Drive Restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string fullPath = Path.Combine(loc, pName);
                    CreateAndOpenNewProject(fullPath, title);
                }
            }
        }

        private void CreateAndOpenNewProject(string folder, string title, bool showSuccessPrompt = true)
        {
            try
            {
                if (Directory.Exists(folder) && File.Exists(Path.Combine(folder, "project.json")))
                {
                    var res = MessageBox.Show($"A project already exists at {folder}. Open it?", "Existing Project", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        OpenProject(folder);
                    }
                    return;
                }

                Directory.CreateDirectory(folder);

                // 1. project.json
                string projJson = "{\n" +
                                  $"  \"title\": \"{title.Replace("\"", "\\\"")}\",\n" +
                                  "  \"startDungeon\": \"dungeon_01\",\n" +
                                  "  \"startLevel\": 1\n" +
                                  "}";
                File.WriteAllText(Path.Combine(folder, "project.json"), projJson);

                // 2. key legend.json
                string defaultLegend = "{\n" +
                                       "  \"mappings\": {\n" +
                                       "    \" \": \"floor\",\n" +
                                       "    \".\": \"floor\",\n" +
                                       "    \"#\": \"wall\",\n" +
                                       "    \"@\": \"spawn\",\n" +
                                       "    \"S\": \"stairs_up\",\n" +
                                       "    \"s\": \"stairs_down\",\n" +
                                       "    \"D\": \"door\",\n" +
                                       "    \"~\": \"void\",\n" +
                                       "    \"<\": \"stairs_up\",\n" +
                                       "    \">\": \"stairs_down\"\n" +
                                       "  }\n" +
                                       "}";
                File.WriteAllText(Path.Combine(folder, "key legend.json"), defaultLegend);

                // 3. Seed item_classes.json from template or Dungeon01
                string seedClassesPath = @"D:\Documents\projects\GridLocked\Games\Dungeon01\item_classes.json";
                if (File.Exists(seedClassesPath))
                {
                    File.Copy(seedClassesPath, Path.Combine(folder, "item_classes.json"), true);
                }
                else
                {
                    File.WriteAllText(Path.Combine(folder, "item_classes.json"), "[]");
                }

                // 4. Seed items.json
                string seedItemsPath = @"D:\Documents\projects\GridLocked\Games\Dungeon01\items.json";
                if (File.Exists(seedItemsPath))
                {
                    File.Copy(seedItemsPath, Path.Combine(folder, "items.json"), true);
                }
                else
                {
                    File.WriteAllText(Path.Combine(folder, "items.json"), "[]");
                }

                // 5. Seed objects.json
                string seedObjectsPath = @"D:\Documents\projects\GridLocked\Games\Dungeon01\objects.json";
                if (File.Exists(seedObjectsPath))
                {
                    File.Copy(seedObjectsPath, Path.Combine(folder, "objects.json"), true);
                }

                // 6. Dungeon & Level starter structure
                string levelDir = Path.Combine(folder, "dungeons", "dungeon_01", "level_1");
                Directory.CreateDirectory(levelDir);

                string starterMap = "############\n" +
                                    "#..........#\n" +
                                    "#..@.......#\n" +
                                    "#..........#\n" +
                                    "#..........#\n" +
                                    "#..........#\n" +
                                    "#..........#\n" +
                                    "#..........#\n" +
                                    "#..........#\n" +
                                    "#..........#\n" +
                                    "#....D.....#\n" +
                                    "############\n";
                File.WriteAllText(Path.Combine(levelDir, "map.txt"), starterMap);

                string starterItems = "# Items Map\n# [x, y, subSquare, count] itemID\n";
                File.WriteAllText(Path.Combine(levelDir, "items.txt"), starterItems);

                string starterMonsters = "# Monsters Map\n# [x, y, facing] monsterId\n";
                File.WriteAllText(Path.Combine(levelDir, "monsters.txt"), starterMonsters);

                // 7. Open the newly created project!
                OpenProject(folder);
                SelectTab(0);
                if (showSuccessPrompt)
                {
                    MessageBox.Show($"New project '{title}' created successfully!\n\nLocation: {folder}", "Project Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create new project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CloseProject()
        {
            if (_project == null) return;

            var res = MessageBox.Show("Save changes before closing project?", "Close Project", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res == DialogResult.Cancel) return;
            if (res == DialogResult.Yes)
            {
                SaveCurrentLevelAndProject();
            }

            _project = null;
            _projectDir = null;
            _currentLevel = null;
            Text = "GridLocked Studio - No Project Loaded";

            _cbDungeons.Items.Clear();
            _cbLevels.Items.Clear();

            _painter.SetLevel(null);
            _painter.Project = null;
            _painter.Invalidate();

            _itemDefsList.Clear();
            _filteredDefsList.Clear();
            if (_lbItems != null) _lbItems.Items.Clear();
            if (_dgvLegend != null) _dgvLegend.Rows.Clear();
            if (_cbToolProp != null) _cbToolProp.Items.Clear();
            if (_cbToolArchetype != null) _cbToolArchetype.Items.Clear();
            if (_txtSettingsTitle != null) _txtSettingsTitle.Text = "";
            if (_lblSettingsFolder != null) _lblSettingsFolder.Text = "(No project loaded)";
            if (_cbSettingsStartDungeon != null) _cbSettingsStartDungeon.Items.Clear();

            _projectExplorer?.Populate(null, null);
            UpdateMenuState();
        }

        private void ReloadProject()
        {
            if (string.IsNullOrEmpty(_projectDir) || !Directory.Exists(_projectDir)) return;
            OpenProject(_projectDir);
        }

        private void ToggleProjectExplorer()
        {
            if (_mainSplit == null) return;
            _mainSplit.Panel1Collapsed = !_mainSplit.Panel1Collapsed;
            if (_menuItemToggleExplorer != null)
                _menuItemToggleExplorer.Checked = !_mainSplit.Panel1Collapsed;
        }

        private void HandleProjectNodeActivated(ProjectNodeInfo info)
        {
            if (info == null) return;

            switch (info.Kind)
            {
                case ProjectNodeKind.ProjectRoot:
                case ProjectNodeKind.ProjectSettings:
                case ProjectNodeKind.KeyLegend:
                    SelectTab(3); // Project Settings & Legend tab
                    break;

                case ProjectNodeKind.Dungeon:
                    SelectTab(0); // Map Painter
                    if (!string.IsNullOrEmpty(info.DungeonId))
                    {
                        SelectDungeonAndLevel(info.DungeonId, -1);
                        ShowDungeonInspector(info.DungeonId, -1);
                    }
                    break;

                case ProjectNodeKind.LootTables:
                    SelectTab(0); // Map Painter
                    SelectDungeonAndLevel(info.DungeonId, info.LevelNumber);
                    ShowDungeonInspector(info.DungeonId, info.LevelNumber);
                    break;

                case ProjectNodeKind.Level:
                case ProjectNodeKind.Map:
                case ProjectNodeKind.Transitions:
                case ProjectNodeKind.LevelItems:
                case ProjectNodeKind.LevelMonsters:
                    SelectTab(0); // Map Painter
                    SelectDungeonAndLevel(info.DungeonId, info.LevelNumber);
                    break;

                case ProjectNodeKind.ZoneLighting:
                    SelectTab(2); // Zone Lighting
                    SelectDungeonAndLevel(info.DungeonId, info.LevelNumber);
                    break;

                case ProjectNodeKind.ItemsSchema:
                case ProjectNodeKind.ItemCategory:
                case ProjectNodeKind.ItemClasses:
                case ProjectNodeKind.ObjectsSchema:
                    SelectTab(1); // Item & Object Creator
                    if (!string.IsNullOrEmpty(info.Category) && _cbCategoryFilter != null)
                    {
                        for (int i = 0; i < _cbCategoryFilter.Items.Count; i++)
                        {
                            if (string.Equals(_cbCategoryFilter.Items[i].ToString(), info.Category, StringComparison.OrdinalIgnoreCase))
                            {
                                _cbCategoryFilter.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    break;

                case ProjectNodeKind.ItemDefinition:
                    SelectTab(1); // Item & Object Creator
                    if (!string.IsNullOrEmpty(info.ItemId))
                    {
                        SelectItemById(info.ItemId);
                    }
                    break;

                default:
                    break;
            }
        }

        public void SelectDungeonAndLevel(string dId, int levelNum)
        {
            if (string.IsNullOrEmpty(dId) && _cbDungeons != null && _cbDungeons.Items.Count > 0)
                dId = _cbDungeons.Items[0].ToString();

            if (!string.IsNullOrEmpty(dId) && _cbDungeons != null && _cbDungeons.Items.Contains(dId))
            {
                if (!string.Equals(_cbDungeons.SelectedItem?.ToString(), dId, StringComparison.OrdinalIgnoreCase))
                {
                    _cbDungeons.SelectedItem = dId;
                }
            }

            if (levelNum > 0 && _project != null && _cbDungeons?.SelectedItem != null &&
                _project.Dungeons.TryGetValue(_cbDungeons.SelectedItem.ToString(), out var pkg))
            {
                for (int i = 0; i < pkg.Levels.Count; i++)
                {
                    if (pkg.Levels[i].LevelNumber == levelNum)
                    {
                        if (_cbLevels != null && _cbLevels.SelectedIndex != i)
                            _cbLevels.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void SelectItemById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _lbItems == null) return;
            if (_cbCategoryFilter != null && _cbCategoryFilter.SelectedIndex != 0)
            {
                _cbCategoryFilter.SelectedIndex = 0; // All categories
            }
            for (int i = 0; i < _filteredDefsList.Count; i++)
            {
                if (string.Equals(_filteredDefsList[i].Id, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    _lbItems.SelectedIndex = i;
                    break;
                }
            }
        }

        private void PromptAddNewLevel()
        {
            if (_project == null || string.IsNullOrEmpty(_projectDir))
            {
                MessageBox.Show("Please open or create a project first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string dId = _cbDungeons.SelectedItem?.ToString() ?? "dungeon_01";
            if (!_project.Dungeons.TryGetValue(dId, out var pkg))
            {
                MessageBox.Show("No active dungeon found.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int nextLvl = 1;
            foreach (var l in pkg.Levels)
            {
                if (l.LevelNumber >= nextLvl) nextLvl = l.LevelNumber + 1;
            }

            string lvlDir = Path.Combine(_projectDir, "dungeons", dId, $"level_{nextLvl}");
            if (!Directory.Exists(lvlDir)) Directory.CreateDirectory(lvlDir);

            string defaultMap =
                "##########\n" +
                "#........#\n" +
                "#........#\n" +
                "#...@....#\n" +
                "#........#\n" +
                "#........#\n" +
                "#........#\n" +
                "#........#\n" +
                "#........#\n" +
                "##########\n";

            File.WriteAllText(Path.Combine(lvlDir, "map.txt"), defaultMap);
            File.WriteAllText(Path.Combine(lvlDir, "items.txt"), "# Items Map\n");
            File.WriteAllText(Path.Combine(lvlDir, "monsters.txt"), "# Monsters Map\n");

            ReloadProject();
            SelectTab(0);
            SelectDungeonAndLevel(dId, nextLvl);
        }

        private void PromptAddNewDungeon()
        {
            if (_project == null || string.IsNullOrEmpty(_projectDir))
            {
                MessageBox.Show("Please open or create a project first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newDungeonId = $"dungeon_{_project.Dungeons.Count + 1:D2}";
            string dDir = Path.Combine(_projectDir, "dungeons", newDungeonId);
            string lvlDir = Path.Combine(dDir, "level_1");
            Directory.CreateDirectory(lvlDir);

            string dJson = "{\n  \"name\": \"" + newDungeonId + "\"\n}\n";
            File.WriteAllText(Path.Combine(dDir, "dungeon.json"), dJson);

            string defaultMap =
                "##########\n" +
                "#........#\n" +
                "#...@....#\n" +
                "#........#\n" +
                "##########\n";

            File.WriteAllText(Path.Combine(lvlDir, "map.txt"), defaultMap);
            File.WriteAllText(Path.Combine(lvlDir, "items.txt"), "# Items Map\n");
            File.WriteAllText(Path.Combine(lvlDir, "monsters.txt"), "# Monsters Map\n");

            ReloadProject();
            SelectTab(0);
            SelectDungeonAndLevel(newDungeonId, 1);
        }

        private void OpenProjectInExplorer()
        {
            if (string.IsNullOrEmpty(_projectDir) || !Directory.Exists(_projectDir))
            {
                MessageBox.Show("No project is currently loaded.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Process.Start("explorer.exe", _projectDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open explorer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectTab(int index)
        {
            if (_tabs != null && index >= 0 && index < _tabs.TabPages.Count)
            {
                _tabs.SelectedIndex = index;
            }
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show(
                "GridLocked Dungeon Crawler Studio\n\n" +
                "• Retro First-Person Vector/Wireframe RPG Engine\n" +
                "• Pure JSON No-Compile Architecture\n" +
                "• ASCII Map Painter & Placer with Full Undo/Redo\n" +
                "• Visual Object & Item Schema Designer\n" +
                "• Dynamic Zone Lighting & Shader Profile Editor\n\n" +
                "GridLocked Studio v2.0",
                "About GridLocked Studio",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateUndoButtons()
        {
            bool canUndo = _painter != null && _painter.CanUndo;
            bool canRedo = _painter != null && _painter.CanRedo;

            if (_btnToolUndo != null) _btnToolUndo.Enabled = canUndo;
            if (_btnToolRedo != null) _btnToolRedo.Enabled = canRedo;
            if (_menuItemUndo != null) _menuItemUndo.Enabled = canUndo;
            if (_menuItemRedo != null) _menuItemRedo.Enabled = canRedo;
        }

        private void UpdateMenuState()
        {
            bool hasProject = _project != null;
            if (_menuItemSave != null) _menuItemSave.Enabled = hasProject;
            if (_menuItemClose != null) _menuItemClose.Enabled = hasProject;
            if (_menuItemPlaytest != null) _menuItemPlaytest.Enabled = hasProject;
            if (_menuItemReload != null) _menuItemReload.Enabled = hasProject;
            UpdateUndoButtons();
        }

        private void PopulateToolProps()
        {
            if (_cbToolProp == null || _project == null) return;
            _cbToolProp.Items.Clear();

            var objIds = new List<string> { "tree_oak", "stone_pillar", "altar_gold" };
            foreach (var kvp in _project.Legend.Mappings)
            {
                if (!kvp.Value.Equals("floor", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Value.Equals("wall", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Value.Equals("spawn", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Value.Equals("door", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Value.Equals("void", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Value.Equals("stairs_up", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Value.Equals("stairs_down", StringComparison.OrdinalIgnoreCase))
                {
                    if (!objIds.Contains(kvp.Value)) objIds.Add(kvp.Value);
                }
            }
            foreach (var id in objIds) _cbToolProp.Items.Add(id);
            if (_cbToolProp.Items.Count > 0) _cbToolProp.SelectedIndex = 0;
        }

        private void PopulateToolArchetypes()
        {
            if (_cbToolArchetype == null || _project == null) return;
            _cbToolArchetype.Items.Clear();

            foreach (var kvp in _project.Legend.Archetypes.OrderBy(k => k.Key))
            {
                string displayKey = kvp.Key == ' ' ? "[space]" : $"'{kvp.Key}'";
                _cbToolArchetype.Items.Add($"{displayKey} - {kvp.Value.Name} ({kvp.Value.Id})");
            }

            if (_cbToolArchetype.Items.Count > 0)
            {
                _cbToolArchetype.SelectedIndex = 0;
                UpdateActiveArchetypeBrush();
            }
        }

        private void UpdateActiveArchetypeBrush()
        {
            if (_cbToolArchetype == null || _project == null || _cbToolArchetype.SelectedIndex < 0) return;
            string sel = _cbToolArchetype.SelectedItem?.ToString() ?? "";
            char key = ' ';
            if (sel.StartsWith("[space]"))
            {
                key = ' ';
            }
            else if (sel.StartsWith("'") && sel.Length >= 3 && sel[2] == '\'')
            {
                key = sel[1];
            }
            var arch = _project.Legend.GetArchetype(key);
            _painter.ActiveArchetype = arch;
        }

        private void OpenProject(string path)
        {
            try
            {
                _projectDir = path;
                _project = GameProject.LoadFromDirectory(path);
                Text = $"GridLocked Studio - {_project.Title}";
                _painter.Project = _project;

                _cbDungeons.Items.Clear();
                foreach (var d in _project.Dungeons.Keys)
                    _cbDungeons.Items.Add(d);

                if (_cbDungeons.Items.Count > 0)
                    _cbDungeons.SelectedIndex = 0;

                PopulateToolProps();
                PopulateToolArchetypes();
                PopulateItemsTab();
                PopulateProjectSettings();
                _projectExplorer?.Populate(_project, _projectDir);
                UpdateMenuState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load project: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDungeonSelected()
        {
            if (_project == null || _cbDungeons.SelectedItem == null) return;
            string dId = _cbDungeons.SelectedItem.ToString();

            _cbLevels.Items.Clear();
            if (_project.Dungeons.TryGetValue(dId, out var pkg))
            {
                foreach (var lvl in pkg.Levels)
                    _cbLevels.Items.Add(lvl.LevelName);

                if (_cbLevels.Items.Count > 0)
                    _cbLevels.SelectedIndex = 0;
            }
        }

        private void OnLevelSelected()
        {
            if (_project == null || _cbDungeons.SelectedItem == null || _cbLevels.SelectedItem == null) return;
            string dId = _cbDungeons.SelectedItem.ToString();
            int lvlIdx = _cbLevels.SelectedIndex;

            if (_project.Dungeons.TryGetValue(dId, out var pkg) && lvlIdx >= 0 && lvlIdx < pkg.Levels.Count)
            {
                _currentLevel = pkg.Levels[lvlIdx];
                _painter.SetLevel(_currentLevel);
                _painter.Size = new Size(_currentLevel.Width * _painter.CellSize, _currentLevel.Height * _painter.CellSize);

                LoadZoneConfigToUi(_currentLevel.Config);
                UpdateUndoButtons();
            }
        }

        private void ReflowInspectorCards()
        {
            if (_inspectorPanel == null) return;
            int y = 10;
            int pad = 10;
            Control[] cards = { _grpTileBase, _grpEntityCard, _grpMovementCard, _grpTransition, _grpSubSquareItems };
            foreach (var card in cards)
            {
                if (card != null && card.Visible)
                {
                    card.Top = y;
                    card.Left = 10;
                    y += card.Height + pad;
                }
            }
        }

        private static string GetFacingStringFromAngle(double angle)
        {
            double deg = angle * 180.0 / Math.PI;
            while (deg < 0) deg += 360.0;
            while (deg >= 360.0) deg -= 360.0;
            if (deg >= 45 && deg < 135) return "South";
            if (deg >= 135 && deg < 225) return "West";
            if (deg >= 225 && deg < 315) return "North";
            return "East";
        }

        private void PopulateTileInspector(int x, int y, DungeonTile tile)
        {
            if (tile == null) return;
            _grpDungeonInspector.Visible = false;
            _grpTileBase.Visible = true;
            _isPopulatingInspector = true;
            try
            {
                _lblSelectedCoords.Text = $"Tile: ({x}, {y})";
                _cbTileType.SelectedItem = tile.Type.ToString();

                if (tile.ArchetypeKey.HasValue)
                {
                    string keyDisplay = tile.ArchetypeKey.Value == ' ' ? "[space]" : $"'{tile.ArchetypeKey.Value}'";
                    _lblTileArchetype.Text = $"Archetype: [{keyDisplay}] {tile.ArchetypeName ?? tile.ArchetypeId ?? ""}";
                }
                else
                {
                    _lblTileArchetype.Text = $"Archetype: (None / Base {tile.Type})";
                }

                _chkTileCanWalkThru.Checked = tile.CanWalkThru && tile.CanWalkOver;
                _chkTileBlocksVision.Checked = tile.BlocksVision;
                _chkTileBreakable.Checked = tile.IsBreakable;
                _nudTileBreakHP.Enabled = tile.IsBreakable;
                _nudTileBreakHP.Value = Math.Max(1, Math.Min(1000, tile.BreakHP > 0 ? tile.BreakHP : (tile.MaxBreakHP > 0 ? tile.MaxBreakHP : 50)));
                _chkTileMoveable.Checked = tile.IsMoveable;
                _chkTileCanBeOpened.Checked = tile.CanBeOpened;
                _chkTileIsOpen.Enabled = tile.CanBeOpened;
                _chkTileIsOpen.Checked = tile.IsOpen;
                _chkTileHazardous.Checked = tile.IsHazardous;
                _nudTileHazardDamage.Enabled = tile.IsHazardous;
                _nudTileHazardDamage.Value = Math.Max(0, Math.Min(999, tile.HazardDamage));

                // 1. Entity / Creature Card
                bool hasEntity = tile.CenterOccupant is MonsterInstance;
                _grpEntityCard.Visible = hasEntity;
                _grpMovementCard.Visible = hasEntity;
                _btnSpawnEntityHere.Visible = !hasEntity && (tile.Type == TileType.Floor || tile.Type == TileType.Spawn);

                if (hasEntity && tile.CenterOccupant is MonsterInstance m)
                {
                    _cbEntityDef.Items.Clear();
                    if (_project != null)
                    {
                        foreach (var ed in _project.Entities.GetAll())
                        {
                            _cbEntityDef.Items.Add(ed.Id);
                        }
                    }
                    if (!_cbEntityDef.Items.Contains(m.Id)) _cbEntityDef.Items.Add(m.Id);
                    _cbEntityDef.SelectedItem = m.Id;

                    _cbEntityFacing.SelectedItem = GetFacingStringFromAngle(m.FacingAngle);
                    if (_cbEntityFacing.SelectedIndex < 0) _cbEntityFacing.SelectedIndex = 2; // South

                    _nudEntityLevel.Value = Math.Max(1, Math.Min(20, m.Level));
                    _chkEntityUnique.Checked = m.IsUnique;
                    _txtEntityTitle.Enabled = m.IsUnique;
                    _txtEntityTitle.Text = m.UniqueTitle ?? "";

                    _chkEntityArchetypeLoot.Checked = m.DropArchetypeLoot;
                    _chkEntityZoneLoot.Checked = m.DropZoneLoot;

                    _cbEntityCustomLoot.Items.Clear();
                    _cbEntityCustomLoot.Items.Add("(None)");
                    _cbEntityCustomLoot.Items.Add("dungeon_boss_rare");
                    _cbEntityCustomLoot.Items.Add("crypt_undead_loot");
                    _cbEntityCustomLoot.Items.Add("treasure_hoard");
                    if (!string.IsNullOrEmpty(m.CustomLootTable) && !_cbEntityCustomLoot.Items.Contains(m.CustomLootTable))
                        _cbEntityCustomLoot.Items.Add(m.CustomLootTable);
                    _cbEntityCustomLoot.SelectedItem = string.IsNullOrEmpty(m.CustomLootTable) ? "(None)" : m.CustomLootTable;

                    // Guaranteed items
                    _cbGuaranteedItem.Items.Clear();
                    foreach (var it in _itemDefsList)
                        _cbGuaranteedItem.Items.Add(it.Id);
                    if (_cbGuaranteedItem.Items.Count > 0) _cbGuaranteedItem.SelectedIndex = 0;

                    _lbGuaranteedDrops.Items.Clear();
                    foreach (var g in m.GuaranteedDrops)
                        _lbGuaranteedDrops.Items.Add(g);

                    // Live stats preview
                    var stats = _project?.Entities?.Get(m.Id)?.GetStatsForLevel(m.Level);
                    if (stats != null)
                    {
                        _lblEntityStatsPreview.Text = $"HP:{stats.HP}  MP:{stats.MP}  Dmg:{stats.MinDamage}-{stats.MaxDamage}  Arm:{stats.Armor}  Spd:{stats.Speed}";
                    }
                    else
                    {
                        _lblEntityStatsPreview.Text = $"HP:{m.HP}  MP:{m.MP}  Dmg:{m.MinDamage}-{m.MaxDamage}  Arm:{m.Armor}  Spd:{m.Speed}";
                    }

                    // 1.5. Movement & Patrol Route Card
                    if (m.MovementMode == EntityMovementMode.Static)
                        _cbMovementMode.SelectedIndex = 0;
                    else if (m.MovementMode == EntityMovementMode.PatrolRoute)
                        _cbMovementMode.SelectedIndex = 1;
                    else
                        _cbMovementMode.SelectedIndex = 2;

                    _cbCircuitStyle.SelectedItem = m.CircuitStyle.ToString();
                    if (_cbCircuitStyle.SelectedIndex < 0) _cbCircuitStyle.SelectedIndex = 0;

                    _nudWaypointPause.Value = (decimal)Math.Max(0.0, Math.Min(60.0, m.WaypointPauseSeconds));
                    _nudWanderInterval.Value = (decimal)Math.Max(0.5, Math.Min(60.0, m.WanderIntervalSeconds));
                    _nudMaxChase.Value = Math.Max(1, Math.Min(50, m.MaxChaseDistance));
                    _chkReturnPreCombat.Checked = m.ReturnToPreCombatSquare;
                    _lblNavStatus.Text = $"Status: {m.LastNavStatus}";

                    UpdateWaypointsList(m);
                    UpdateWanderLabels(m);
                    UpdateMovementCardVisibility();
                }

                // 2. Transition & Security Inspector
                var trans = tile.Transition;
                bool isTransTile = trans != null || tile.Type == TileType.Door || tile.Type == TileType.StairsUp || tile.Type == TileType.StairsDown || tile.Type == TileType.VoidOrPit;
                _grpTransition.Visible = isTransTile;

                if (isTransTile)
                {
                    if (trans == null && _currentLevel != null)
                    {
                        if (tile.Type == TileType.Door)
                            trans = new DoorTransition($"door_{x}_{y}", x, y, _project?.Objects?.Get("door_closed"), _project?.Objects?.Get("door_open"));
                        else if (tile.Type == TileType.StairsUp)
                            trans = new StairsTransition($"stairs_up_{x}_{y}", TransitionKind.StairsUp, x, y, Math.Max(1, _currentLevel.LevelNumber - 1), _project?.Objects?.Get("stairs_up"));
                        else if (tile.Type == TileType.StairsDown)
                            trans = new StairsTransition($"stairs_down_{x}_{y}", TransitionKind.StairsDown, x, y, _currentLevel.LevelNumber + 1, _project?.Objects?.Get("stairs_down"));
                        else
                            trans = new PitTransition($"pit_{x}_{y}", x, y, _project?.Objects?.Get("pit"));

                        tile.Transition = trans;
                        _currentLevel.Transitions.Add(trans);
                        _currentLevel.LargeObjects.Add(trans);
                    }

                    if (trans != null)
                    {
                        _cbTransTargetDungeon.Items.Clear();
                        _cbTransTargetDungeon.Items.Add("(Current Dungeon)");
                        if (_project != null)
                        {
                            foreach (var d in _project.Dungeons.Keys)
                                _cbTransTargetDungeon.Items.Add(d);
                        }
                        if (string.IsNullOrEmpty(trans.TargetDungeon))
                            _cbTransTargetDungeon.SelectedIndex = 0;
                        else
                            _cbTransTargetDungeon.SelectedItem = trans.TargetDungeon;
                        if (_cbTransTargetDungeon.SelectedIndex < 0) _cbTransTargetDungeon.SelectedIndex = 0;

                        _nudTransTargetLevel.Value = trans.TargetLevel;
                        _nudTransTargetX.Value = trans.TargetX;
                        _nudTransTargetY.Value = trans.TargetY;
                        _cbTransTargetFacing.SelectedItem = string.IsNullOrEmpty(trans.TargetFacing) ? "Keep" : trans.TargetFacing;
                        if (_cbTransTargetFacing.SelectedIndex < 0) _cbTransTargetFacing.SelectedIndex = 0;

                        _chkTransLocked.Checked = trans.IsLocked;
                        _cbTransKey.Enabled = trans.IsLocked;

                        _cbTransKey.Items.Clear();
                        _cbTransKey.Items.Add("key_iron");
                        foreach (var it in _itemDefsList)
                        {
                            if (string.Equals(it.SubCategory, "Key", StringComparison.OrdinalIgnoreCase) || it.Id.ToLowerInvariant().Contains("key"))
                                if (!_cbTransKey.Items.Contains(it.Id)) _cbTransKey.Items.Add(it.Id);
                        }
                        _cbTransKey.Text = trans.RequiredKey ?? "";

                        _chkTransTrapped.Checked = trans.IsTrapped;
                        _cbTransTrapType.Enabled = trans.IsTrapped;
                        _nudTransTrapDamage.Enabled = trans.IsTrapped;
                        _chkTransRecurring.Enabled = trans.IsTrapped;
                        _nudTransJiggleChance.Enabled = trans.IsTrapped;
                        _nudTransOpenChance.Enabled = trans.IsTrapped;

                        _cbTransTrapType.SelectedItem = string.IsNullOrEmpty(trans.TrapType) ? "PoisonDart" : trans.TrapType;
                        if (_cbTransTrapType.SelectedIndex < 0) _cbTransTrapType.SelectedIndex = 0;
                        _nudTransTrapDamage.Value = Math.Max(0, trans.TrapDamage);
                        _chkTransRecurring.Checked = trans.TrapIsRecurring;
                        _nudTransJiggleChance.Value = (decimal)Math.Max(0, Math.Min(100, trans.TrapJiggleChance * 100.0));
                        _nudTransOpenChance.Value = (decimal)Math.Max(0, Math.Min(100, trans.TrapOpenChance * 100.0));

                        _chkTransOpen.Checked = trans.IsOpen;
                        _chkTransAuto.Checked = trans.AutoTransitionOnStep;
                    }
                }

                // 3. Ground 4 Sub-Square Item Slots Card
                bool isWalkable = tile.Type != TileType.Wall;
                _grpSubSquareItems.Visible = isWalkable;
                if (isWalkable)
                {
                    for (int s = 0; s < 4; s++)
                    {
                        _cbSlotItems[s].Items.Clear();
                        _cbSlotItems[s].Items.Add("(Empty)");
                        foreach (var def in _itemDefsList)
                            _cbSlotItems[s].Items.Add(def.Id);

                        var inst = tile.Slots[s];
                        if (inst != null)
                            _cbSlotItems[s].SelectedItem = inst.Definition.Id;
                        else
                            _cbSlotItems[s].SelectedIndex = 0;
                    }
                }

                ReflowInspectorCards();
            }
            finally
            {
                _isPopulatingInspector = false;
            }
        }

        private void OnSpawnEntityClicked()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (tile.CenterOccupant != null) return;

            var firstDef = _project?.Entities?.GetAll().FirstOrDefault();
            string defId = firstDef?.Id ?? "skeleton_warrior";
            string defName = firstDef?.Name ?? "Skeleton Warrior";

            var monster = new MonsterInstance(defId, defName)
            {
                TileX = _painter.SelectedX,
                TileY = _painter.SelectedY,
                Level = 1,
                HP = firstDef?.GetStatsForLevel(1)?.HP ?? 30,
                MaxHP = firstDef?.GetStatsForLevel(1)?.HP ?? 30,
                MinDamage = firstDef?.GetStatsForLevel(1)?.MinDamage ?? 3,
                MaxDamage = firstDef?.GetStatsForLevel(1)?.MaxDamage ?? 6,
                Armor = firstDef?.GetStatsForLevel(1)?.Armor ?? 0,
                Speed = firstDef?.GetStatsForLevel(1)?.Speed ?? 10,
                XpReward = firstDef?.GetStatsForLevel(1)?.XpReward ?? 15,
                SpriteKind = firstDef?.SpriteKind ?? "skeleton"
            };

            _painter.RecordSingleTileEdit(_painter.SelectedX, _painter.SelectedY, () =>
            {
                tile.CenterOccupant = monster;
                if (!_currentLevel.Monsters.Contains(monster)) _currentLevel.Monsters.Add(monster);
                PopulateTileInspector(_painter.SelectedX, _painter.SelectedY, tile);
                _painter.Invalidate();
            });
        }

        private void OnRemoveEntityClicked()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (tile.CenterOccupant is MonsterInstance m)
            {
                _painter.RecordSingleTileEdit(_painter.SelectedX, _painter.SelectedY, () =>
                {
                    tile.CenterOccupant = null;
                    _currentLevel.Monsters.Remove(m);
                    PopulateTileInspector(_painter.SelectedX, _painter.SelectedY, tile);
                    _painter.Invalidate();
                });
            }
        }

        private void OnEntityPropertyChanged()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance m)) return;

            string selDefId = _cbEntityDef.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selDefId))
            {
                m.Id = selDefId;
                var def = _project?.Entities?.Get(selDefId);
                if (def != null)
                {
                    m.SpriteKind = def.SpriteKind;
                    m.TintColor = def.TintColor;
                }
            }

            m.Level = (int)_nudEntityLevel.Value;
            m.IsUnique = _chkEntityUnique.Checked;
            _txtEntityTitle.Enabled = m.IsUnique;
            m.UniqueTitle = _txtEntityTitle.Text.Trim();
            m.Name = (m.IsUnique && !string.IsNullOrEmpty(m.UniqueTitle)) ? m.UniqueTitle : (_project?.Entities?.Get(m.Id)?.Name ?? m.Id);

            string facing = _cbEntityFacing.SelectedItem?.ToString() ?? "South";
            if (facing == "North") m.FacingAngle = 1.5 * Math.PI;
            else if (facing == "East") m.FacingAngle = 0.0;
            else if (facing == "South") m.FacingAngle = 0.5 * Math.PI;
            else if (facing == "West") m.FacingAngle = Math.PI;

            m.DropArchetypeLoot = _chkEntityArchetypeLoot.Checked;
            m.DropZoneLoot = _chkEntityZoneLoot.Checked;
            string cLoot = _cbEntityCustomLoot.SelectedItem?.ToString();
            m.CustomLootTable = (cLoot == "(None)" || string.IsNullOrEmpty(cLoot)) ? "" : cLoot;

            // Refresh live stats preview
            var currentStats = _project?.Entities?.Get(m.Id)?.GetStatsForLevel(m.Level);
            if (currentStats != null)
            {
                m.HP = currentStats.HP;
                m.MaxHP = currentStats.HP;
                m.MP = currentStats.MP;
                m.MaxMP = currentStats.MP;
                m.MinDamage = currentStats.MinDamage;
                m.MaxDamage = currentStats.MaxDamage;
                m.Armor = currentStats.Armor;
                m.Speed = currentStats.Speed;
                m.XpReward = currentStats.XpReward;
                m.ActiveAbilities.Clear();
                m.ActiveAbilities.AddRange(currentStats.Abilities);

                _lblEntityStatsPreview.Text = $"HP:{m.HP}  MP:{m.MP}  Dmg:{m.MinDamage}-{m.MaxDamage}  Arm:{m.Armor}  Spd:{m.Speed}";
            }

            _painter.Invalidate();
        }

        private void OnAddGuaranteedDrop()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance m)) return;

            string itId = _cbGuaranteedItem.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(itId) && !_lbGuaranteedDrops.Items.Contains(itId))
            {
                m.GuaranteedDrops.Add(itId);
                _lbGuaranteedDrops.Items.Add(itId);
            }
        }

        private void OnRemoveGuaranteedDrop()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance m)) return;

            int selIdx = _lbGuaranteedDrops.SelectedIndex;
            if (selIdx >= 0)
            {
                string itId = _lbGuaranteedDrops.Items[selIdx].ToString();
                m.GuaranteedDrops.Remove(itId);
                _lbGuaranteedDrops.Items.RemoveAt(selIdx);
            }
        }

        // --- Movement & Route Helpers ---

        private void UpdateWaypointsList(MonsterInstance m)
        {
            _lbWaypoints.Items.Clear();
            _lbWaypoints.Items.Add($"Spawn: (X:{m.TileX}, Y:{m.TileY})");
            for (int i = 0; i < m.PatrolWaypoints.Count; i++)
            {
                var pt = m.PatrolWaypoints[i];
                _lbWaypoints.Items.Add($"Pt {i + 1}: (X:{pt.X}, Y:{pt.Y})");
            }
        }

        private void UpdateMovementCardVisibility()
        {
            bool isPatrol = _cbMovementMode.SelectedIndex == 1;
            bool isWander = _cbMovementMode.SelectedIndex == 2;

            _lblCircuit.Visible = isPatrol;
            _cbCircuitStyle.Visible = isPatrol;
            _lblPause.Visible = isPatrol;
            _nudWaypointPause.Visible = isPatrol;
            _btnRecordRoute.Visible = isPatrol;
            _btnClearRoute.Visible = isPatrol;
            _lbWaypoints.Visible = isPatrol;

            _btnPaintWanderArea.Visible = isWander;
            _btnClearWanderArea.Visible = isWander;
            _lblWanderTilesCount.Visible = isWander;
            _lblMaxChase.Visible = isWander;
            _nudMaxChase.Visible = isWander;
            _chkReturnPreCombat.Visible = isWander;
            _lblWanderInt.Visible = isWander;
            _nudWanderInterval.Visible = isWander;
            _lblNavStatus.Visible = isWander;
            _btnSimulateWanderStep.Visible = isWander;

            if (isPatrol)
                _grpMovementCard.Height = 225;
            else if (isWander)
                _grpMovementCard.Height = 240;
            else
                _grpMovementCard.Height = 55;

            ReflowInspectorCards();
        }

        private void OnStepWanderClicked()
        {
            if (_currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance m)) return;

            EntityMovementSystem.StepEntity(m, _currentLevel);

            _painter.SelectTile(m.TileX, m.TileY);
            PopulateTileInspector(m.TileX, m.TileY, _currentLevel.Tiles[m.TileY, m.TileX]);
            _painter.Invalidate();
        }

        private void OnMovementModeChanged()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance m)) return;

            if (_cbMovementMode.SelectedIndex == 0) m.MovementMode = EntityMovementMode.Static;
            else if (_cbMovementMode.SelectedIndex == 1) m.MovementMode = EntityMovementMode.PatrolRoute;
            else m.MovementMode = EntityMovementMode.Wander;

            if (m.MovementMode != EntityMovementMode.PatrolRoute && _isRecordingRoute)
            {
                _isRecordingRoute = false;
                _btnRecordRoute.BackColor = Color.FromArgb(70, 70, 75);
                _btnRecordRoute.Text = "✏️ Record Path (Click Tiles)";
            }

            if (m.MovementMode != EntityMovementMode.Wander && _isPaintingWanderArea)
            {
                _isPaintingWanderArea = false;
                _btnPaintWanderArea.BackColor = Color.FromArgb(70, 70, 75);
                _btnPaintWanderArea.Text = "🖌️ Paint Territory (Click Tiles)";
            }

            UpdateMovementCardVisibility();
            _painter.Invalidate();
        }

        private void OnMovementPropertyChanged()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance m)) return;

            if (Enum.TryParse<PatrolCircuitStyle>(_cbCircuitStyle.SelectedItem?.ToString(), out var cs))
                m.CircuitStyle = cs;

            m.WaypointPauseSeconds = (double)_nudWaypointPause.Value;
            m.WanderIntervalSeconds = (double)_nudWanderInterval.Value;
            m.MaxChaseDistance = (int)_nudMaxChase.Value;
            m.ReturnToPreCombatSquare = _chkReturnPreCombat.Checked;

            _painter.Invalidate();
        }

        private void ToggleRecordRoute()
        {
            if (_currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance)) return;

            _isRecordingRoute = !_isRecordingRoute;
            if (_isRecordingRoute)
            {
                _btnRecordRoute.BackColor = Color.FromArgb(200, 100, 0);
                _btnRecordRoute.Text = "⏹️ Click Map Tiles (Recording...)";
            }
            else
            {
                _btnRecordRoute.BackColor = Color.FromArgb(70, 70, 75);
                _btnRecordRoute.Text = "✏️ Record Path (Click Tiles)";
            }
        }

        private void ClearPatrolWaypoints()
        {
            if (_currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (tile.CenterOccupant is MonsterInstance m)
            {
                m.PatrolWaypoints.Clear();
                UpdateWaypointsList(m);
                _painter.Invalidate();
            }
        }

        private void TogglePaintWanderArea()
        {
            if (_currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (!(tile.CenterOccupant is MonsterInstance)) return;

            _isPaintingWanderArea = !_isPaintingWanderArea;
            if (_isPaintingWanderArea)
            {
                _btnPaintWanderArea.BackColor = Color.FromArgb(0, 139, 139);
                _btnPaintWanderArea.Text = "⏹️ Done Painting Territory";
            }
            else
            {
                _btnPaintWanderArea.BackColor = Color.FromArgb(70, 70, 75);
                _btnPaintWanderArea.Text = "🖌️ Paint Territory (Click Tiles)";
            }
        }

        private void ClearWanderArea()
        {
            if (_currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            if (tile.CenterOccupant is MonsterInstance m)
            {
                m.WanderAreaTiles.Clear();
                UpdateWanderLabels(m);
                _painter.Invalidate();
            }
        }

        private void UpdateWanderLabels(MonsterInstance m)
        {
            if (m == null) return;
            int count = m.WanderAreaTiles != null ? m.WanderAreaTiles.Count : 0;
            _lblWanderTilesCount.Text = $"Territory: ({count} painted tiles)";
        }

        // ==========================================
        // Dungeon & Level Specifics / Loot Inspector
        // ==========================================

        public void ShowDungeonInspector(string dungeonId, int levelNum = -1)
        {
            if (string.IsNullOrEmpty(dungeonId) && _project != null && _project.Dungeons.Count > 0)
            {
                dungeonId = _project.Dungeons.Keys.FirstOrDefault();
            }
            if (string.IsNullOrEmpty(dungeonId)) return;

            _activeDungeonInspectorId = dungeonId;
            _activeDungeonInspectorLevel = levelNum;

            // Hide tile cards
            _grpTileBase.Visible = false;
            _grpEntityCard.Visible = false;
            _grpMovementCard.Visible = false;
            _grpTransition.Visible = false;
            _grpSubSquareItems.Visible = false;

            // Show dungeon inspector
            _grpDungeonInspector.Visible = true;
            _grpDungeonInspector.Top = 10;
            _grpDungeonInspector.Left = 10;

            string dName = dungeonId;
            if (_project != null && _project.Dungeons.TryGetValue(dungeonId, out var dung))
                dName = dung.Name;

            _lblDungeonHeader.Text = $"🏰 {dName}";
            if (levelNum > 0)
                _lblDungeonDetails.Text = $"Level {levelNum} Specifics ({dungeonId})";
            else
                _lblDungeonDetails.Text = $"Dungeon-Wide Specifics ({dungeonId})";

            RefreshDungeonLootTablesCombo();
            PopulateLootAddItemCombo();
        }

        private void CloseDungeonInspector()
        {
            _grpDungeonInspector.Visible = false;
            _grpTileBase.Visible = true;

            if (_painter.SelectedX >= 0 && _painter.SelectedY >= 0 && _currentLevel != null)
            {
                var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
                PopulateTileInspector(_painter.SelectedX, _painter.SelectedY, tile);
            }
            else
            {
                ReflowInspectorCards();
            }
        }

        private void RefreshDungeonLootTablesCombo()
        {
            _isPopulatingLootInspector = true;
            try
            {
                _cbDungeonLootTables.Items.Clear();
                if (_project == null || _project.LootTables == null) return;

                var list = _project.LootTables.GetForDungeon(_activeDungeonInspectorId).ToList();
                if (list.Count == 0)
                {
                    list = _project.LootTables.GetAll().ToList();
                }

                foreach (var tbl in list)
                {
                    _cbDungeonLootTables.Items.Add(tbl.Id);
                }

                if (_cbDungeonLootTables.Items.Count > 0)
                {
                    _cbDungeonLootTables.SelectedIndex = 0;
                    _selectedLootTable = _project.LootTables.Get(_cbDungeonLootTables.SelectedItem.ToString());
                }
                else
                {
                    _selectedLootTable = null;
                }
                PopulateLootTableFields();
            }
            finally
            {
                _isPopulatingLootInspector = false;
            }
        }

        private void PopulateLootAddItemCombo()
        {
            _cbLootAddItem.Items.Clear();
            if (_itemDefsList != null)
            {
                foreach (var it in _itemDefsList)
                {
                    _cbLootAddItem.Items.Add(it.Id);
                }
            }
            if (_cbLootAddItem.Items.Count > 0)
                _cbLootAddItem.SelectedIndex = 0;
        }

        private void PopulateLootTableFields()
        {
            _isPopulatingLootInspector = true;
            try
            {
                if (_selectedLootTable == null)
                {
                    _txtLootTableName.Text = "";
                    _nudLootMinGold.Value = 0;
                    _nudLootMaxGold.Value = 0;
                    _nudLootRolls.Value = 1;
                    _dgvLootEntries.Rows.Clear();
                    return;
                }

                _txtLootTableName.Text = _selectedLootTable.Name ?? _selectedLootTable.Id;
                _nudLootMinGold.Value = Math.Max(0, _selectedLootTable.MinGold);
                _nudLootMaxGold.Value = Math.Max(0, _selectedLootTable.MaxGold);
                _nudLootRolls.Value = Math.Max(1, _selectedLootTable.Rolls);

                _dgvLootEntries.Rows.Clear();
                foreach (var entry in _selectedLootTable.Entries)
                {
                    _dgvLootEntries.Rows.Add(
                        entry.ItemId,
                        entry.Weight,
                        Math.Round(entry.DropChance, 1),
                        entry.MinCount,
                        entry.MaxCount
                    );
                }
            }
            finally
            {
                _isPopulatingLootInspector = false;
            }
        }

        private void OnDungeonLootTableSelected()
        {
            if (_isPopulatingLootInspector || _project == null || _cbDungeonLootTables.SelectedItem == null) return;
            string id = _cbDungeonLootTables.SelectedItem.ToString();
            _selectedLootTable = _project.LootTables.Get(id);
            PopulateLootTableFields();
        }

        private void PromptCreateNewLootTable()
        {
            if (_project == null) return;
            string newId = $"loot_{_activeDungeonInspectorId}_{DateTime.Now.Ticks % 10000}";
            var newTable = new LootTable
            {
                Id = newId,
                Name = $"New Loot Pool ({_activeDungeonInspectorId})",
                DungeonId = _activeDungeonInspectorId,
                LevelNumber = _activeDungeonInspectorLevel,
                MinGold = 5,
                MaxGold = 25,
                Rolls = 1
            };
            newTable.Entries.Add(new LootDropEntry("potion_healing", 20, 75.0, 1, 2));
            _project.LootTables.Register(newTable);

            RefreshDungeonLootTablesCombo();
            _cbDungeonLootTables.SelectedItem = newId;
        }

        private void DeleteCurrentLootTable()
        {
            if (_selectedLootTable == null || _project == null) return;
            var confirm = MessageBox.Show($"Delete loot table '{_selectedLootTable.Name}' ({_selectedLootTable.Id})?", "Delete Loot Table", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                _project.LootTables.Remove(_selectedLootTable.Id);
                RefreshDungeonLootTablesCombo();
            }
        }

        private void OnLootTablePropsChanged()
        {
            if (_isPopulatingLootInspector || _selectedLootTable == null) return;
            _selectedLootTable.Name = _txtLootTableName.Text.Trim();
            _selectedLootTable.MinGold = (int)_nudLootMinGold.Value;
            _selectedLootTable.MaxGold = (int)_nudLootMaxGold.Value;
            _selectedLootTable.Rolls = (int)_nudLootRolls.Value;
        }

        private void OnLootTableGridChanged()
        {
            if (_isPopulatingLootInspector || _selectedLootTable == null) return;
            _selectedLootTable.Entries.Clear();
            foreach (DataGridViewRow row in _dgvLootEntries.Rows)
            {
                if (row.IsNewRow) continue;
                string itId = row.Cells[0].Value?.ToString() ?? "";
                if (string.IsNullOrEmpty(itId)) continue;

                int weight = 10;
                if (row.Cells[1].Value != null) int.TryParse(row.Cells[1].Value.ToString(), out weight);

                double chance = 100.0;
                if (row.Cells[2].Value != null) double.TryParse(row.Cells[2].Value.ToString(), out chance);

                int min = 1;
                if (row.Cells[3].Value != null) int.TryParse(row.Cells[3].Value.ToString(), out min);

                int max = 1;
                if (row.Cells[4].Value != null) int.TryParse(row.Cells[4].Value.ToString(), out max);

                _selectedLootTable.Entries.Add(new LootDropEntry(itId, weight, chance, min, max));
            }
        }

        private void AddEntryToCurrentLootTable()
        {
            if (_selectedLootTable == null) return;
            string itId = _cbLootAddItem.Text.Trim();
            if (string.IsNullOrEmpty(itId)) return;

            _selectedLootTable.Entries.Add(new LootDropEntry(itId, 10, 100.0, 1, 1));
            PopulateLootTableFields();
        }

        private void RemoveEntryFromCurrentLootTable()
        {
            if (_selectedLootTable == null || _dgvLootEntries.SelectedRows.Count == 0) return;
            int idx = _dgvLootEntries.SelectedRows[0].Index;
            if (idx >= 0 && idx < _selectedLootTable.Entries.Count)
            {
                _selectedLootTable.Entries.RemoveAt(idx);
                PopulateLootTableFields();
            }
        }

        private void SimulateTestRoll()
        {
            if (_selectedLootTable == null) return;
            var rand = new Random();
            int gold = rand.Next(_selectedLootTable.MinGold, Math.Max(_selectedLootTable.MinGold, _selectedLootTable.MaxGold) + 1);
            var drops = new List<string>();

            for (int r = 0; r < _selectedLootTable.Rolls; r++)
            {
                int totalWeight = _selectedLootTable.Entries.Sum(e => e.Weight);
                if (totalWeight <= 0) break;
                int roll = rand.Next(totalWeight);
                int acc = 0;
                foreach (var entry in _selectedLootTable.Entries)
                {
                    acc += entry.Weight;
                    if (roll < acc)
                    {
                        if (rand.NextDouble() * 100.0 <= entry.DropChance)
                        {
                            int count = rand.Next(entry.MinCount, Math.Max(entry.MinCount, entry.MaxCount) + 1);
                            drops.Add($"{entry.ItemId} x{count}");
                        }
                        break;
                    }
                }
            }

            string dropList = drops.Count > 0 ? string.Join(", ", drops) : "(None)";
            MessageBox.Show(
                $"🎲 Test Drop Roll for '{_selectedLootTable.Name}':\n\n💰 Gold Awarded: {gold} GP\n🎁 Items Dropped: {dropList}",
                "Loot Table Simulation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void OnTransitionPropertyChanged()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
            var trans = tile.Transition;
            if (trans == null) return;

            string targetD = _cbTransTargetDungeon.SelectedItem?.ToString();
            trans.TargetDungeon = (targetD == "(Current Dungeon)" || string.IsNullOrEmpty(targetD)) ? "" : targetD;
            trans.TargetLevel = (int)_nudTransTargetLevel.Value;
            trans.TargetX = (int)_nudTransTargetX.Value;
            trans.TargetY = (int)_nudTransTargetY.Value;
            trans.TargetFacing = _cbTransTargetFacing.SelectedItem?.ToString() ?? "Keep";

            trans.IsLocked = _chkTransLocked.Checked;
            trans.RequiredKey = _cbTransKey.Text.Trim();

            trans.IsTrapped = _chkTransTrapped.Checked;
            trans.TrapType = _cbTransTrapType.SelectedItem?.ToString() ?? "PoisonDart";
            trans.TrapDamage = (int)_nudTransTrapDamage.Value;
            trans.TrapIsRecurring = _chkTransRecurring.Checked;
            trans.TrapJiggleChance = (double)_nudTransJiggleChance.Value / 100.0;
            trans.TrapOpenChance = (double)_nudTransOpenChance.Value / 100.0;

            trans.IsOpen = _chkTransOpen.Checked;
            if (trans.IsOpen) trans.Open(); else trans.Close();

            trans.AutoTransitionOnStep = _chkTransAuto.Checked;
            _painter.Invalidate();
        }

        private void OnTileFlagChanged()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            int x = _painter.SelectedX;
            int y = _painter.SelectedY;
            if (x >= _currentLevel.Width || y >= _currentLevel.Height) return;

            var tile = _currentLevel.Tiles[y, x];
            if (tile == null) return;

            _painter.RecordSingleTileEdit(x, y, () =>
            {
                tile.CanWalkThru = _chkTileCanWalkThru.Checked;
                tile.CanWalkOver = _chkTileCanWalkThru.Checked;
                tile.BlocksVision = _chkTileBlocksVision.Checked;
                tile.IsBreakable = _chkTileBreakable.Checked;
                tile.BreakHP = (int)_nudTileBreakHP.Value;
                tile.MaxBreakHP = Math.Max(tile.MaxBreakHP, tile.BreakHP);
                tile.IsMoveable = _chkTileMoveable.Checked;
                tile.CanBeOpened = _chkTileCanBeOpened.Checked;
                tile.IsOpen = _chkTileIsOpen.Checked;
                tile.IsHazardous = _chkTileHazardous.Checked;
                tile.HazardDamage = (int)_nudTileHazardDamage.Value;
            });

            _painter.Invalidate();
        }

        private void OnInspectorTileTypeChanged()
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            if (Enum.TryParse<TileType>(_cbTileType.SelectedItem?.ToString(), out var tt))
            {
                _painter.RecordSingleTileEdit(_painter.SelectedX, _painter.SelectedY, () =>
                {
                    var t = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
                    t.Type = tt;
                    var defaultArch = new TileArchetype(' ', tt.ToString().ToLowerInvariant(), tt.ToString(), tt);
                    t.ApplyArchetype(defaultArch);
                });
                var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
                PopulateTileInspector(_painter.SelectedX, _painter.SelectedY, tile);
                _painter.Invalidate();
            }
        }

        private void OnSlotItemChanged(int slotIdx)
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            _painter.RecordSingleTileEdit(_painter.SelectedX, _painter.SelectedY, () =>
            {
                var tile = _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX];
                string sel = _cbSlotItems[slotIdx].SelectedItem?.ToString();
                if (string.IsNullOrEmpty(sel) || sel == "(Empty)")
                {
                    tile.Slots[slotIdx] = null;
                }
                else
                {
                    var inst = _project.Items.CreateInstance(sel);
                    tile.Slots[slotIdx] = inst;
                }
            });
        }

        private void ClearSlot(int slotIdx)
        {
            if (_isPopulatingInspector || _currentLevel == null || _painter.SelectedX < 0 || _painter.SelectedY < 0) return;
            _painter.RecordSingleTileEdit(_painter.SelectedX, _painter.SelectedY, () =>
            {
                _currentLevel.Tiles[_painter.SelectedY, _painter.SelectedX].Slots[slotIdx] = null;
                _isPopulatingInspector = true;
                try { _cbSlotItems[slotIdx].SelectedIndex = 0; } finally { _isPopulatingInspector = false; }
            });
        }

        // --- Items Tab ---
        private readonly List<ItemDefinition> _itemDefsList = new List<ItemDefinition>();

        private void PopulateItemsTab()
        {
            _itemDefsList.Clear();
            _lbItems.Items.Clear();
            _cbItemInherit.Items.Clear();
            _cbItemInherit.Items.Add("(None)");

            string itemsPath = Path.Combine(_projectDir, "items.json");
            if (File.Exists(itemsPath))
            {
                var json = JsonLite.Parse(File.ReadAllText(itemsPath));
                if (json != null && json.ArrayValue != null)
                {
                    foreach (var n in json.ArrayValue)
                    {
                        var def = new ItemDefinition
                        {
                            Id = n.GetString("id", ""),
                            Name = n.GetString("name", ""),
                            Inherit = n.GetString("inherit", ""),
                            Description = n.GetString("description", ""),
                            IsTemplate = n.GetBool("isTemplate", false),
                            Category = n.GetString("category", "Prop"),
                            SubCategory = n.GetString("subCategory", ""),
                            Weight = (float)n.GetDouble("weight", 1.0),
                            Value = n.GetInt("value", n.GetInt("sellCost", 10)),
                            Collectable = n.GetBool("collectable", true),
                            PassThru = n.GetBool("passThru", true),
                            IsEquipable = n.GetBool("isEquipable", false),
                            EquipSlot = n.GetString("equipSlot", "MainHand"),
                            AttackType = n.GetString("attackType", "Melee"),
                            Hands = n.GetInt("hands", 1),
                            MinDamage = n.GetInt("minDamage", 1),
                            MaxDamage = n.GetInt("maxDamage", 4),
                            Range = n.GetInt("range", 1),
                            AmmoType = n.GetString("ammoType", ""),
                            IsMagic = n.GetBool("isMagic", false),
                            MagicElement = n.GetString("magicElement", "None"),
                            SpellEffect = n.GetString("spellEffect", ""),
                            ManaCost = n.GetInt("manaCost", 0),
                            MagicPower = n.GetInt("magicPower", 0),
                            ArmorRating = n.GetInt("armorRating", n.GetInt("defense", 0)),
                            MagicResistance = n.GetInt("magicResistance", 0),
                            ResistElement = n.GetString("resistElement", "None"),
                            ConsumableType = n.GetString("consumableType", "None"),
                            EffectAmount = n.GetInt("effectAmount", 0),
                            Duration = (float)n.GetDouble("duration", 0.0),
                            IsDifficultTerrain = n.GetBool("isDifficultTerrain", false),
                            SpeedPenalty = (float)n.GetDouble("speedPenalty", 0.0),
                            HazardDamage = n.GetInt("hazardDamage", 0),
                            IsContainer = n.GetBool("isContainer", false),
                            Capacity = n.GetInt("capacity", 10),
                            SpriteKind = n.GetString("sprite", "rock"),
                            Color = ZoneConfig.ParseColorHex(n.GetString("color", ""), Color.Gray),
                            DefaultTrapped = n.GetString("trapped", null),
                            DefaultLocked = n.GetString("locked", null),
                            DefaultCursed = n.GetBool("cursed", false),
                            DefaultCurseType = n.GetString("curseType", null)
                        };

                        var magicNode = n.Get("magic");
                        if (magicNode != null && magicNode.ObjectValue != null)
                        {
                            def.IsMagic = true;
                            def.MagicElement = magicNode.GetString("element", def.MagicElement);
                            def.SpellEffect = magicNode.GetString("spellEffect", def.SpellEffect);
                            def.ManaCost = magicNode.GetInt("manaCost", def.ManaCost);
                            def.MagicPower = magicNode.GetInt("power", def.MagicPower);
                        }

                        var propsNode = n.Get("properties");
                        if (propsNode != null && propsNode.ObjectValue != null)
                        {
                            foreach (var kvp in propsNode.ObjectValue)
                                def.CustomProperties[kvp.Key] = kvp.Value.StringValue ?? kvp.Value.ToString();
                        }

                        _itemDefsList.Add(def);
                        _cbItemInherit.Items.Add(def.Id);
                    }
                }
            }

            FilterAndDisplayItems();

            if (_lbItems.Items.Count > 0)
                _lbItems.SelectedIndex = 0;
        }

        private void FilterAndDisplayItems()
        {
            _filteredDefsList.Clear();
            _lbItems.Items.Clear();

            string catFilter = _cbCategoryFilter?.SelectedItem?.ToString() ?? "[ All Categories ]";
            string search = _txtItemSearch?.Text?.Trim().ToLowerInvariant() ?? "";

            foreach (var it in _itemDefsList)
            {
                // Category filter check
                if (catFilter.Contains("Weapons") && !string.Equals(it.Category, "Weapon", StringComparison.OrdinalIgnoreCase)) continue;
                if (catFilter.Contains("Armor") && !string.Equals(it.Category, "Armor", StringComparison.OrdinalIgnoreCase)) continue;
                if (catFilter.Contains("Accessories") && !string.Equals(it.Category, "Accessory", StringComparison.OrdinalIgnoreCase)) continue;
                if (catFilter.Contains("Consumables") && !string.Equals(it.Category, "Consumable", StringComparison.OrdinalIgnoreCase)) continue;
                if (catFilter.Contains("Containers") && !string.Equals(it.Category, "Container", StringComparison.OrdinalIgnoreCase)) continue;
                if (catFilter.Contains("Props") && !string.Equals(it.Category, "Prop", StringComparison.OrdinalIgnoreCase)) continue;
                if (catFilter.Contains("Templates") && !it.IsTemplate) continue;

                // Search keyword check
                if (!string.IsNullOrEmpty(search))
                {
                    bool match = (it.Id != null && it.Id.ToLowerInvariant().Contains(search))
                              || (it.Name != null && it.Name.ToLowerInvariant().Contains(search))
                              || (it.SubCategory != null && it.SubCategory.ToLowerInvariant().Contains(search))
                              || (it.Description != null && it.Description.ToLowerInvariant().Contains(search));
                    if (!match) continue;
                }

                _filteredDefsList.Add(it);

                string tag = "[Prop]";
                if (it.IsTemplate) tag = "[Template]";
                else if (string.Equals(it.Category, "Weapon", StringComparison.OrdinalIgnoreCase)) tag = "[Weapon]";
                else if (string.Equals(it.Category, "Armor", StringComparison.OrdinalIgnoreCase)) tag = "[Armor]";
                else if (string.Equals(it.Category, "Accessory", StringComparison.OrdinalIgnoreCase)) tag = "[Accessory]";
                else if (string.Equals(it.Category, "Consumable", StringComparison.OrdinalIgnoreCase)) tag = "[Potion]";
                else if (string.Equals(it.Category, "Container", StringComparison.OrdinalIgnoreCase)) tag = "[Container]";

                _lbItems.Items.Add($"{tag} {it.Id} - {it.Name}");
            }

            if (_lblItemCount != null)
                _lblItemCount.Text = $"{_filteredDefsList.Count} of {_itemDefsList.Count} items";

            if (_lbItems.Items.Count > 0 && _lbItems.SelectedIndex < 0)
                _lbItems.SelectedIndex = 0;
        }

        private void OnItemSelectedInList()
        {
            int idx = _lbItems.SelectedIndex;
            if (idx < 0 || idx >= _filteredDefsList.Count) return;

            _selectedItemDef = _filteredDefsList[idx];

            // 1. Identity
            _txtItemId.Text = _selectedItemDef.Id;
            _txtItemName.Text = _selectedItemDef.Name;
            _cbCategory.SelectedItem = _selectedItemDef.Category;
            _cbSubCategory.Text = _selectedItemDef.SubCategory ?? "";
            _cbItemInherit.SelectedItem = string.IsNullOrEmpty(_selectedItemDef.Inherit) ? "(None)" : _selectedItemDef.Inherit;
            _txtDescription.Text = _selectedItemDef.Description ?? "";
            _chkIsTemplate.Checked = _selectedItemDef.IsTemplate;

            // 2. Physical
            _nudWeight.Value = (decimal)Math.Max(0, _selectedItemDef.Weight);
            _nudValue.Value = Math.Max(0, _selectedItemDef.Value);
            _chkCollectable.Checked = _selectedItemDef.Collectable;
            _chkPassThru.Checked = _selectedItemDef.PassThru;
            _cbSpriteKind.SelectedItem = _selectedItemDef.SpriteKind;
            _currentItemColor = _selectedItemDef.Color;
            _pnlItemColorSwatch.BackColor = _selectedItemDef.Color;

            // 3. Combat & Weapon
            if (string.Equals(_selectedItemDef.AttackType, "Ranged", StringComparison.OrdinalIgnoreCase))
                _rbAttackRanged.Checked = true;
            else if (string.Equals(_selectedItemDef.AttackType, "Cast", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(_selectedItemDef.AttackType, "Magic", StringComparison.OrdinalIgnoreCase))
                _rbAttackCast.Checked = true;
            else
                _rbAttackMelee.Checked = true;

            _cbHands.SelectedIndex = (_selectedItemDef.Hands == 2) ? 1 : 0;
            _nudMinDamage.Value = Math.Max(0, _selectedItemDef.MinDamage);
            _nudMaxDamage.Value = Math.Max(1, _selectedItemDef.MaxDamage);
            _nudRange.Value = Math.Max(1, _selectedItemDef.Range);
            _txtAmmoType.Text = _selectedItemDef.AmmoType ?? "";

            // 4. Affects & Modifiers
            _chkHasEffect.Checked = _selectedItemDef.HasEffect;
            _cbEffectType.SelectedItem = string.IsNullOrEmpty(_selectedItemDef.EffectType) ? "None" : _selectedItemDef.EffectType;
            if (_cbEffectType.SelectedIndex < 0) _cbEffectType.SelectedIndex = 0;
            _txtProcEffect.Text = _selectedItemDef.ProcEffect ?? "";
            _nudResourceCost.Value = Math.Max(0, _selectedItemDef.ResourceCost);
            _nudEffectPower.Value = Math.Max(0, _selectedItemDef.EffectPower);
            UpdateEffectsControlsState();

            // 5. Armor
            _cbEquipSlot.SelectedItem = string.IsNullOrEmpty(_selectedItemDef.EquipSlot) ? "MainHand" : _selectedItemDef.EquipSlot;
            _nudArmorRating.Value = Math.Max(0, _selectedItemDef.ArmorRating);
            _nudEnergyResistance.Value = Math.Max(0, _selectedItemDef.EnergyResistance);
            _cbResistType.SelectedItem = string.IsNullOrEmpty(_selectedItemDef.ResistType) ? "None" : _selectedItemDef.ResistType;
            if (_cbResistType.SelectedIndex < 0) _cbResistType.SelectedIndex = 0;

            // 6. Consumables
            _cbConsumableType.SelectedItem = string.IsNullOrEmpty(_selectedItemDef.ConsumableType) ? "None" : _selectedItemDef.ConsumableType;
            _nudEffectAmount.Value = Math.Max(0, _selectedItemDef.EffectAmount);
            _nudDuration.Value = (decimal)Math.Max(0, _selectedItemDef.Duration);

            // 7. Containers
            _chkContainer.Checked = _selectedItemDef.IsContainer;
            _nudCapacity.Value = Math.Max(1, _selectedItemDef.Capacity);
            _txtTrapped.Text = _selectedItemDef.DefaultTrapped ?? "";
            _txtLocked.Text = _selectedItemDef.DefaultLocked ?? "";
            _chkCursed.Checked = _selectedItemDef.DefaultCursed;
            _txtCurseType.Text = _selectedItemDef.DefaultCurseType ?? "";

            // 8. Terrain
            _chkDifficult.Checked = _selectedItemDef.IsDifficultTerrain;
            _nudSpeedPenalty.Value = (decimal)_selectedItemDef.SpeedPenalty;
            _nudHazardDamage.Value = _selectedItemDef.HazardDamage;

            // 9. Custom properties
            PopulateCustomPropertiesList();

            UpdateCategoryPanelsVisibility();
        }

        private void UpdateCategoryPanelsVisibility()
        {
            string cat = _cbCategory.SelectedItem?.ToString() ?? "Prop";
            bool isWeapon = string.Equals(cat, "Weapon", StringComparison.OrdinalIgnoreCase);
            bool isArmor = string.Equals(cat, "Armor", StringComparison.OrdinalIgnoreCase);
            bool isAcc = string.Equals(cat, "Accessory", StringComparison.OrdinalIgnoreCase);
            bool isConsumable = string.Equals(cat, "Consumable", StringComparison.OrdinalIgnoreCase);
            bool isContainer = string.Equals(cat, "Container", StringComparison.OrdinalIgnoreCase);
            bool isProp = string.Equals(cat, "Prop", StringComparison.OrdinalIgnoreCase);

            _grpWeapon.Enabled = isWeapon;
            _grpEffects.Enabled = isWeapon || isAcc || _chkHasEffect.Checked;
            _grpArmor.Enabled = isArmor || isAcc;
            _grpConsumable.Enabled = isConsumable;
            _grpContainer.Enabled = isContainer || isProp;
            _grpTerrain.Enabled = isProp;
        }

        private void UpdateEffectsControlsState()
        {
            bool active = _chkHasEffect.Checked || _rbAttackCast.Checked;
            _cbEffectType.Enabled = active;
            _txtProcEffect.Enabled = active;
            _nudResourceCost.Enabled = active;
            _nudEffectPower.Enabled = active;
        }

        private void PopulateCustomPropertiesList()
        {
            _pnlCustomPropsList.Controls.Clear();
            if (_selectedItemDef == null || _selectedItemDef.CustomProperties.Count == 0)
            {
                var lblNone = new Label { Text = "(No custom properties set)", ForeColor = Color.Gray, AutoSize = true, Top = 5, Left = 5 };
                _pnlCustomPropsList.Controls.Add(lblNone);
                return;
            }

            int py = 2;
            foreach (var kvp in _selectedItemDef.CustomProperties)
            {
                var lbl = new Label
                {
                    Text = $"{kvp.Key}: {kvp.Value}",
                    Left = 5,
                    Top = py,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f)
                };
                _pnlCustomPropsList.Controls.Add(lbl);
                py += 20;
            }
        }

        private void PromptAddCustomProperty()
        {
            if (_selectedItemDef == null) return;

            using (var form = new Form { Text = "Add Custom Property", Width = 320, Height = 190, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false })
            {
                var lblK = new Label { Text = "Property Name (e.g. material, blastRadius):", Left = 15, Top = 12, AutoSize = true };
                var txtK = new TextBox { Left = 15, Top = 32, Width = 270 };
                var lblV = new Label { Text = "Value:", Left = 15, Top = 62, AutoSize = true };
                var txtV = new TextBox { Left = 15, Top = 82, Width = 270 };
                var btnOk = new Button { Text = "Add", Left = 125, Top = 115, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Left = 210, Top = 115, Width = 75, DialogResult = DialogResult.Cancel };
                form.Controls.AddRange(new Control[] { lblK, txtK, lblV, txtV, btnOk, btnCancel });
                form.AcceptButton = btnOk;

                if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txtK.Text))
                {
                    _selectedItemDef.CustomProperties[txtK.Text.Trim()] = txtV.Text.Trim();
                    PopulateCustomPropertiesList();
                }
            }
        }

        private void ApplyItemDetails()
        {
            if (_selectedItemDef == null) return;

            _selectedItemDef.Id = _txtItemId.Text.Trim();
            _selectedItemDef.Name = _txtItemName.Text.Trim();
            _selectedItemDef.Category = _cbCategory.SelectedItem?.ToString() ?? "Prop";
            _selectedItemDef.SubCategory = _cbSubCategory.Text.Trim();
            string inh = _cbItemInherit.SelectedItem?.ToString();
            _selectedItemDef.Inherit = (inh == "(None)" || string.IsNullOrEmpty(inh)) ? null : inh;
            _selectedItemDef.Description = _txtDescription.Text.Trim();
            _selectedItemDef.IsTemplate = _chkIsTemplate.Checked;

            _selectedItemDef.Weight = (float)_nudWeight.Value;
            _selectedItemDef.Value = (int)_nudValue.Value;
            _selectedItemDef.Collectable = _chkCollectable.Checked;
            _selectedItemDef.PassThru = _chkPassThru.Checked;
            _selectedItemDef.SpriteKind = _cbSpriteKind.SelectedItem?.ToString() ?? "rock";
            _selectedItemDef.Color = _currentItemColor;

            // Combat
            if (_rbAttackRanged.Checked) _selectedItemDef.AttackType = "Ranged";
            else if (_rbAttackCast.Checked) _selectedItemDef.AttackType = "Cast";
            else _selectedItemDef.AttackType = "Melee";

            _selectedItemDef.Hands = (_cbHands.SelectedIndex == 1) ? 2 : 1;
            _selectedItemDef.MinDamage = (int)_nudMinDamage.Value;
            _selectedItemDef.MaxDamage = (int)_nudMaxDamage.Value;
            _selectedItemDef.Range = (int)_nudRange.Value;
            _selectedItemDef.AmmoType = _txtAmmoType.Text.Trim();

            // Affects & Modifiers
            _selectedItemDef.HasEffect = _chkHasEffect.Checked || _rbAttackCast.Checked;
            _selectedItemDef.EffectType = _cbEffectType.SelectedItem?.ToString() ?? "None";
            _selectedItemDef.ProcEffect = _txtProcEffect.Text.Trim();
            _selectedItemDef.ResourceCost = (int)_nudResourceCost.Value;
            _selectedItemDef.EffectPower = (int)_nudEffectPower.Value;

            // Armor
            _selectedItemDef.EquipSlot = _cbEquipSlot.SelectedItem?.ToString() ?? "MainHand";
            _selectedItemDef.ArmorRating = (int)_nudArmorRating.Value;
            _selectedItemDef.EnergyResistance = (int)_nudEnergyResistance.Value;
            _selectedItemDef.ResistType = _cbResistType.SelectedItem?.ToString() ?? "None";

            // Consumable
            _selectedItemDef.ConsumableType = _cbConsumableType.SelectedItem?.ToString() ?? "None";
            _selectedItemDef.EffectAmount = (int)_nudEffectAmount.Value;
            _selectedItemDef.Duration = (float)_nudDuration.Value;

            // Container
            _selectedItemDef.IsContainer = _chkContainer.Checked;
            _selectedItemDef.Capacity = (int)_nudCapacity.Value;
            _selectedItemDef.DefaultTrapped = string.IsNullOrWhiteSpace(_txtTrapped.Text) ? null : _txtTrapped.Text.Trim();
            _selectedItemDef.DefaultLocked = string.IsNullOrWhiteSpace(_txtLocked.Text) ? null : _txtLocked.Text.Trim();
            _selectedItemDef.DefaultCursed = _chkCursed.Checked;
            _selectedItemDef.DefaultCurseType = string.IsNullOrWhiteSpace(_txtCurseType.Text) ? null : _txtCurseType.Text.Trim();

            // Terrain
            _selectedItemDef.IsDifficultTerrain = _chkDifficult.Checked;
            _selectedItemDef.SpeedPenalty = (float)_nudSpeedPenalty.Value;
            _selectedItemDef.HazardDamage = (int)_nudHazardDamage.Value;

            // Refresh list entry
            FilterAndDisplayItems();

            for (int i = 0; i < _filteredDefsList.Count; i++)
            {
                if (_filteredDefsList[i] == _selectedItemDef)
                {
                    _lbItems.SelectedIndex = i;
                    break;
                }
            }

            MessageBox.Show($"Item '{_selectedItemDef.Id}' changes applied in memory.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CreateNewBlankItem()
        {
            var newItem = new ItemDefinition
            {
                Id = "new_item_" + (_itemDefsList.Count + 1),
                Name = "New Item",
                Category = "Prop",
                Collectable = true,
                PassThru = true,
                SpriteKind = "rock",
                Color = Color.LightSlateGray
            };
            _itemDefsList.Add(newItem);
            _cbItemInherit.Items.Add(newItem.Id);
            FilterAndDisplayItems();

            for (int i = 0; i < _filteredDefsList.Count; i++)
            {
                if (_filteredDefsList[i] == newItem)
                {
                    _lbItems.SelectedIndex = i;
                    break;
                }
            }
        }

        private void CreateFromPreset(string presetKind)
        {
            ItemDefinition it;
            switch (presetKind)
            {
                case "sword_short":
                    it = new ItemDefinition
                    {
                        Id = "short_sword_" + (_itemDefsList.Count + 1),
                        Name = "Short Sword",
                        Inherit = "generic_weapon_sword",
                        Category = "Weapon",
                        SubCategory = "Sword",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "MainHand",
                        AttackType = "Melee",
                        Hands = 1,
                        MinDamage = 3,
                        MaxDamage = 7,
                        Range = 1,
                        Weight = 2.5f,
                        Value = 25,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(208, 208, 208)
                    };
                    it.CustomProperties["material"] = "Iron";
                    break;

                case "sword_long":
                    it = new ItemDefinition
                    {
                        Id = "long_sword_" + (_itemDefsList.Count + 1),
                        Name = "Steel Long Sword",
                        Inherit = "generic_weapon_sword",
                        Category = "Weapon",
                        SubCategory = "Sword",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "MainHand",
                        AttackType = "Melee",
                        Hands = 1,
                        MinDamage = 4,
                        MaxDamage = 9,
                        Range = 1,
                        Weight = 3.5f,
                        Value = 45,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(232, 232, 232)
                    };
                    it.CustomProperties["material"] = "Steel";
                    break;

                case "sword_great":
                    it = new ItemDefinition
                    {
                        Id = "great_sword_" + (_itemDefsList.Count + 1),
                        Name = "Claymore Great Sword",
                        Inherit = "generic_weapon_sword",
                        Category = "Weapon",
                        SubCategory = "Sword",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "TwoHand",
                        AttackType = "Melee",
                        Hands = 2,
                        MinDamage = 6,
                        MaxDamage = 14,
                        Range = 1,
                        Weight = 6.0f,
                        Value = 75,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(176, 176, 176)
                    };
                    it.CustomProperties["material"] = "Steel";
                    break;

                case "dagger":
                    it = new ItemDefinition
                    {
                        Id = "stiletto_" + (_itemDefsList.Count + 1),
                        Name = "Stiletto Dagger",
                        Category = "Weapon",
                        SubCategory = "Dagger",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "MainHand",
                        AttackType = "Melee",
                        Hands = 1,
                        MinDamage = 2,
                        MaxDamage = 5,
                        Range = 1,
                        Weight = 0.8f,
                        Value = 20,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(176, 196, 222)
                    };
                    break;

                case "bow":
                    it = new ItemDefinition
                    {
                        Id = "hunting_bow_" + (_itemDefsList.Count + 1),
                        Name = "Composite Hunting Bow",
                        Category = "Weapon",
                        SubCategory = "Bow",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "TwoHand",
                        AttackType = "Ranged",
                        Hands = 2,
                        MinDamage = 3,
                        MaxDamage = 8,
                        Range = 6,
                        AmmoType = "arrow",
                        Weight = 2.0f,
                        Value = 35,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(139, 90, 43)
                    };
                    break;

                case "crossbow":
                    it = new ItemDefinition
                    {
                        Id = "crossbow_" + (_itemDefsList.Count + 1),
                        Name = "Heavy Crossbow",
                        Category = "Weapon",
                        SubCategory = "Bow",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "TwoHand",
                        AttackType = "Ranged",
                        Hands = 2,
                        MinDamage = 5,
                        MaxDamage = 12,
                        Range = 7,
                        AmmoType = "bolt",
                        Weight = 4.5f,
                        Value = 80,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(105, 105, 105)
                    };
                    break;

                case "staff_fire":
                    it = new ItemDefinition
                    {
                        Id = "staff_fire_" + (_itemDefsList.Count + 1),
                        Name = "Staff of the Fire Pyre",
                        Inherit = "generic_weapon_staff",
                        Category = "Weapon",
                        SubCategory = "Staff",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "TwoHand",
                        AttackType = "Cast",
                        Hands = 2,
                        MinDamage = 6,
                        MaxDamage = 14,
                        Range = 5,
                        HasEffect = true,
                        EffectType = "Fire",
                        ProcEffect = "Fireball",
                        ResourceCost = 8,
                        EffectPower = 12,
                        Weight = 3.8f,
                        Value = 120,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(224, 64, 32)
                    };
                    break;

                case "wand_arcane":
                    it = new ItemDefinition
                    {
                        Id = "wand_arcane_" + (_itemDefsList.Count + 1),
                        Name = "Arcane Wand of Sparks",
                        Category = "Weapon",
                        SubCategory = "Wand",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "MainHand",
                        AttackType = "Cast",
                        Hands = 1,
                        MinDamage = 3,
                        MaxDamage = 8,
                        Range = 4,
                        HasEffect = true,
                        EffectType = "Arcane",
                        ProcEffect = "Arcane Bolt",
                        ResourceCost = 4,
                        Weight = 0.6f,
                        Value = 60,
                        SpriteKind = "weapon",
                        Color = Color.FromArgb(155, 89, 182)
                    };
                    break;

                case "shield_tower":
                    it = new ItemDefinition
                    {
                        Id = "tower_shield_" + (_itemDefsList.Count + 1),
                        Name = "Reinforced Tower Shield",
                        Inherit = "generic_armor_shield",
                        Category = "Armor",
                        SubCategory = "Shield",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "OffHand",
                        ArmorRating = 6,
                        Weight = 6.5f,
                        Value = 65,
                        SpriteKind = "shield",
                        Color = Color.FromArgb(80, 96, 112)
                    };
                    break;

                case "helm_paladin":
                    it = new ItemDefinition
                    {
                        Id = "paladin_helm_" + (_itemDefsList.Count + 1),
                        Name = "Paladin's Visor Helm",
                        Category = "Armor",
                        SubCategory = "Helmet",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "Head",
                        ArmorRating = 4,
                        MagicResistance = 10,
                        ResistElement = "Holy",
                        Weight = 3.5f,
                        Value = 55,
                        SpriteKind = "armor",
                        Color = Color.FromArgb(112, 128, 144)
                    };
                    break;

                case "chest_crusader":
                    it = new ItemDefinition
                    {
                        Id = "crusader_plate_" + (_itemDefsList.Count + 1),
                        Name = "Crusader's Breastplate",
                        Category = "Armor",
                        SubCategory = "Chest",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "Chest",
                        ArmorRating = 8,
                        Weight = 9.5f,
                        Value = 110,
                        SpriteKind = "armor",
                        Color = Color.FromArgb(105, 105, 105)
                    };
                    break;

                case "boots_ranger":
                    it = new ItemDefinition
                    {
                        Id = "ranger_boots_" + (_itemDefsList.Count + 1),
                        Name = "Ranger's Leather Boots",
                        Category = "Armor",
                        SubCategory = "Boots",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "Feet",
                        ArmorRating = 2,
                        Weight = 1.5f,
                        Value = 25,
                        SpriteKind = "armor",
                        Color = Color.FromArgb(139, 69, 19)
                    };
                    break;

                case "ring_fire":
                    it = new ItemDefinition
                    {
                        Id = "ring_flame_" + (_itemDefsList.Count + 1),
                        Name = "Ring of Flame Resistance",
                        Category = "Accessory",
                        SubCategory = "Ring",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "Ring",
                        MagicResistance = 25,
                        ResistElement = "Fire",
                        Weight = 0.1f,
                        Value = 75,
                        SpriteKind = "key",
                        Color = Color.FromArgb(255, 69, 0)
                    };
                    break;

                case "amulet_holy":
                    it = new ItemDefinition
                    {
                        Id = "amulet_holy_" + (_itemDefsList.Count + 1),
                        Name = "Amulet of Divine Blessing",
                        Category = "Accessory",
                        SubCategory = "Neck",
                        Collectable = true,
                        PassThru = true,
                        IsEquipable = true,
                        EquipSlot = "Neck",
                        MagicResistance = 15,
                        ResistElement = "Holy",
                        Weight = 0.2f,
                        Value = 85,
                        SpriteKind = "key",
                        Color = Color.FromArgb(255, 215, 0)
                    };
                    break;

                case "potion_hp":
                    it = new ItemDefinition
                    {
                        Id = "potion_hp_" + (_itemDefsList.Count + 1),
                        Name = "Potion of Healing",
                        Inherit = "generic_consumable_potion",
                        Category = "Consumable",
                        SubCategory = "Potion",
                        Collectable = true,
                        PassThru = true,
                        ConsumableType = "HealHP",
                        EffectAmount = 50,
                        Weight = 0.5f,
                        Value = 20,
                        SpriteKind = "potion",
                        Color = Color.FromArgb(224, 48, 48)
                    };
                    break;

                case "potion_mp":
                    it = new ItemDefinition
                    {
                        Id = "potion_mp_" + (_itemDefsList.Count + 1),
                        Name = "Potion of Mana",
                        Inherit = "generic_consumable_potion",
                        Category = "Consumable",
                        SubCategory = "Potion",
                        Collectable = true,
                        PassThru = true,
                        ConsumableType = "RestoreMP",
                        EffectAmount = 40,
                        Weight = 0.5f,
                        Value = 25,
                        SpriteKind = "potion",
                        Color = Color.FromArgb(48, 96, 224)
                    };
                    break;

                case "potion_antidote":
                    it = new ItemDefinition
                    {
                        Id = "potion_antidote_" + (_itemDefsList.Count + 1),
                        Name = "Alchemical Antidote",
                        Inherit = "generic_consumable_potion",
                        Category = "Consumable",
                        SubCategory = "Potion",
                        Collectable = true,
                        PassThru = true,
                        ConsumableType = "CurePoison",
                        EffectAmount = 1,
                        Weight = 0.4f,
                        Value = 15,
                        SpriteKind = "potion",
                        Color = Color.FromArgb(48, 192, 64)
                    };
                    break;

                case "scroll_fireball":
                    it = new ItemDefinition
                    {
                        Id = "scroll_fireball_" + (_itemDefsList.Count + 1),
                        Name = "Scroll of Fireball",
                        Category = "Consumable",
                        SubCategory = "Scroll",
                        Collectable = true,
                        PassThru = true,
                        ConsumableType = "CastSpell",
                        SpellEffect = "Fireball",
                        EffectAmount = 25,
                        Weight = 0.2f,
                        Value = 35,
                        SpriteKind = "scroll",
                        Color = Color.FromArgb(245, 222, 179)
                    };
                    break;

                case "scroll_lightning":
                    it = new ItemDefinition
                    {
                        Id = "scroll_lightning_" + (_itemDefsList.Count + 1),
                        Name = "Scroll of Lightning",
                        Category = "Consumable",
                        SubCategory = "Scroll",
                        Collectable = true,
                        PassThru = true,
                        ConsumableType = "CastSpell",
                        SpellEffect = "Chain Lightning",
                        EffectAmount = 35,
                        Weight = 0.2f,
                        Value = 45,
                        SpriteKind = "scroll",
                        Color = Color.FromArgb(224, 224, 128)
                    };
                    break;

                case "chest_wood":
                    it = new ItemDefinition
                    {
                        Id = "chest_wood_" + (_itemDefsList.Count + 1),
                        Name = "Wooden Chest",
                        Category = "Container",
                        SubCategory = "Chest",
                        Collectable = false,
                        PassThru = false,
                        IsContainer = true,
                        Capacity = 12,
                        Weight = 25.0f,
                        Value = 15,
                        SpriteKind = "chest",
                        Color = Color.FromArgb(139, 90, 43)
                    };
                    break;

                case "chest_trapped":
                    it = new ItemDefinition
                    {
                        Id = "chest_trapped_" + (_itemDefsList.Count + 1),
                        Name = "Trapped Poison Chest",
                        Inherit = "small_chest",
                        Category = "Container",
                        SubCategory = "Chest",
                        Collectable = false,
                        PassThru = false,
                        IsContainer = true,
                        Capacity = 12,
                        DefaultTrapped = "poison_dart",
                        Weight = 25.0f,
                        Value = 20,
                        SpriteKind = "chest",
                        Color = Color.FromArgb(139, 90, 43)
                    };
                    break;

                case "chest_locked":
                    it = new ItemDefinition
                    {
                        Id = "chest_locked_" + (_itemDefsList.Count + 1),
                        Name = "Ironbound Locked Chest",
                        Inherit = "small_chest",
                        Category = "Container",
                        SubCategory = "Chest",
                        Collectable = false,
                        PassThru = false,
                        IsContainer = true,
                        Capacity = 16,
                        DefaultLocked = "key_iron",
                        Weight = 35.0f,
                        Value = 35,
                        SpriteKind = "chest",
                        Color = Color.FromArgb(100, 100, 110)
                    };
                    break;

                case "prop_rock":
                    it = new ItemDefinition
                    {
                        Id = "rock_" + (_itemDefsList.Count + 1),
                        Name = "Small Rock",
                        Category = "Prop",
                        SubCategory = "Rock",
                        Collectable = true,
                        PassThru = true,
                        Weight = 1.0f,
                        Value = 0,
                        SpriteKind = "rock",
                        Color = Color.FromArgb(144, 144, 144)
                    };
                    break;

                case "prop_boulder":
                    it = new ItemDefinition
                    {
                        Id = "boulder_" + (_itemDefsList.Count + 1),
                        Name = "Heavy Boulder",
                        Category = "Prop",
                        SubCategory = "Boulder",
                        Collectable = false,
                        PassThru = false,
                        Weight = 150.0f,
                        Value = 0,
                        SpriteKind = "rock",
                        Color = Color.FromArgb(80, 80, 80)
                    };
                    break;

                case "prop_rubble":
                    it = new ItemDefinition
                    {
                        Id = "rubble_" + (_itemDefsList.Count + 1),
                        Name = "Loose Rubble",
                        Category = "Prop",
                        SubCategory = "Rubble",
                        Collectable = false,
                        PassThru = true,
                        IsDifficultTerrain = true,
                        SpeedPenalty = 0.4f,
                        Weight = 10.0f,
                        Value = 0,
                        SpriteKind = "rock",
                        Color = Color.FromArgb(120, 101, 80)
                    };
                    break;

                case "prop_spikes":
                    it = new ItemDefinition
                    {
                        Id = "spikes_" + (_itemDefsList.Count + 1),
                        Name = "Floor Spikes",
                        Category = "Prop",
                        SubCategory = "Spikes",
                        Collectable = false,
                        PassThru = true,
                        IsDifficultTerrain = true,
                        HazardDamage = 15,
                        Weight = 0.0f,
                        Value = 0,
                        SpriteKind = "rock",
                        Color = Color.FromArgb(139, 0, 0)
                    };
                    break;

                case "prop_key":
                    it = new ItemDefinition
                    {
                        Id = "key_iron_" + (_itemDefsList.Count + 1),
                        Name = "Iron Key",
                        Category = "Prop",
                        SubCategory = "Key",
                        Collectable = true,
                        PassThru = true,
                        Weight = 0.2f,
                        Value = 5,
                        SpriteKind = "key",
                        Color = Color.FromArgb(192, 192, 192)
                    };
                    break;

                default:
                    it = new ItemDefinition { Id = "item_" + (_itemDefsList.Count + 1), Name = "New Item", Category = "Prop" };
                    break;
            }

            _itemDefsList.Add(it);
            _cbItemInherit.Items.Add(it.Id);
            FilterAndDisplayItems();

            for (int i = 0; i < _filteredDefsList.Count; i++)
            {
                if (_filteredDefsList[i] == it)
                {
                    _lbItems.SelectedIndex = i;
                    break;
                }
            }
        }

        private void DuplicateItem()
        {
            if (_selectedItemDef == null) return;
            var dup = _selectedItemDef.Clone();
            dup.Id = _selectedItemDef.Id + "_copy";
            dup.Name = _selectedItemDef.Name + " (Copy)";
            _itemDefsList.Add(dup);
            _cbItemInherit.Items.Add(dup.Id);
            FilterAndDisplayItems();

            for (int i = 0; i < _filteredDefsList.Count; i++)
            {
                if (_filteredDefsList[i] == dup)
                {
                    _lbItems.SelectedIndex = i;
                    break;
                }
            }
        }

        private void DeleteSelectedItem()
        {
            if (_selectedItemDef == null) return;
            if (MessageBox.Show($"Delete item '{_selectedItemDef.Id}' ({_selectedItemDef.Name})?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _itemDefsList.Remove(_selectedItemDef);
                FilterAndDisplayItems();
            }
        }

        private void SaveItemsJson()
        {
            if (string.IsNullOrEmpty(_projectDir)) return;
            string path = Path.Combine(_projectDir, "items.json");

            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < _itemDefsList.Count; i++)
            {
                var it = _itemDefsList[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"id\": \"{it.Id}\",");
                sb.AppendLine($"    \"name\": \"{it.Name}\",");
                if (!string.IsNullOrEmpty(it.Inherit))
                    sb.AppendLine($"    \"inherit\": \"{it.Inherit}\",");
                if (!string.IsNullOrEmpty(it.Category) && it.Category != "Prop")
                    sb.AppendLine($"    \"category\": \"{it.Category}\",");
                if (!string.IsNullOrEmpty(it.SubCategory))
                    sb.AppendLine($"    \"subCategory\": \"{it.SubCategory}\",");
                if (!string.IsNullOrEmpty(it.Description))
                    sb.AppendLine($"    \"description\": \"{it.Description.Replace("\"", "\\\"")}\",");
                if (it.IsTemplate)
                    sb.AppendLine("    \"isTemplate\": true,");

                sb.AppendLine($"    \"collectable\": {(it.Collectable ? "true" : "false")},");
                sb.AppendLine($"    \"passThru\": {(it.PassThru ? "true" : "false")},");

                if (it.Weight > 0)
                    sb.AppendLine($"    \"weight\": {it.Weight:0.0},");
                if (it.Value > 0)
                    sb.AppendLine($"    \"value\": {it.Value},");

                // Combat
                if (string.Equals(it.Category, "Weapon", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"    \"attackType\": \"{it.AttackType}\",");
                    sb.AppendLine($"    \"hands\": {it.Hands},");
                    sb.AppendLine($"    \"minDamage\": {it.MinDamage},");
                    sb.AppendLine($"    \"maxDamage\": {it.MaxDamage},");
                    if (it.Range > 1) sb.AppendLine($"    \"range\": {it.Range},");
                    if (!string.IsNullOrEmpty(it.AmmoType)) sb.AppendLine($"    \"ammoType\": \"{it.AmmoType}\",");
                }

                // Affects & Modifiers (Genre-Agnostic)
                if (it.HasEffect)
                {
                    sb.AppendLine("    \"hasEffect\": true,");
                    if (!string.IsNullOrEmpty(it.EffectType) && it.EffectType != "None")
                    {
                        sb.AppendLine($"    \"effectType\": \"{it.EffectType}\",");
                        sb.AppendLine($"    \"magicElement\": \"{it.EffectType}\",");
                    }
                    if (!string.IsNullOrEmpty(it.ProcEffect))
                    {
                        sb.AppendLine($"    \"procEffect\": \"{it.ProcEffect}\",");
                        sb.AppendLine($"    \"spellEffect\": \"{it.ProcEffect}\",");
                    }
                    if (it.ResourceCost > 0)
                    {
                        sb.AppendLine($"    \"resourceCost\": {it.ResourceCost},");
                        sb.AppendLine($"    \"manaCost\": {it.ResourceCost},");
                    }
                    if (it.EffectPower > 0)
                    {
                        sb.AppendLine($"    \"effectPower\": {it.EffectPower},");
                        sb.AppendLine($"    \"magicPower\": {it.EffectPower},");
                    }
                }

                // Armor & Defense
                if (string.Equals(it.Category, "Armor", StringComparison.OrdinalIgnoreCase) || string.Equals(it.Category, "Accessory", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(it.EquipSlot)) sb.AppendLine($"    \"equipSlot\": \"{it.EquipSlot}\",");
                    if (it.ArmorRating > 0) sb.AppendLine($"    \"armorRating\": {it.ArmorRating},");
                    if (it.EnergyResistance > 0)
                    {
                        sb.AppendLine($"    \"energyResistance\": {it.EnergyResistance},");
                        sb.AppendLine($"    \"magicResistance\": {it.EnergyResistance},");
                    }
                    if (!string.IsNullOrEmpty(it.ResistType) && it.ResistType != "None")
                    {
                        sb.AppendLine($"    \"resistType\": \"{it.ResistType}\",");
                        sb.AppendLine($"    \"resistElement\": \"{it.ResistType}\",");
                    }
                }

                // Consumables
                if (string.Equals(it.Category, "Consumable", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(it.ConsumableType) && it.ConsumableType != "None") sb.AppendLine($"    \"consumableType\": \"{it.ConsumableType}\",");
                    if (it.EffectAmount > 0) sb.AppendLine($"    \"effectAmount\": {it.EffectAmount},");
                    if (it.Duration > 0) sb.AppendLine($"    \"duration\": {it.Duration:0.0},");
                }

                // Containers
                if (it.IsContainer)
                {
                    sb.AppendLine("    \"isContainer\": true,");
                    sb.AppendLine($"    \"capacity\": {it.Capacity},");
                }
                if (!string.IsNullOrEmpty(it.DefaultTrapped))
                    sb.AppendLine($"    \"trapped\": \"{it.DefaultTrapped}\",");
                if (!string.IsNullOrEmpty(it.DefaultLocked))
                    sb.AppendLine($"    \"locked\": \"{it.DefaultLocked}\",");
                if (it.DefaultCursed)
                {
                    sb.AppendLine("    \"cursed\": true,");
                    if (!string.IsNullOrEmpty(it.DefaultCurseType))
                        sb.AppendLine($"    \"curseType\": \"{it.DefaultCurseType}\",");
                }

                // Terrain
                if (it.IsDifficultTerrain)
                {
                    sb.AppendLine("    \"isDifficultTerrain\": true,");
                    sb.AppendLine($"    \"speedPenalty\": {it.SpeedPenalty:0.00},");
                    sb.AppendLine($"    \"hazardDamage\": {it.HazardDamage},");
                }

                // Custom Properties
                if (it.CustomProperties.Count > 0)
                {
                    sb.AppendLine("    \"properties\": {");
                    int cpIdx = 0;
                    foreach (var cp in it.CustomProperties)
                    {
                        sb.Append($"      \"{cp.Key}\": \"{cp.Value.Replace("\"", "\\\"")}\"");
                        if (cpIdx < it.CustomProperties.Count - 1) sb.Append(",");
                        sb.AppendLine();
                        cpIdx++;
                    }
                    sb.AppendLine("    },");
                }

                sb.AppendLine($"    \"sprite\": \"{it.SpriteKind}\",");
                sb.AppendLine($"    \"color\": \"#{it.Color.R:X2}{it.Color.G:X2}{it.Color.B:X2}\"");

                sb.Append("  }");
                if (i < _itemDefsList.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("]");

            File.WriteAllText(path, sb.ToString());
            MessageBox.Show($"Saved {path}", "Saved items.json", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- Zone Config Tab ---
        private void LoadZoneConfigToUi(ZoneConfig cfg)
        {
            _tbDarkMax.Value = Math.Max(0, Math.Min(30, (int)(cfg.DarkMaxBrightness * 100)));
            _lblDarkMaxVal.Text = $"{_tbDarkMax.Value}%";
            _nudMaxViewDepth.Value = (decimal)cfg.MaxViewDepth;
            _nudMinViewDepth.Value = (decimal)cfg.MinViewDepth;
            _chkInvertDark.Checked = cfg.InvertColorsInDarkness;
            _nudInnerRadius.Value = (decimal)cfg.SconceInnerRadius;
            _nudOuterRadius.Value = (decimal)cfg.SconceOuterRadius;
            _lanternColor = cfg.LanternColor;
            _pnlLanternColor.BackColor = cfg.LanternColor;
            _floorColor = cfg.FloorColor;
            _pnlFloorColor.BackColor = cfg.FloorColor;
            _ceilingColor = cfg.CeilingColor;
            _pnlCeilingColor.BackColor = cfg.CeilingColor;
            _chkShowFloorGrid.Checked = cfg.ShowFloorGrid;
        }

        private void SaveZoneConfig()
        {
            if (_currentLevel == null) return;
            var cfg = _currentLevel.Config;
            cfg.DarkMaxBrightness = _tbDarkMax.Value / 100f;
            cfg.MaxViewDepth = (float)_nudMaxViewDepth.Value;
            cfg.MinViewDepth = (float)_nudMinViewDepth.Value;
            cfg.InvertColorsInDarkness = _chkInvertDark.Checked;
            cfg.SconceInnerRadius = (double)_nudInnerRadius.Value;
            cfg.SconceOuterRadius = (double)_nudOuterRadius.Value;
            cfg.LanternColor = _lanternColor;
            cfg.FloorColor = _floorColor;
            cfg.CeilingColor = _ceilingColor;
            cfg.ShowFloorGrid = _chkShowFloorGrid.Checked;

            MessageBox.Show("Zone lighting profile updated in memory.", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveCurrentLevelAndProject()
        {
            if (_currentLevel == null || string.IsNullOrEmpty(_projectDir)) return;

            string dId = _cbDungeons.SelectedItem?.ToString() ?? "dungeon_01";
            string levelDir = Path.Combine(_projectDir, "dungeons", dId, $"level_{_currentLevel.LevelNumber}");
            if (!Directory.Exists(levelDir)) Directory.CreateDirectory(levelDir);

            // 1. Save map.txt using case-sensitive project legend
            var mapSb = new StringBuilder();
            for (int y = 0; y < _currentLevel.Height; y++)
            {
                for (int x = 0; x < _currentLevel.Width; x++)
                {
                    var t = _currentLevel.Tiles[y, x];
                    char c = '.';
                    if (t.ArchetypeKey.HasValue && _project.Legend.Archetypes.ContainsKey(t.ArchetypeKey.Value))
                    {
                        c = t.ArchetypeKey.Value;
                    }
                    else if (t.CenterOccupant is LargeObjectInstance lo)
                    {
                        c = _project.Legend.FindKeyFor(lo.ObjectKind, 'T');
                    }
                    else if (t.Type == TileType.Wall)
                    {
                        c = _project.Legend.FindKeyFor("wall", '#');
                    }
                    else if (t.Type == TileType.Spawn)
                    {
                        c = _project.Legend.FindKeyFor("spawn", '@');
                    }
                    else if (t.Type == TileType.StairsUp)
                    {
                        c = _project.Legend.FindKeyFor("stairs_up", 'S');
                    }
                    else if (t.Type == TileType.StairsDown)
                    {
                        c = _project.Legend.FindKeyFor("stairs_down", 's');
                    }
                    else if (t.Type == TileType.Door)
                    {
                        c = _project.Legend.FindKeyFor("door", 'D');
                    }
                    else if (t.Type == TileType.VoidOrPit)
                    {
                        c = _project.Legend.FindKeyFor("void", '~');
                    }
                    else
                    {
                        c = _project.Legend.FindKeyFor("floor", '.');
                    }
                    mapSb.Append(c);
                }
                mapSb.AppendLine();
            }
            File.WriteAllText(Path.Combine(levelDir, "map.txt"), mapSb.ToString());

            // 2. Save items.txt
            var itemsSb = new StringBuilder();
            itemsSb.AppendLine("# Items Map");
            itemsSb.AppendLine("# [x, y, subSquare, count] itemID");
            for (int y = 0; y < _currentLevel.Height; y++)
            {
                for (int x = 0; x < _currentLevel.Width; x++)
                {
                    var t = _currentLevel.Tiles[y, x];
                    for (int s = 0; s < 4; s++)
                    {
                        var it = t.Slots[s];
                        if (it != null)
                        {
                            itemsSb.Append($"[{x}, {y}, {s + 1}, {it.Count}] {it.Definition.Id}");
                            if (it.IsTrapped && !string.IsNullOrEmpty(it.TrapType) && it.Definition.DefaultTrapped != it.TrapType)
                                itemsSb.Append($" {{ \"trapped\": \"{it.TrapType}\" }}");
                            itemsSb.AppendLine();
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(levelDir, "items.txt"), itemsSb.ToString());

            // 3. Save monsters.txt
            var monstersSb = new StringBuilder();
            monstersSb.AppendLine("# Monsters Map");
            for (int y = 0; y < _currentLevel.Height; y++)
            {
                for (int x = 0; x < _currentLevel.Width; x++)
                {
                    var t = _currentLevel.Tiles[y, x];
                    if (t.CenterOccupant is MonsterInstance m)
                    {
                        string facing = "S";
                        double deg = m.FacingAngle * 180.0 / Math.PI;
                        if (Math.Abs(deg - 270) < 1.0) facing = "N";
                        else if (Math.Abs(deg - 90) < 1.0) facing = "S";
                        else if (Math.Abs(deg - 0) < 1.0) facing = "E";
                        else if (Math.Abs(deg - 180) < 1.0) facing = "W";

                        var metaSb = new StringBuilder();
                        bool hasMeta = m.IsUnique || !m.DropArchetypeLoot || !m.DropZoneLoot || !string.IsNullOrEmpty(m.CustomLootTable) || m.GuaranteedDrops.Count > 0 || m.MovementMode != EntityMovementMode.Static;
                        if (hasMeta)
                        {
                            var fields = new List<string>();
                            if (m.IsUnique) fields.Add("\"unique\": true");
                            if (!string.IsNullOrEmpty(m.UniqueTitle)) fields.Add($"\"title\": \"{m.UniqueTitle.Replace("\"", "\\\"")}\"");
                            if (!m.DropArchetypeLoot) fields.Add("\"archetypeLoot\": false");
                            if (!m.DropZoneLoot) fields.Add("\"zoneLoot\": false");
                            if (!string.IsNullOrEmpty(m.CustomLootTable)) fields.Add($"\"customLoot\": \"{m.CustomLootTable.Replace("\"", "\\\"")}\"");
                            if (m.GuaranteedDrops.Count > 0)
                            {
                                var gItems = string.Join(", ", m.GuaranteedDrops.Select(g => $"\"{g}\""));
                                fields.Add($"\"guaranteed\": [{gItems}]");
                            }

                            if (m.MovementMode != EntityMovementMode.Static)
                            {
                                fields.Add($"\"moveMode\": \"{m.MovementMode}\"");
                                if (m.MovementMode == EntityMovementMode.PatrolRoute)
                                {
                                    if (m.PatrolWaypoints.Count > 0)
                                    {
                                        var pts = string.Join(", ", m.PatrolWaypoints.Select(p => $"[{p.X}, {p.Y}]"));
                                        fields.Add($"\"waypoints\": [{pts}]");
                                    }
                                    fields.Add($"\"circuit\": \"{m.CircuitStyle}\"");
                                    fields.Add($"\"waypointPause\": {m.WaypointPauseSeconds:0.##}");
                                }
                                else if (m.MovementMode == EntityMovementMode.Wander)
                                {
                                    if (m.WanderAreaTiles.Count > 0)
                                    {
                                        var wpts = string.Join(", ", m.WanderAreaTiles.Select(p => $"[{p.X}, {p.Y}]"));
                                        fields.Add($"\"wanderTiles\": [{wpts}]");
                                    }
                                    else
                                    {
                                        fields.Add($"\"wanderRadius\": {m.WanderRadius}");
                                    }
                                    fields.Add($"\"wanderInterval\": {m.WanderIntervalSeconds:0.##}");
                                    fields.Add($"\"maxChase\": {m.MaxChaseDistance}");
                                    if (!m.ReturnToPreCombatSquare) fields.Add("\"returnPreCombat\": false");
                                }
                            }

                            metaSb.Append(" {" + string.Join(", ", fields) + "}");
                        }

                        monstersSb.AppendLine($"[{x}, {y}, {facing}, {m.Level}] {m.Id}{metaSb}");
                    }
                }
            }
            File.WriteAllText(Path.Combine(levelDir, "monsters.txt"), monstersSb.ToString());

            // Save loot_tables.json
            _project.LootTables.SaveToFile(Path.Combine(_projectDir, "loot_tables.json"));

            // Save key legend.json
            _project.Legend.SaveToFile(Path.Combine(_projectDir, "key legend.json"));

            // 4. Save transitions.json
            var transList = new List<TransitionObject>();
            for (int y = 0; y < _currentLevel.Height; y++)
            {
                for (int x = 0; x < _currentLevel.Width; x++)
                {
                    var tr = _currentLevel.Tiles[y, x].Transition;
                    if (tr != null) transList.Add(tr);
                }
            }

            if (transList.Count > 0)
            {
                var transSb = new StringBuilder();
                transSb.AppendLine("[");
                for (int i = 0; i < transList.Count; i++)
                {
                    var tr = transList[i];
                    transSb.AppendLine("  {");
                    transSb.AppendLine($"    \"x\": {tr.TileX},");
                    transSb.AppendLine($"    \"y\": {tr.TileY},");
                    transSb.AppendLine($"    \"type\": \"{tr.Kind}\",");
                    if (!string.IsNullOrEmpty(tr.TargetDungeon)) transSb.AppendLine($"    \"targetDungeon\": \"{tr.TargetDungeon}\",");
                    if (tr.TargetLevel != -1) transSb.AppendLine($"    \"targetLevel\": {tr.TargetLevel},");
                    if (tr.TargetX >= 0) transSb.AppendLine($"    \"targetX\": {tr.TargetX},");
                    if (tr.TargetY >= 0) transSb.AppendLine($"    \"targetY\": {tr.TargetY},");
                    if (!string.IsNullOrEmpty(tr.TargetFacing) && tr.TargetFacing != "Keep") transSb.AppendLine($"    \"targetFacing\": \"{tr.TargetFacing}\",");
                    if (tr.IsOpen) transSb.AppendLine("    \"isOpen\": true,");
                    if (tr.IsLocked) transSb.AppendLine("    \"isLocked\": true,");
                    if (!string.IsNullOrEmpty(tr.RequiredKey)) transSb.AppendLine($"    \"requiredKey\": \"{tr.RequiredKey}\",");
                    if (tr.IsTrapped)
                    {
                        transSb.AppendLine("    \"isTrapped\": true,");
                        transSb.AppendLine($"    \"trapType\": \"{tr.TrapType}\",");
                        transSb.AppendLine($"    \"trapDamage\": {tr.TrapDamage},");
                        if (tr.TrapIsRecurring) transSb.AppendLine("    \"trapIsRecurring\": true,");
                        if (tr.TrapCharges != 1) transSb.AppendLine($"    \"trapCharges\": {tr.TrapCharges},");
                        if (Math.Abs(tr.TrapJiggleChance - 0.33) > 0.001) transSb.AppendLine($"    \"trapJiggleChance\": {tr.TrapJiggleChance},");
                        if (Math.Abs(tr.TrapOpenChance - 1.0) > 0.001) transSb.AppendLine($"    \"trapOpenChance\": {tr.TrapOpenChance},");
                    }
                    if (tr.AutoTransitionOnStep) transSb.AppendLine("    \"autoTransition\": true,");
                    if (!string.IsNullOrEmpty(tr.OpenMessage)) transSb.AppendLine($"    \"openMessage\": \"{tr.OpenMessage}\",");
                    if (!string.IsNullOrEmpty(tr.LockedMessage)) transSb.AppendLine($"    \"lockedMessage\": \"{tr.LockedMessage}\",");
                    if (!string.IsNullOrEmpty(tr.TransitionMessage)) transSb.AppendLine($"    \"transitionMessage\": \"{tr.TransitionMessage}\",");
                    transSb.AppendLine($"    \"promptText\": \"{tr.InteractionPrompt}\"");
                    transSb.Append("  }");
                    if (i < transList.Count - 1) transSb.Append(",");
                    transSb.AppendLine();
                }
                transSb.AppendLine("]");
                File.WriteAllText(Path.Combine(levelDir, "transitions.json"), transSb.ToString());
            }

            // 5. Save key legend.json
            if (_project != null && !string.IsNullOrEmpty(_projectDir))
            {
                string keyLegendPath = Path.Combine(_projectDir, "key legend.json");
                _project.Legend.SaveToFile(keyLegendPath);
            }

            // 5. Save project.json
            if (_project != null && !string.IsNullOrEmpty(_projectDir))
            {
                SaveProjectSettingsJson(showPrompt: false);
            }

            SaveItemsJson();
            _projectExplorer?.Populate(_project, _projectDir);
            MessageBox.Show($"Level, items, project settings, and key legend saved successfully!", "Saved Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- Project Settings & Key Legend Tab ---
        private void BuildTabProjectSettings()
        {
            var tab = new TabPage("Project Settings & Key Legend");
            _tabs.TabPages.Add(tab);

            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            tab.Controls.Add(pnl);

            // --- Section 1: Project Metadata Configuration ---
            var grpMeta = new GroupBox
            {
                Text = "Project Configuration (project.json)",
                Left = 20, Top = 15, Width = 960, Height = 135,
                Font = new Font(FontFamily.GenericSansSerif, 9.5f, FontStyle.Bold),
                ForeColor = Color.DarkSlateBlue
            };
            pnl.Controls.Add(grpMeta);

            var lblFolderTitle = new Label { Text = "Project Folder:", Left = 15, Top = 28, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular), ForeColor = Color.Black };
            grpMeta.Controls.Add(lblFolderTitle);

            _lblSettingsFolder = new Label
            {
                Left = 115, Top = 28, Width = 680, AutoSize = false,
                Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Text = "(No project loaded)"
            };
            grpMeta.Controls.Add(_lblSettingsFolder);

            var btnOpenExplorer = new Button
            {
                Text = "Open Folder",
                Left = 810, Top = 23, Width = 130, Height = 27,
                Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular),
                ForeColor = Color.Black
            };
            btnOpenExplorer.Click += (s, e) => OpenProjectInExplorer();
            grpMeta.Controls.Add(btnOpenExplorer);

            var lblTitle = new Label { Text = "Game Title:", Left = 15, Top = 65, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular), ForeColor = Color.Black };
            grpMeta.Controls.Add(lblTitle);

            _txtSettingsTitle = new TextBox { Left = 115, Top = 62, Width = 260, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular) };
            grpMeta.Controls.Add(_txtSettingsTitle);

            var lblStartDungeon = new Label { Text = "Start Dungeon:", Left = 395, Top = 65, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular), ForeColor = Color.Black };
            grpMeta.Controls.Add(lblStartDungeon);

            _cbSettingsStartDungeon = new ComboBox { Left = 495, Top = 62, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular) };
            grpMeta.Controls.Add(_cbSettingsStartDungeon);

            var lblStartLevel = new Label { Text = "Start Level:", Left = 640, Top = 65, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular), ForeColor = Color.Black };
            grpMeta.Controls.Add(lblStartLevel);

            _nudSettingsStartLevel = new NumericUpDown { Left = 720, Top = 62, Width = 65, Minimum = 1, Maximum = 99, Value = 1, Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular) };
            grpMeta.Controls.Add(_nudSettingsStartLevel);

            var btnSaveSettings = new Button
            {
                Text = "Save Settings",
                Left = 810, Top = 60, Width = 130, Height = 28,
                BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White,
                Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold)
            };
            btnSaveSettings.Click += (s, e) => SaveProjectSettingsJson(showPrompt: true);
            grpMeta.Controls.Add(btnSaveSettings);

            var lblMetaHint = new Label
            {
                Text = "Defines the game display title and the starting dungeon/level loaded when launching GridLocked.",
                Left = 15, Top = 98, AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            grpMeta.Controls.Add(lblMetaHint);

            // --- Section 2: ASCII Map Key Legend ---
            var grpLegend = new GroupBox
            {
                Text = "ASCII Map Key Legend (key legend.json)",
                Left = 20, Top = 165, Width = 960, Height = 490,
                Font = new Font(FontFamily.GenericSansSerif, 9.5f, FontStyle.Bold),
                ForeColor = Color.DarkSlateBlue
            };
            pnl.Controls.Add(grpLegend);

            var lblLegendDesc = new Label
            {
                Text = "Maps single ASCII characters in map.txt to dungeon elements, fixtures, or custom vector objects.\r\n" +
                       "Engine default keys (' ', '.', '#', '@', 'S', 's', 'D', '~', '<', '>') apply automatically.\r\n" +
                       "If 'key legend.json' exists in the project folder, it overrides these defaults and defines custom keys.",
                Left = 15, Top = 25, AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            grpLegend.Controls.Add(lblLegendDesc);

            _dgvLegend = new DataGridView
            {
                Left = 15, Top = 78, Width = 740, Height = 390,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular),
                ForeColor = Color.Black
            };
            _dgvLegend.Columns.Add("Key", "Key");
            _dgvLegend.Columns["Key"].Width = 45;
            _dgvLegend.Columns.Add("Id", "ID");
            _dgvLegend.Columns["Id"].Width = 85;
            _dgvLegend.Columns.Add("Name", "Name");
            _dgvLegend.Columns["Name"].Width = 105;
            _dgvLegend.Columns.Add("BaseType", "Type");
            _dgvLegend.Columns["BaseType"].Width = 65;
            _dgvLegend.Columns.Add("Walkable", "Walkable");
            _dgvLegend.Columns["Walkable"].Width = 60;
            _dgvLegend.Columns.Add("Vision", "Blocks Vis");
            _dgvLegend.Columns["Vision"].Width = 70;
            _dgvLegend.Columns.Add("Breakable", "Breakable");
            _dgvLegend.Columns["Breakable"].Width = 75;
            _dgvLegend.Columns.Add("Moveable", "Pushable");
            _dgvLegend.Columns["Moveable"].Width = 65;
            _dgvLegend.Columns.Add("Openable", "Openable");
            _dgvLegend.Columns["Openable"].Width = 65;
            _dgvLegend.Columns.Add("Hazard", "Hazard");
            _dgvLegend.Columns["Hazard"].Width = 70;
            _dgvLegend.Columns.Add("Prop", "Prop Kind");
            _dgvLegend.Columns["Prop"].Width = 80;
            _dgvLegend.CellDoubleClick += (s, e) => AddOrEditLegendKey(isNew: false);
            grpLegend.Controls.Add(_dgvLegend);

            int btnX = 770;
            int btnY = 78;

            var btnAdd = new Button { Text = "+ Add Archetype Key...", Left = btnX, Top = btnY, Width = 170, Height = 30, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular) };
            btnAdd.Click += (s, e) => AddOrEditLegendKey(isNew: true);
            grpLegend.Controls.Add(btnAdd);

            var btnEdit = new Button { Text = "✎ Edit Selected Key...", Left = btnX, Top = btnY + 36, Width = 170, Height = 30, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular), ForeColor = Color.Black };
            btnEdit.Click += (s, e) => AddOrEditLegendKey(isNew: false);
            grpLegend.Controls.Add(btnEdit);

            var btnRemove = new Button { Text = "✕ Remove Selected Key", Left = btnX, Top = btnY + 72, Width = 170, Height = 30, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular), ForeColor = Color.Black };
            btnRemove.Click += (s, e) => RemoveSelectedLegendKey();
            grpLegend.Controls.Add(btnRemove);

            var btnReset = new Button { Text = "Reset to Defaults", Left = btnX, Top = btnY + 108, Width = 170, Height = 30, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular), ForeColor = Color.Black };
            btnReset.Click += (s, e) => ResetLegendToDefaults();
            grpLegend.Controls.Add(btnReset);

            var btnSaveLegend = new Button { Text = "Save key legend.json", Left = btnX, Top = btnY + 150, Width = 170, Height = 34, BackColor = Color.FromArgb(46, 139, 87), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold) };
            btnSaveLegend.Click += (s, e) => SaveKeyLegendJson();
            grpLegend.Controls.Add(btnSaveLegend);
        }

        private void PopulateProjectSettings()
        {
            if (_project == null)
            {
                if (_lblSettingsFolder != null) _lblSettingsFolder.Text = "(No project loaded)";
                if (_txtSettingsTitle != null) _txtSettingsTitle.Text = "";
                if (_cbSettingsStartDungeon != null) _cbSettingsStartDungeon.Items.Clear();
                if (_nudSettingsStartLevel != null) _nudSettingsStartLevel.Value = 1;
                if (_dgvLegend != null) _dgvLegend.Rows.Clear();
                return;
            }

            if (_lblSettingsFolder != null) _lblSettingsFolder.Text = _projectDir ?? "(Unknown)";
            if (_txtSettingsTitle != null) _txtSettingsTitle.Text = _project.Title ?? "";

            if (_cbSettingsStartDungeon != null)
            {
                _cbSettingsStartDungeon.Items.Clear();
                foreach (var d in _project.Dungeons.Keys)
                    _cbSettingsStartDungeon.Items.Add(d);

                if (!string.IsNullOrEmpty(_project.StartDungeonId) && _cbSettingsStartDungeon.Items.Contains(_project.StartDungeonId))
                    _cbSettingsStartDungeon.SelectedItem = _project.StartDungeonId;
                else if (_cbSettingsStartDungeon.Items.Count > 0)
                    _cbSettingsStartDungeon.SelectedIndex = 0;
            }

            if (_nudSettingsStartLevel != null)
            {
                int lvl = Math.Max(1, Math.Min(99, _project.StartLevelNumber));
                _nudSettingsStartLevel.Value = lvl;
            }

            PopulateLegendGrid();
        }

        private void SaveProjectSettingsJson(bool showPrompt = true)
        {
            if (_project == null || string.IsNullOrEmpty(_projectDir)) return;

            if (_txtSettingsTitle != null)
                _project.Title = _txtSettingsTitle.Text.Trim();
            if (_cbSettingsStartDungeon != null && _cbSettingsStartDungeon.SelectedItem != null)
                _project.StartDungeonId = _cbSettingsStartDungeon.SelectedItem.ToString();
            if (_nudSettingsStartLevel != null)
                _project.StartLevelNumber = (int)_nudSettingsStartLevel.Value;

            Text = $"GridLocked Studio - {_project.Title}";

            string projJsonPath = Path.Combine(_projectDir, "project.json");
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"title\": \"{_project.Title.Replace("\"", "\\\"")}\",");
            sb.AppendLine($"  \"startDungeon\": \"{_project.StartDungeonId}\",");
            sb.AppendLine($"  \"startLevel\": {_project.StartLevelNumber}");
            sb.AppendLine("}");
            File.WriteAllText(projJsonPath, sb.ToString());

            if (showPrompt)
            {
                MessageBox.Show($"Project settings saved to:\n{projJsonPath}", "Saved Project Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PopulateLegendGrid()
        {
            if (_dgvLegend == null || _project == null) return;
            _dgvLegend.Rows.Clear();

            foreach (var kvp in _project.Legend.Archetypes.OrderBy(k => k.Key))
            {
                var a = kvp.Value;
                string displayKey = a.Key == ' ' ? "[space]" : a.Key.ToString();
                string walkable = (a.CanWalkThru && a.CanWalkOver) ? "Yes" : (a.CanWalkThru ? "Thru" : (a.CanWalkOver ? "Over" : "No"));
                string vision = a.BlocksVision ? "Blocks" : "Clear";
                string breakable = a.IsBreakable ? $"Yes ({a.BreakHP} HP)" : "No";
                string moveable = a.IsMoveable ? "Yes (Push)" : "No";
                string openable = a.CanBeOpened ? (a.StartsOpen ? "Open" : "Closed") : "No";
                string hazard = a.IsHazardous ? $"Yes ({a.HazardDamage})" : "No";
                string prop = string.IsNullOrEmpty(a.ObjectKind) ? "-" : a.ObjectKind;

                _dgvLegend.Rows.Add(
                    displayKey,
                    a.Id,
                    a.Name,
                    a.BaseType.ToString(),
                    walkable,
                    vision,
                    breakable,
                    moveable,
                    openable,
                    hazard,
                    prop
                );
            }
        }

        private void AddOrEditLegendKey(bool isNew = true)
        {
            if (_project == null) return;

            TileArchetype existingArch = null;
            if (!isNew && _dgvLegend.CurrentRow != null)
            {
                string keyStr = _dgvLegend.CurrentRow.Cells["Key"].Value?.ToString();
                if (!string.IsNullOrEmpty(keyStr))
                {
                    char keyChar = keyStr == "[space]" ? ' ' : keyStr[0];
                    existingArch = _project.Legend.GetArchetype(keyChar);
                }
            }

            using (var prompt = new Form())
            {
                prompt.Width = 470;
                prompt.Height = 520;
                prompt.Text = isNew ? "Add Archetype Legend Key" : $"Edit Archetype Legend Key ('{(existingArch?.Key == ' ' ? "[space]" : existingArch?.Key.ToString())}')";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular);

                // Row 1: Key
                var lblK = new Label { Left = 20, Top = 18, Text = "ASCII Character:", AutoSize = true };
                var txtK = new TextBox { Left = 135, Top = 15, Width = 50, MaxLength = 1, Text = isNew ? "" : (existingArch?.Key == ' ' ? " " : existingArch?.Key.ToString()), Enabled = isNew };
                var lblKHint = new Label { Left = 195, Top = 18, Text = "(Character used in map.txt)", AutoSize = true, ForeColor = Color.Gray };

                // Row 2: ID
                var lblId = new Label { Left = 20, Top = 48, Text = "Archetype ID:", AutoSize = true };
                var txtId = new TextBox { Left = 135, Top = 45, Width = 290, Text = existingArch?.Id ?? "" };

                // Row 3: Name
                var lblName = new Label { Left = 20, Top = 78, Text = "Display Name:", AutoSize = true };
                var txtName = new TextBox { Left = 135, Top = 75, Width = 290, Text = existingArch?.Name ?? "" };

                // Row 4: Base Type
                var lblType = new Label { Left = 20, Top = 108, Text = "Base Tile Type:", AutoSize = true };
                var cbBaseType = new ComboBox { Left = 135, Top = 105, Width = 290, DropDownStyle = ComboBoxStyle.DropDownList };
                cbBaseType.Items.AddRange(new object[] { "Floor", "Wall", "Door", "StairsUp", "StairsDown", "VoidOrPit" });
                cbBaseType.SelectedItem = existingArch != null ? existingArch.BaseType.ToString() : "Floor";

                // GroupBox: Physics & Interaction
                var grpPhysics = new GroupBox { Text = "Traversal, Destructibility & Interaction Flags", Left = 20, Top = 140, Width = 405, Height = 210, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold) };

                var chkWalk = new CheckBox { Text = "Can Walk Thru / Over", Left = 15, Top = 25, AutoSize = true, Checked = existingArch?.CanWalkThru ?? true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(chkWalk);

                var chkVision = new CheckBox { Text = "Blocks Vision (Opaque)", Left = 210, Top = 25, AutoSize = true, Checked = existingArch?.BlocksVision ?? false, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(chkVision);

                var chkBreak = new CheckBox { Text = "Breakable / Destructible", Left = 15, Top = 58, AutoSize = true, Checked = existingArch?.IsBreakable ?? false, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(chkBreak);

                var lblHP = new Label { Text = "HP:", Left = 210, Top = 59, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(lblHP);

                var nudHP = new NumericUpDown { Left = 240, Top = 56, Width = 65, Minimum = 1, Maximum = 1000, Value = Math.Max(1, Math.Min(1000, existingArch?.BreakHP ?? 50)), Enabled = chkBreak.Checked, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                chkBreak.CheckedChanged += (s, e) => nudHP.Enabled = chkBreak.Checked;
                grpPhysics.Controls.Add(nudHP);

                var chkMove = new CheckBox { Text = "Moveable / Pushable (Boulder)", Left = 15, Top = 92, AutoSize = true, Checked = existingArch?.IsMoveable ?? false, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(chkMove);

                var chkOpen = new CheckBox { Text = "Can Be Opened (Gate/Door)", Left = 15, Top = 126, AutoSize = true, Checked = existingArch?.CanBeOpened ?? false, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(chkOpen);

                var chkStartsOpen = new CheckBox { Text = "Starts Open", Left = 210, Top = 126, AutoSize = true, Checked = existingArch?.StartsOpen ?? false, Enabled = chkOpen.Checked, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                chkOpen.CheckedChanged += (s, e) => chkStartsOpen.Enabled = chkOpen.Checked;
                grpPhysics.Controls.Add(chkStartsOpen);

                var chkHazard = new CheckBox { Text = "Hazardous (Damages on step)", Left = 15, Top = 160, AutoSize = true, Checked = existingArch?.IsHazardous ?? false, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(chkHazard);

                var lblDmg = new Label { Text = "Dmg:", Left = 210, Top = 161, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                grpPhysics.Controls.Add(lblDmg);

                var nudDmg = new NumericUpDown { Left = 250, Top = 158, Width = 60, Minimum = 0, Maximum = 999, Value = Math.Max(0, Math.Min(999, existingArch?.HazardDamage ?? 0)), Enabled = chkHazard.Checked, Font = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular) };
                chkHazard.CheckedChanged += (s, e) => nudDmg.Enabled = chkHazard.Checked;
                grpPhysics.Controls.Add(nudDmg);

                cbBaseType.SelectedIndexChanged += (s, e) =>
                {
                    if (isNew && Enum.TryParse<TileType>(cbBaseType.SelectedItem?.ToString(), out var selTt))
                    {
                        var temp = new TileArchetype('?', selTt.ToString().ToLowerInvariant(), selTt.ToString(), selTt);
                        chkWalk.Checked = temp.CanWalkThru;
                        chkVision.Checked = temp.BlocksVision;
                        chkBreak.Checked = temp.IsBreakable;
                        nudHP.Value = Math.Max(1, Math.Min(1000, temp.BreakHP));
                        chkMove.Checked = temp.IsMoveable;
                        chkOpen.Checked = temp.CanBeOpened;
                        chkStartsOpen.Checked = temp.StartsOpen;
                        chkHazard.Checked = temp.IsHazardous;
                        nudDmg.Value = Math.Max(0, Math.Min(999, temp.HazardDamage));
                        if (string.IsNullOrEmpty(txtId.Text)) txtId.Text = selTt.ToString().ToLowerInvariant();
                        if (string.IsNullOrEmpty(txtName.Text)) txtName.Text = selTt.ToString();
                    }
                };

                // Row: Prop Kind
                var lblProp = new Label { Left = 20, Top = 365, Text = "3D Prop Kind:", AutoSize = true };
                var cbProp = new ComboBox { Left = 135, Top = 362, Width = 290 };
                cbProp.Items.AddRange(new object[] { "(None)", "stone_pillar", "boulder", "altar_gold", "tree_oak", "pit", "chest_closed", "cracked_wall", "gate", "lava" });
                string curKind = existingArch?.ObjectKind ?? "";
                if (string.IsNullOrEmpty(curKind)) cbProp.SelectedIndex = 0;
                else
                {
                    if (!cbProp.Items.Contains(curKind)) cbProp.Items.Add(curKind);
                    cbProp.SelectedItem = curKind;
                }

                // Buttons
                var btnOk = new Button { Text = "Save Archetype", Left = 195, Top = 420, Width = 130, Height = 32, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Left = 335, Top = 420, Width = 90, Height = 32, DialogResult = DialogResult.Cancel };

                prompt.Controls.AddRange(new Control[] { lblK, txtK, lblKHint, lblId, txtId, lblName, txtName, lblType, cbBaseType, grpPhysics, lblProp, cbProp, btnOk, btnCancel });
                prompt.AcceptButton = btnOk;
                prompt.CancelButton = btnCancel;

                if (prompt.ShowDialog(this) == DialogResult.OK)
                {
                    char key;
                    if (isNew)
                    {
                        string kStr = txtK.Text;
                        if (string.IsNullOrEmpty(kStr))
                        {
                            MessageBox.Show("Please specify a valid single ASCII character for the key.", "Invalid Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        key = kStr[0];
                    }
                    else
                    {
                        key = existingArch != null ? existingArch.Key : (txtK.Text.Length > 0 ? txtK.Text[0] : ' ');
                    }

                    string id = string.IsNullOrWhiteSpace(txtId.Text) ? "custom_archetype" : txtId.Text.Trim();
                    string name = string.IsNullOrWhiteSpace(txtName.Text) ? id : txtName.Text.Trim();
                    Enum.TryParse<TileType>(cbBaseType.SelectedItem?.ToString(), out var bType);
                    string propKind = cbProp.Text.Trim();
                    if (propKind == "(None)") propKind = "";

                    var arch = new TileArchetype
                    {
                        Key = key,
                        Id = id,
                        Name = name,
                        BaseType = bType,
                        CanWalkThru = chkWalk.Checked,
                        CanWalkOver = chkWalk.Checked,
                        BlocksVision = chkVision.Checked,
                        IsBreakable = chkBreak.Checked,
                        BreakHP = (int)nudHP.Value,
                        IsMoveable = chkMove.Checked,
                        CanBeOpened = chkOpen.Checked,
                        StartsOpen = chkStartsOpen.Checked,
                        IsHazardous = chkHazard.Checked,
                        HazardDamage = (int)nudDmg.Value,
                        ObjectKind = propKind
                    };

                    _project.Legend.SetArchetype(arch);
                    PopulateLegendGrid();
                    PopulateToolArchetypes();
                    PopulateToolProps();
                    _painter.Invalidate();
                }
            }
        }

        private void RemoveSelectedLegendKey()
        {
            if (_dgvLegend.CurrentRow == null || _project == null) return;
            string keyStr = _dgvLegend.CurrentRow.Cells["Key"].Value?.ToString();
            if (string.IsNullOrEmpty(keyStr)) return;

            char c = keyStr == "[space]" ? ' ' : keyStr[0];
            _project.Legend.Remove(c);
            PopulateLegendGrid();
            PopulateToolArchetypes();
            PopulateToolProps();
            _painter.Invalidate();
        }

        private void ResetLegendToDefaults()
        {
            if (_project == null) return;
            var res = MessageBox.Show("Reset legend mappings back to engine defaults?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                _project.Legend.SetDefaults();
                PopulateLegendGrid();
                PopulateToolArchetypes();
                PopulateToolProps();
                _painter.Invalidate();
            }
        }

        private void SaveKeyLegendJson()
        {
            if (string.IsNullOrEmpty(_projectDir) || _project == null) return;
            string path = Path.Combine(_projectDir, "key legend.json");
            _project.Legend.SaveToFile(path);
            MessageBox.Show($"Saved key legend to:\n{path}", "Saved key legend.json", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LaunchPlaytest()
        {
            if (_project == null || string.IsNullOrEmpty(_projectDir)) return;

            SaveCurrentLevelAndProject();

            string exePath = @"D:\Documents\projects\GridLocked\bin\Debug\GridLocked.exe";
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"\"{_projectDir}\"",
                    WorkingDirectory = Path.GetDirectoryName(exePath)
                });
            }
            else
            {
                MessageBox.Show($"Could not find {exePath}. Please build GridLocked first.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExportProjectToGlp()
        {
            if (_project == null || string.IsNullOrEmpty(_projectDir))
            {
                MessageBox.Show("No project is currently loaded to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveCurrentLevelAndProject();

            string modulesDir = @"D:\Documents\projects\GridLocked\bin\Debug\modules";
            if (!Directory.Exists(modulesDir))
            {
                try { Directory.CreateDirectory(modulesDir); } catch { }
            }

            string safeName = string.Join("_", _project.Title.Split(Path.GetInvalidFileNameChars())).Trim();
            if (string.IsNullOrEmpty(safeName)) safeName = Path.GetFileName(_projectDir);

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Export GridLocked Module Package (.glp)";
                sfd.Filter = "GridLocked Module Package (*.glp)|*.glp";
                sfd.InitialDirectory = modulesDir;
                sfd.FileName = $"{safeName}.glp";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        GlpPackage.CreateFromDirectory(_projectDir, sfd.FileName);
                        var res = MessageBox.Show(
                            $"Successfully packaged module to:\n{sfd.FileName}\n\nWould you like to test play this module in GridLocked now?",
                            "Export Succeeded",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (res == DialogResult.Yes)
                        {
                            string exePath = @"D:\Documents\projects\GridLocked\bin\Debug\GridLocked.exe";
                            if (File.Exists(exePath))
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = exePath,
                                    Arguments = $"\"{sfd.FileName}\"",
                                    WorkingDirectory = Path.GetDirectoryName(exePath)
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export module: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
