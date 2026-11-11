using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using GridLocked;

namespace GridLockedEditor
{
    public enum ProjectNodeKind
    {
        ProjectRoot,
        ProjectSettings,
        KeyLegend,
        Dungeon,
        Level,
        Map,
        ZoneLighting,
        Transitions,
        LevelItems,
        LevelMonsters,
        ItemsSchema,
        ItemCategory,
        ItemDefinition,
        ItemClasses,
        LootTables,
        ObjectsSchema,
        Folder,
        AudioFolder,
        ImagesFolder,
        AudioFile,
        ImageFile,
        GenericFile
    }

    public sealed class ProjectNodeInfo
    {
        public ProjectNodeKind Kind { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public string DungeonId { get; set; }
        public int LevelNumber { get; set; } = -1;
        public string ItemId { get; set; }
        public string Category { get; set; }
    }

    public sealed class ProjectExplorerControl : UserControl
    {
        private TreeView _tree;
        private Panel _headerPanel;
        private Label _lblTitle;
        private ToolStripButton _btnRefresh;
        private ToolStripButton _btnCollapse;
        private ToolStripButton _btnOpenFolder;
        private ImageList _icons;
        private ContextMenuStrip _contextMenu;

        private GameProject _project;
        private string _projectDir;

        public event Action<ProjectNodeInfo> NodeActivated;
        public event Action RequestAddLevel;
        public event Action RequestAddDungeon;
        public event Action RequestRefresh;

        public ProjectExplorerControl()
        {
            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9f);
            BuildControl();
        }

        private void BuildControl()
        {
            _icons = CreateProjectImageList();

            // Header Panel (VS Solution Explorer Style)
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(243, 243, 246),
                Padding = new Padding(6, 2, 4, 2)
            };

            _lblTitle = new Label
            {
                Text = "PROJECT EXPLORER",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 70, 80),
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 140,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _headerPanel.Controls.Add(_lblTitle);

            var toolStrip = new ToolStrip
            {
                Dock = DockStyle.Right,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.Transparent,
                RenderMode = ToolStripRenderMode.System,
                AutoSize = false,
                Width = 100,
                Height = 24
            };

            _btnRefresh = new ToolStripButton("🔄") { ToolTipText = "Refresh Hierarchy (F5)" };
            _btnRefresh.Click += (s, e) => RequestRefresh?.Invoke();

            _btnCollapse = new ToolStripButton("⊟") { ToolTipText = "Collapse All" };
            _btnCollapse.Click += (s, e) => _tree?.CollapseAll();

            _btnOpenFolder = new ToolStripButton("📁") { ToolTipText = "Open Project Directory in Windows Explorer" };
            _btnOpenFolder.Click += (s, e) => OpenInExplorer(_projectDir);

            toolStrip.Items.Add(_btnRefresh);
            toolStrip.Items.Add(_btnCollapse);
            toolStrip.Items.Add(_btnOpenFolder);
            _headerPanel.Controls.Add(toolStrip);

            Controls.Add(_headerPanel);

            // Context Menu
            _contextMenu = new ContextMenuStrip();
            var mnuAddLevel = new ToolStripMenuItem("➕ Add New Level...", null, (s, e) => RequestAddLevel?.Invoke());
            var mnuAddDungeon = new ToolStripMenuItem("🏰 Add New Dungeon...", null, (s, e) => RequestAddDungeon?.Invoke());
            var mnuRefresh = new ToolStripMenuItem("🔄 Refresh", null, (s, e) => RequestRefresh?.Invoke());
            var mnuExplore = new ToolStripMenuItem("📁 Open in Windows Explorer", null, (s, e) =>
            {
                var tag = _tree?.SelectedNode?.Tag as ProjectNodeInfo;
                string p = tag?.FilePath ?? _projectDir;
                if (!string.IsNullOrEmpty(p) && File.Exists(p)) p = Path.GetDirectoryName(p);
                OpenInExplorer(p);
            });

            _contextMenu.Items.Add(mnuAddLevel);
            _contextMenu.Items.Add(mnuAddDungeon);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(mnuRefresh);
            _contextMenu.Items.Add(mnuExplore);

