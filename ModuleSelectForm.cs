using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GridLocked
{
    public sealed class ModuleSelectForm : Form
    {
        private ListBox _lbModules;
        private Label _lblTitle;
        private Label _lblFile;
        private Label _lblType;
        private Label _lblDungeons;
        private Label _lblStart;
        private Label _lblPath;
        private Button _btnPlay;
        private Panel _pnlEmpty;
        private string _modulesDir;

        public string SelectedModulePath { get; private set; }

        public ModuleSelectForm()
        {
            Text = "GridLocked - Select Adventure Module";
            Width = 760;
            Height = 490;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(28, 28, 30);
            ForeColor = Color.White;
            KeyPreview = true;

            _modulesDir = Path.Combine(Path.GetDirectoryName(typeof(ModuleSelectForm).Assembly.Location), "modules");
            if (!Directory.Exists(_modulesDir))
            {
                try { Directory.CreateDirectory(_modulesDir); } catch { }
            }

            BuildUi();
            RefreshModules();

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && _btnPlay.Enabled)
                {
                    LaunchSelected();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        private void BuildUi()
        {
            // 1. Header Banner
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(20, 20, 22),
                Padding = new Padding(15, 8, 15, 8)
            };
            Controls.Add(pnlHeader);

            var lblLogo = new Label
            {
                Text = "G R I D L O C K E D",
                Left = 15, Top = 8,
                AutoSize = true,
                Font = new Font("Consolas", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 210, 255)
            };
            pnlHeader.Controls.Add(lblLogo);

            var lblSubtitle = new Label
            {
                Text = "Modular Retro 3D RPG Engine  •  Select a game module (.glp) to play",
                Left = 17, Top = 38,
                AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Regular),
                ForeColor = Color.DarkGray
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // 2. Left Panel: Available Modules List
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 370,
                Padding = new Padding(15, 12, 10, 15)
            };
            Controls.Add(pnlLeft);

            var lblModulesHeader = new Label
            {
                Text = "Available Adventure Modules:",
                Left = 15, Top = 12,
                AutoSize = true,
                Font = new Font(FontFamily.GenericSansSerif, 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            pnlLeft.Controls.Add(lblModulesHeader);

            _lbModules = new ListBox
            {
                Left = 15, Top = 36,
                Width = 345, Height = 320,
                BackColor = Color.FromArgb(38, 38, 42),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9.5f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                ItemHeight = 22
            };
            _lbModules.SelectedIndexChanged += (s, e) => OnModuleSelected();
            _lbModules.DoubleClick += (s, e) => LaunchSelected();
            pnlLeft.Controls.Add(_lbModules);

            // 3. Right Panel: Module Details Card
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 12, 15, 15)
            };
            Controls.Add(pnlRight);
            pnlRight.BringToFront();

            var grpDetails = new GroupBox
            {
                Text = "Adventure Details",
                Left = 10, Top = 12,
                Width = 345, Height = 250,
                Font = new Font(FontFamily.GenericSansSerif, 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 190, 240)
            };
            pnlRight.Controls.Add(grpDetails);

            int curY = 28;
            _lblTitle = new Label { Text = "(Select a module)", Left = 15, Top = curY, Width = 315, AutoSize = false, Font = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Bold), ForeColor = Color.White };
            grpDetails.Controls.Add(_lblTitle);
            curY += 32;

            var lblF = new Label { Text = "Package File:", Left = 15, Top = curY, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold), ForeColor = Color.Gray };
            grpDetails.Controls.Add(lblF);
            _lblFile = new Label { Text = "-", Left = 110, Top = curY, Width = 220, AutoSize = false, ForeColor = Color.White };
            grpDetails.Controls.Add(_lblFile);
            curY += 24;

            var lblT = new Label { Text = "Format:", Left = 15, Top = curY, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold), ForeColor = Color.Gray };
            grpDetails.Controls.Add(lblT);
            _lblType = new Label { Text = "-", Left = 110, Top = curY, Width = 220, AutoSize = false, ForeColor = Color.White };
            grpDetails.Controls.Add(_lblType);
            curY += 24;

            var lblD = new Label { Text = "Dungeons:", Left = 15, Top = curY, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold), ForeColor = Color.Gray };
            grpDetails.Controls.Add(lblD);
            _lblDungeons = new Label { Text = "-", Left = 110, Top = curY, Width = 220, AutoSize = false, ForeColor = Color.White };
            grpDetails.Controls.Add(_lblDungeons);
            curY += 24;

            var lblS = new Label { Text = "Starting:", Left = 15, Top = curY, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 8.5f, FontStyle.Bold), ForeColor = Color.Gray };
            grpDetails.Controls.Add(lblS);
            _lblStart = new Label { Text = "-", Left = 110, Top = curY, Width = 220, AutoSize = false, ForeColor = Color.White };
            grpDetails.Controls.Add(_lblStart);
            curY += 28;

            _lblPath = new Label { Text = "", Left = 15, Top = curY, Width = 315, Height = 48, Font = new Font(FontFamily.GenericSansSerif, 7.5f, FontStyle.Italic), ForeColor = Color.DimGray };
            grpDetails.Controls.Add(_lblPath);

            // Action Buttons
            _btnPlay = new Button
            {
                Text = "Play Adventure",
                Left = 10, Top = grpDetails.Bottom + 12,
                Width = 345, Height = 42,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                Font = new Font(FontFamily.GenericSansSerif, 11f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnPlay.Click += (s, e) => LaunchSelected();
            pnlRight.Controls.Add(_btnPlay);

            var btnOpenFolder = new Button
            {
                Text = "Open modules/ Folder",
                Left = 10, Top = _btnPlay.Bottom + 8,
                Width = 168, Height = 30,
                BackColor = Color.FromArgb(50, 50, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOpenFolder.Click += (s, e) => OpenModulesFolder();
            pnlRight.Controls.Add(btnOpenFolder);

            var btnRefresh = new Button
            {
                Text = "Refresh",
                Left = btnOpenFolder.Right + 8, Top = _btnPlay.Bottom + 8,
                Width = 80, Height = 30,
                BackColor = Color.FromArgb(50, 50, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.Click += (s, e) => RefreshModules();
            pnlRight.Controls.Add(btnRefresh);

            var btnExit = new Button
            {
                Text = "Exit",
                Left = btnRefresh.Right + 8, Top = _btnPlay.Bottom + 8,
                Width = 81, Height = 30,
                BackColor = Color.FromArgb(50, 50, 54),
                ForeColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            btnExit.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlRight.Controls.Add(btnExit);

            // Docking order: pnlHeader full top, pnlLeft docked left, pnlRight fills remaining
            pnlRight.BringToFront();
            pnlLeft.SendToBack();
            pnlHeader.SendToBack();

            // Empty state placeholder
            _pnlEmpty = new Panel
            {
                Left = 15, Top = 36,
                Width = 345, Height = 320,
                BackColor = Color.FromArgb(34, 34, 38),
                Visible = false
            };
            var lblEmptyMsg = new Label
            {
                Text = "No modules found in 'modules/' directory.\r\n\r\n" +
                       "To play a game:\r\n" +
                       "• Place .glp packages in the modules/ folder\r\n" +
                       "• Or use GridLocked Editor to 'Export to Module (.glp)'",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular),
                ForeColor = Color.DarkGray
            };
            _pnlEmpty.Controls.Add(lblEmptyMsg);
            pnlLeft.Controls.Add(_pnlEmpty);
        }

        private void RefreshModules()
        {
            _lbModules.Items.Clear();
            var modules = GlpPackage.ScanModules(_modulesDir);

            if (modules.Count == 0)
            {
                _pnlEmpty.Visible = true;
                _pnlEmpty.BringToFront();
                _btnPlay.Enabled = false;
                _lblTitle.Text = "(No modules found)";
                _lblFile.Text = "-";
                _lblType.Text = "-";
                _lblDungeons.Text = "-";
                _lblStart.Text = "-";
                _lblPath.Text = "";
                return;
            }

            _pnlEmpty.Visible = false;
            foreach (var m in modules)
            {
                _lbModules.Items.Add(m);
            }

            if (_lbModules.Items.Count > 0)
            {
                _lbModules.SelectedIndex = 0;
            }
        }

        private void OnModuleSelected()
        {
            if (_lbModules.SelectedItem is ModuleInfo info)
            {
                _lblTitle.Text = info.Title;
                _lblFile.Text = info.FileName;
                _lblType.Text = info.IsGlpPackage ? "Packaged Binary (.glp)" : "Directory Project";
                _lblDungeons.Text = $"{info.DungeonCount} Dungeon(s)";
                _lblStart.Text = $"{info.StartDungeonId} / Level {info.StartLevelNumber}";
                _lblPath.Text = info.FilePath;
                _btnPlay.Enabled = true;
            }
            else
            {
                _btnPlay.Enabled = false;
            }
        }

        private void LaunchSelected()
        {
            if (_lbModules.SelectedItem is ModuleInfo info)
            {
                SelectedModulePath = info.FilePath;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void OpenModulesFolder()
        {
            try
            {
                if (!Directory.Exists(_modulesDir)) Directory.CreateDirectory(_modulesDir);
                Process.Start("explorer.exe", _modulesDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open modules folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