            // TreeView
            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                ImageList = _icons,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                HideSelection = false,
                ContextMenuStrip = _contextMenu,
                BackColor = Color.FromArgb(252, 252, 253)
            };

            _tree.NodeMouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    _tree.SelectedNode = e.Node;
                }
                else if (e.Button == MouseButtons.Left && e.Node?.Tag is ProjectNodeInfo info)
                {
                    NodeActivated?.Invoke(info);
                }
            };

            _tree.NodeMouseDoubleClick += (s, e) =>
            {
                if (e.Node?.Tag is ProjectNodeInfo info)
                {
                    NodeActivated?.Invoke(info);
                    // For files without custom tabs, open with system default editor
                    if (info.Kind == ProjectNodeKind.GenericFile || info.Kind == ProjectNodeKind.AudioFile || info.Kind == ProjectNodeKind.ImageFile)
                    {
                        if (!string.IsNullOrEmpty(info.FilePath) && File.Exists(info.FilePath))
                        {
                            try { Process.Start(info.FilePath); } catch { }
                        }
                    }
                }
            };

            Controls.Add(_tree);
            _tree.BringToFront();
        }

        public void Populate(GameProject project, string projectDir)
        {
            _project = project;
            _projectDir = projectDir;

            _tree.BeginUpdate();
            _tree.Nodes.Clear();

            if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
            {
                var emptyNode = new TreeNode("No project open") { ImageKey = "project", SelectedImageKey = "project" };
                _tree.Nodes.Add(emptyNode);
                _tree.EndUpdate();
                return;
            }

            string projTitle = project?.Title;
            if (string.IsNullOrEmpty(projTitle))
                projTitle = Path.GetFileName(projectDir);

            // Root Project Node
            var rootNode = new TreeNode($"📦 {projTitle}")
            {
                ImageKey = "project",
                SelectedImageKey = "project",
                Tag = new ProjectNodeInfo
                {
                    Kind = ProjectNodeKind.ProjectRoot,
                    Title = projTitle,
                    FilePath = projectDir
                }
            };

            // 1. Project Configuration Files
            string projJson = Path.Combine(projectDir, "project.json");
            if (File.Exists(projJson))
            {
                rootNode.Nodes.Add(new TreeNode("⚙ project.json")
                {
                    ImageKey = "json",
                    SelectedImageKey = "json",
                    Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.ProjectSettings, Title = "project.json", FilePath = projJson }
                });
            }

            string legendJson = Path.Combine(projectDir, "key legend.json");
            if (File.Exists(legendJson))
            {
                rootNode.Nodes.Add(new TreeNode("🔑 key legend.json")
                {
                    ImageKey = "map",
                    SelectedImageKey = "map",
                    Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.KeyLegend, Title = "key legend.json", FilePath = legendJson }
                });
            }

            // 2. Dungeons & Levels
            var dungeonsGroupNode = new TreeNode("🏰 Dungeons")
            {
                ImageKey = "dungeon",
                SelectedImageKey = "dungeon",
                Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.Folder, Title = "Dungeons", FilePath = Path.Combine(projectDir, "dungeons") }
            };

            if (project != null && project.Dungeons.Count > 0)
            {
                foreach (var kvp in project.Dungeons)
                {
                    string dungeonId = kvp.Key;
                    var pkg = kvp.Value;
                    string dDir = Path.Combine(projectDir, "dungeons", dungeonId);

                    var dNode = new TreeNode($"📁 {pkg.Name} ({dungeonId})")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folder_open",
                        Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.Dungeon, Title = pkg.Name, DungeonId = dungeonId, FilePath = dDir }
                    };

                    string dJson = Path.Combine(dDir, "dungeon.json");
                    if (File.Exists(dJson))
                    {
                        dNode.Nodes.Add(new TreeNode("📜 dungeon.json")
                        {
                            ImageKey = "json",
                            SelectedImageKey = "json",
                            Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.GenericFile, Title = "dungeon.json", FilePath = dJson }
                        });
                    }

                    // Dungeon-wide Loot Tables
                    dNode.Nodes.Add(new TreeNode("💰 Loot Tables")
                    {
                        ImageKey = "json",
                        SelectedImageKey = "json",
                        Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.LootTables, Title = "Loot Tables", DungeonId = dungeonId }
                    });

                    // Levels
                    foreach (var lvl in pkg.Levels)
                    {
                        string lvlDir = Path.Combine(dDir, $"level_{lvl.LevelNumber}");
                        var lvlNode = new TreeNode($"🗺️ {lvl.LevelName}")
                        {
                            ImageKey = "level",
                            SelectedImageKey = "level",
                            Tag = new ProjectNodeInfo
                            {
                                Kind = ProjectNodeKind.Level,
                                Title = lvl.LevelName,
                                DungeonId = dungeonId,
                                LevelNumber = lvl.LevelNumber,
                                FilePath = lvlDir
                            }
                        };

                        // Children of level
                        string mapFile = Path.Combine(lvlDir, "map.txt");
                        lvlNode.Nodes.Add(new TreeNode("🗺️ map.txt")
                        {
                            ImageKey = "map",
                            SelectedImageKey = "map",
                            Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.Map, Title = "map.txt", DungeonId = dungeonId, LevelNumber = lvl.LevelNumber, FilePath = mapFile }
                        });

                        string cfgFile = Path.Combine(lvlDir, "config.json");
                        lvlNode.Nodes.Add(new TreeNode("💡 config.json (Zone Lighting)")
                        {
                            ImageKey = "lighting",
                            SelectedImageKey = "lighting",
                            Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.ZoneLighting, Title = "config.json", DungeonId = dungeonId, LevelNumber = lvl.LevelNumber, FilePath = cfgFile }
                        });

                        string transFile = Path.Combine(lvlDir, "transitions.json");
                        if (File.Exists(transFile))
                        {
                            lvlNode.Nodes.Add(new TreeNode("🚪 transitions.json")
                            {
                                ImageKey = "door",
                                SelectedImageKey = "door",
                                Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.Transitions, Title = "transitions.json", DungeonId = dungeonId, LevelNumber = lvl.LevelNumber, FilePath = transFile }
                            });
                        }

                        string itemsFile = Path.Combine(lvlDir, "items.txt");
                        if (File.Exists(itemsFile))
                        {
                            lvlNode.Nodes.Add(new TreeNode("💎 items.txt")
                            {
                                ImageKey = "items",
                                SelectedImageKey = "items",
                                Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.LevelItems, Title = "items.txt", DungeonId = dungeonId, LevelNumber = lvl.LevelNumber, FilePath = itemsFile }
                            });
                        }

                        string monFile = Path.Combine(lvlDir, "monsters.txt");
                        if (File.Exists(monFile))
                        {
                            lvlNode.Nodes.Add(new TreeNode("👾 monsters.txt")
                            {
                                ImageKey = "monster",
                                SelectedImageKey = "monster",
                                Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.LevelMonsters, Title = "monsters.txt", DungeonId = dungeonId, LevelNumber = lvl.LevelNumber, FilePath = monFile }
                            });
                        }

                        dNode.Nodes.Add(lvlNode);
                    }

                    dungeonsGroupNode.Nodes.Add(dNode);
                }
            }
            rootNode.Nodes.Add(dungeonsGroupNode);

            // 3. Objects & Items Schema
            var itemsGroupNode = new TreeNode("📦 Items & Objects")
            {
                ImageKey = "items",
                SelectedImageKey = "items",
                Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.ItemsSchema, Title = "Items & Objects", FilePath = Path.Combine(projectDir, "items.json") }
            };

            string itemsJsonPath = Path.Combine(projectDir, "items.json");
            if (File.Exists(itemsJsonPath) && project?.Items != null)
            {
                var itemsFileNode = new TreeNode("📜 items.json")
                {
                    ImageKey = "json",
                    SelectedImageKey = "json",
                    Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.ItemsSchema, Title = "items.json", FilePath = itemsJsonPath }
                };

                // Group item definitions by Category
                var catMap = new Dictionary<string, List<ItemDefinition>>(StringComparer.OrdinalIgnoreCase);
                foreach (var def in project.Items.GetAll())
                {
                    string cat = string.IsNullOrEmpty(def.Category) ? "General" : def.Category;
                    if (!catMap.ContainsKey(cat)) catMap[cat] = new List<ItemDefinition>();
                    catMap[cat].Add(def);
                }

                foreach (var catKvp in catMap)
                {
                    var catNode = new TreeNode($"📁 {catKvp.Key} ({catKvp.Value.Count})")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folder_open",
                        Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.ItemCategory, Title = catKvp.Key, Category = catKvp.Key }
                    };

                    foreach (var itemDef in catKvp.Value)
                    {
                        catNode.Nodes.Add(new TreeNode($"🗡️ {itemDef.Name} [{itemDef.Id}]")
                        {
                            ImageKey = "items",
                            SelectedImageKey = "items",
                            Tag = new ProjectNodeInfo
                            {
                                Kind = ProjectNodeKind.ItemDefinition,
                                Title = itemDef.Name,
                                ItemId = itemDef.Id,
                                Category = catKvp.Key,
                                FilePath = itemsJsonPath
                            }
                        });
                    }
                    itemsFileNode.Nodes.Add(catNode);
                }

                itemsGroupNode.Nodes.Add(itemsFileNode);
            }

            string classesJson = Path.Combine(projectDir, "item_classes.json");
            if (File.Exists(classesJson))
            {
                itemsGroupNode.Nodes.Add(new TreeNode("📜 item_classes.json")
                {
                    ImageKey = "json",
                    SelectedImageKey = "json",
                    Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.ItemClasses, Title = "item_classes.json", FilePath = classesJson }
                });
            }

            string objectsJson = Path.Combine(projectDir, "objects.json");
            if (File.Exists(objectsJson))
            {
                itemsGroupNode.Nodes.Add(new TreeNode("📜 objects.json")
                {
                    ImageKey = "json",
                    SelectedImageKey = "json",
                    Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.ObjectsSchema, Title = "objects.json", FilePath = objectsJson }
                });
            }
            rootNode.Nodes.Add(itemsGroupNode);

            // 4. Other Directories on Disk (Audio, Images, Zones, Objects, etc.)
            ScanCustomSubdirectories(projectDir, rootNode);

            _tree.Nodes.Add(rootNode);
            rootNode.Expand();
            dungeonsGroupNode.Expand();
            if (dungeonsGroupNode.Nodes.Count > 0)
            {
                dungeonsGroupNode.Nodes[0].Expand();
            }
            itemsGroupNode.Expand();

            _tree.EndUpdate();
        }

        private void ScanCustomSubdirectories(string rootDir, TreeNode parentNode)
        {
            try
            {
                var dirs = Directory.GetDirectories(rootDir);
                foreach (var d in dirs)
                {
                    string dirName = Path.GetFileName(d);
                    // Skip dungeons directory (already handled with custom semantic hierarchy)
                    if (string.Equals(dirName, "dungeons", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dirName, ".tmp", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dirName, "bin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dirName, "obj", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string iconKey = "folder";
                    var kind = ProjectNodeKind.Folder;
                    if (dirName.Equals("Audio", StringComparison.OrdinalIgnoreCase) || dirName.Equals("Sound", StringComparison.OrdinalIgnoreCase))
                    {
                        iconKey = "audio";
                        kind = ProjectNodeKind.AudioFolder;
                    }
                    else if (dirName.Equals("Images", StringComparison.OrdinalIgnoreCase) || dirName.Equals("Textures", StringComparison.OrdinalIgnoreCase))
                    {
                        iconKey = "image";
                        kind = ProjectNodeKind.ImagesFolder;
                    }

                    var dirNode = new TreeNode($"📁 {dirName}")
                    {
                        ImageKey = iconKey,
                        SelectedImageKey = iconKey,
                        Tag = new ProjectNodeInfo { Kind = kind, Title = dirName, FilePath = d }
                    };

                    // Add child files and subdirectories
                    PopulateDirectoryRecursive(d, dirNode);
                    parentNode.Nodes.Add(dirNode);
                }
            }
            catch { }
        }

        private void PopulateDirectoryRecursive(string currentDir, TreeNode parentNode)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(currentDir))
                {
                    string subName = Path.GetFileName(subDir);
                    var subNode = new TreeNode($"📁 {subName}")
                    {
                        ImageKey = "folder",
                        SelectedImageKey = "folder_open",
                        Tag = new ProjectNodeInfo { Kind = ProjectNodeKind.Folder, Title = subName, FilePath = subDir }
                    };
                    PopulateDirectoryRecursive(subDir, subNode);
                    parentNode.Nodes.Add(subNode);
                }

                foreach (var file in Directory.GetFiles(currentDir))
                {
                    string fName = Path.GetFileName(file);
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    string iconKey = "text";
                    var kind = ProjectNodeKind.GenericFile;

                    if (ext == ".json") { iconKey = "json"; }
                    else if (ext == ".wav" || ext == ".mp3" || ext == ".ogg") { iconKey = "audio"; kind = ProjectNodeKind.AudioFile; }
                    else if (ext == ".png" || ext == ".bmp" || ext == ".jpg") { iconKey = "image"; kind = ProjectNodeKind.ImageFile; }
                    else if (ext == ".txt") { iconKey = "text"; }

                    parentNode.Nodes.Add(new TreeNode(fName)
                    {
                        ImageKey = iconKey,
                        SelectedImageKey = iconKey,
                        Tag = new ProjectNodeInfo { Kind = kind, Title = fName, FilePath = file }
                    });
                }
            }
            catch { }
        }

        private static void OpenInExplorer(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            try
            {
                Process.Start("explorer.exe", path);
            }
            catch { }
        }

        private static ImageList CreateProjectImageList()
        {
            var il = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };

            il.Images.Add("project", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(0, 122, 204)))
                    g.FillRectangle(b, 2, 2, 12, 12);
                using (var p = new Pen(Color.White, 1.2f))
                    g.DrawRectangle(p, 3, 3, 10, 10);
                using (var p2 = new Pen(Color.FromArgb(180, 225, 255), 1f))
                    g.DrawLine(p2, 3, 8, 13, 8);
            }));

            il.Images.Add("folder", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(235, 185, 80)))
                {
                    g.FillRectangle(b, 2, 3, 6, 4);
                    g.FillRectangle(b, 2, 5, 12, 9);
                }
                using (var p = new Pen(Color.FromArgb(190, 140, 40), 1f))
                    g.DrawRectangle(p, 2, 5, 12, 9);
            }));

            il.Images.Add("folder_open", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(245, 200, 95)))
                {
                    g.FillRectangle(b, 2, 2, 6, 4);
                    Point[] pts = { new Point(1, 14), new Point(4, 6), new Point(15, 6), new Point(12, 14) };
                    g.FillPolygon(b, pts);
                }
                using (var p = new Pen(Color.FromArgb(190, 140, 40), 1f))
                {
                    Point[] pts = { new Point(1, 14), new Point(4, 6), new Point(15, 6), new Point(12, 14) };
                    g.DrawPolygon(p, pts);
                }
            }));

            il.Images.Add("dungeon", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(120, 125, 135)))
                    g.FillRectangle(b, 2, 4, 12, 10);
                using (var b2 = new SolidBrush(Color.FromArgb(160, 165, 175)))
                {
                    g.FillRectangle(b2, 2, 2, 3, 4);
                    g.FillRectangle(b2, 7, 2, 2, 4);
                    g.FillRectangle(b2, 11, 2, 3, 4);
                }
                using (var db = new SolidBrush(Color.FromArgb(40, 40, 45)))
                    g.FillRectangle(db, 6, 9, 4, 5);
            }));

            il.Images.Add("level", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(70, 130, 180)))
                    g.FillRectangle(b, 2, 2, 12, 12);
                using (var p = new Pen(Color.White, 1f))
                {
                    g.DrawRectangle(p, 3, 3, 10, 10);
                    g.DrawLine(p, 3, 8, 13, 8);
                    g.DrawLine(p, 8, 3, 8, 13);
                }
            }));

            il.Images.Add("map", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(245, 240, 225)))
                    g.FillRectangle(b, 2, 2, 12, 12);
                using (var p = new Pen(Color.FromArgb(140, 120, 80), 1f))
                {
                    g.DrawRectangle(p, 2, 2, 12, 12);
                    g.DrawLine(p, 6, 2, 6, 14);
                    g.DrawLine(p, 10, 2, 10, 14);
                    g.DrawLine(p, 2, 6, 14, 6);
                    g.DrawLine(p, 2, 10, 14, 10);
                }
            }));

            il.Images.Add("lighting", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(255, 210, 40)))
                    g.FillEllipse(b, 4, 2, 8, 8);
                using (var p = new Pen(Color.FromArgb(200, 140, 0), 1f))
                    g.DrawEllipse(p, 4, 2, 8, 8);
                using (var b2 = new SolidBrush(Color.FromArgb(160, 160, 160)))
                    g.FillRectangle(b2, 6, 10, 4, 4);
            }));

            il.Images.Add("items", DrawIcon(g =>
            {
                using (var p = new Pen(Color.FromArgb(0, 180, 220), 2.2f))
                    g.DrawLine(p, 3, 13, 12, 4);
                using (var p2 = new Pen(Color.FromArgb(210, 170, 40), 1.5f))
                    g.DrawLine(p2, 3, 11, 7, 15);
            }));

            il.Images.Add("door", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(140, 85, 45)))
                    g.FillRectangle(b, 3, 2, 10, 12);
                using (var p = new Pen(Color.FromArgb(80, 40, 15), 1f))
                    g.DrawRectangle(p, 3, 2, 10, 12);
                using (var gb = new SolidBrush(Color.Gold))
                    g.FillEllipse(gb, 10, 8, 2, 2);
            }));

            il.Images.Add("json", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(245, 245, 248)))
                    g.FillRectangle(b, 3, 2, 10, 12);
                using (var p = new Pen(Color.FromArgb(220, 120, 20), 1.2f))
                    g.DrawRectangle(p, 3, 2, 10, 12);
                using (var f = new Font("Arial", 6.5f, FontStyle.Bold))
                using (var tb = new SolidBrush(Color.FromArgb(220, 100, 0)))
                    g.DrawString("{}", f, tb, 2.5f, 2.5f);
            }));

            il.Images.Add("audio", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(170, 70, 220)))
                {
                    g.FillEllipse(b, 2, 9, 5, 4);
                    g.FillRectangle(b, 5, 3, 2, 8);
                    g.FillRectangle(b, 5, 3, 7, 3);
                    g.FillRectangle(b, 10, 3, 2, 8);
                    g.FillEllipse(b, 8, 8, 5, 4);
                }
            }));

            il.Images.Add("image", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(40, 160, 120)))
                    g.FillRectangle(b, 2, 2, 12, 12);
                using (var yb = new SolidBrush(Color.Yellow))
                    g.FillEllipse(yb, 4, 4, 3, 3);
                using (var mb = new SolidBrush(Color.White))
                {
                    Point[] pts = { new Point(3, 12), new Point(7, 7), new Point(11, 12) };
                    g.FillPolygon(mb, pts);
                }
            }));

            il.Images.Add("text", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.White))
                    g.FillRectangle(b, 3, 2, 10, 12);
                using (var p = new Pen(Color.FromArgb(160, 160, 170), 1f))
                    g.DrawRectangle(p, 3, 2, 10, 12);
                using (var lp = new Pen(Color.FromArgb(120, 120, 130), 1f))
                {
                    g.DrawLine(lp, 5, 5, 11, 5);
                    g.DrawLine(lp, 5, 8, 11, 8);
                    g.DrawLine(lp, 5, 11, 9, 11);
                }
            }));

            il.Images.Add("monster", DrawIcon(g =>
            {
                using (var b = new SolidBrush(Color.FromArgb(220, 50, 50)))
                    g.FillEllipse(b, 3, 3, 10, 9);
                using (var eb = new SolidBrush(Color.Yellow))
                {
                    g.FillRectangle(eb, 5, 6, 2, 2);
                    g.FillRectangle(eb, 9, 6, 2, 2);
                }
            }));

            return il;
        }

        private static Bitmap DrawIcon(Action<Graphics> drawAction)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                drawAction(g);
            }
            return bmp;
        }
    }
}
