using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using GridLocked;

namespace GridLockedEditor
{
    /// <summary>
    /// Visual designer allowing non-programmers to define new Item Class Templates
    /// with custom fields, types, and min/max ranges, saved to item_classes.json.
    /// </summary>
    public sealed class ClassTemplateDesignerDialog : Form
    {
        private readonly string _projectDir;
        private readonly List<ItemClassTemplate> _templates = new List<ItemClassTemplate>();

        private ListBox _lbTemplates;
        private TextBox _txtClassName;
        private ComboBox _cbInheritsFrom;
        private ComboBox _cbCategory;
        private TextBox _txtDescription;
        private ComboBox _cbDefaultSprite;
        private DataGridView _gridFields;
        private ItemClassTemplate _selectedTemplate;

        public ClassTemplateDesignerDialog(string projectDir)
        {
            _projectDir = projectDir ?? string.Empty;
            Text = "Class & Archetype Template Designer (item_classes.json)";
            Width = 850;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);
            MinimumSize = new Size(750, 500);

            BuildUi();
            LoadTemplates();
        }

        private void BuildUi()
        {
            var split = new SplitContainer { Dock = DockStyle.Fill };
            Controls.Add(split);
            Shown += (s, e) =>
            {
                try { split.SplitterDistance = 240; } catch { }
            };

            // Left: Template List
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            split.Panel1.Controls.Add(pnlLeft);

            var lblHeader = new Label
            {
                Text = "Class Templates",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            pnlLeft.Controls.Add(lblHeader);

            _lbTemplates = new ListBox { Dock = DockStyle.Fill };
            _lbTemplates.SelectedIndexChanged += (s, e) => OnTemplateSelected();
            pnlLeft.Controls.Add(_lbTemplates);
            _lbTemplates.BringToFront();

            var pnlLeftBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(0, 5, 0, 0)
            };
            var btnNewTmpl = new Button { Text = "+ New Class", AutoSize = true, Height = 28 };
            btnNewTmpl.Click += (s, e) => CreateNewTemplate();
            pnlLeftBtns.Controls.Add(btnNewTmpl);

            var btnDelTmpl = new Button { Text = "Delete", AutoSize = true, Height = 28 };
            btnDelTmpl.Click += (s, e) => DeleteSelectedTemplate();
            pnlLeftBtns.Controls.Add(btnDelTmpl);
            pnlLeft.Controls.Add(pnlLeftBtns);

            // Right: Template Properties & Fields
            var pnlRight = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(15) };
            split.Panel2.Controls.Add(pnlRight);

            int y = 10;
            Label AddLabel(string text)
            {
                var l = new Label { Text = text, Left = 15, Top = y, AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                pnlRight.Controls.Add(l);
                y += 20;
                return l;
            }

            AddLabel("Class Name (e.g. MagicStaff, Firearm, ThrowingDagger):");
            _txtClassName = new TextBox { Left = 15, Top = y, Width = 300 };
            pnlRight.Controls.Add(_txtClassName);
            y += 30;

            AddLabel("Base Category:");
            _cbCategory = new ComboBox { Left = 15, Top = y, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbCategory.Items.AddRange(new object[] { "Weapon", "Armor", "Accessory", "Consumable", "Container", "Prop" });
            pnlRight.Controls.Add(_cbCategory);
            y += 30;

            AddLabel("Inherits From Class / Archetype (Optional):");
            _cbInheritsFrom = new ComboBox { Left = 15, Top = y, Width = 300, DropDownStyle = ComboBoxStyle.DropDown };
            _cbInheritsFrom.Items.Add("(None)");
            pnlRight.Controls.Add(_cbInheritsFrom);
            y += 30;

            AddLabel("Class Description:");
            _txtDescription = new TextBox { Left = 15, Top = y, Width = 450, Height = 40, Multiline = true };
            pnlRight.Controls.Add(_txtDescription);
            y += 50;

            AddLabel("Default Visual Sprite:");
            _cbDefaultSprite = new ComboBox { Left = 15, Top = y, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            _cbDefaultSprite.Items.AddRange(new object[] { "weapon", "shield", "armor", "potion", "scroll", "chest", "key", "rock" });
            pnlRight.Controls.Add(_cbDefaultSprite);
            y += 35;

            // Custom fields schema table
            var lblFields = AddLabel("Custom Schema Fields & Ranges (Properties in this Class):");
            _gridFields = new DataGridView
            {
                Left = 15,
                Top = y,
                Width = 500,
                Height = 160,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            _gridFields.Columns.Add("Name", "Field Name");
            var colType = new DataGridViewComboBoxColumn
            {
                Name = "Type",
                HeaderText = "Data Type"
            };
            colType.Items.AddRange("string", "int", "float", "bool", "enum");
            _gridFields.Columns.Add(colType);
            _gridFields.Columns.Add("Default", "Default");
            _gridFields.Columns.Add("Min", "Min");
            _gridFields.Columns.Add("Max", "Max");
            _gridFields.Columns.Add("Tooltip", "Description / Hint");

            pnlRight.Controls.Add(_gridFields);
            y += 170;

            var pnlFieldBtns = new FlowLayoutPanel { Left = 15, Top = y, Width = 500, Height = 35 };
            var btnAddField = new Button { Text = "+ Add Field", AutoSize = true, Height = 28 };
            btnAddField.Click += (s, e) =>
            {
                _gridFields.Rows.Add("newField", "int", "5", "0", "100", "Custom attribute");
            };
            pnlFieldBtns.Controls.Add(btnAddField);

            var btnDelField = new Button { Text = "- Remove Field", AutoSize = true, Height = 28 };
            btnDelField.Click += (s, e) =>
            {
                if (_gridFields.SelectedRows.Count > 0)
                {
                    foreach (DataGridViewRow r in _gridFields.SelectedRows)
                        _gridFields.Rows.Remove(r);
                }
            };
            pnlFieldBtns.Controls.Add(btnDelField);
            pnlRight.Controls.Add(pnlFieldBtns);
            y += 40;

            var pnlBottomActions = new FlowLayoutPanel { Left = 15, Top = y, Width = 500, Height = 45 };
            var btnApply = new Button
            {
                Text = "Apply to Template",
                Width = 140,
                Height = 32,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            btnApply.Click += (s, e) => ApplySelectedTemplate();
            pnlBottomActions.Controls.Add(btnApply);

            var btnSaveJson = new Button
            {
                Text = "Save item_classes.json",
                Width = 180,
                Height = 32,
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            btnSaveJson.Click += (s, e) => SaveTemplatesJson();
            pnlBottomActions.Controls.Add(btnSaveJson);

            pnlRight.Controls.Add(pnlBottomActions);
        }

        private void LoadTemplates()
        {
            _templates.Clear();
            _lbTemplates.Items.Clear();
            _cbInheritsFrom.Items.Clear();
            _cbInheritsFrom.Items.Add("(None)");

            string path = Path.Combine(_projectDir, "item_classes.json");
            if (File.Exists(path))
            {
                var json = JsonLite.Parse(File.ReadAllText(path));
                if (json != null && json.ArrayValue != null)
                {
                    foreach (var node in json.ArrayValue)
                    {
                        var t = new ItemClassTemplate
                        {
                            ClassName = node.GetString("className", ""),
                            InheritsFrom = node.GetString("inheritsFrom", ""),
                            Category = node.GetString("category", "Weapon"),
                            Description = node.GetString("description", ""),
                            DefaultSprite = node.GetString("defaultSprite", "weapon")
                        };

                        var fArr = node.Get("fields");
                        if (fArr != null && fArr.ArrayValue != null)
                        {
                            foreach (var fNode in fArr.ArrayValue)
                            {
                                var f = new TemplateField
                                {
                                    Name = fNode.GetString("name", ""),
                                    Type = fNode.GetString("type", "string"),
                                    DefaultValue = fNode.GetString("default", ""),
                                    MinValue = (float)fNode.GetDouble("min", 0),
                                    MaxValue = (float)fNode.GetDouble("max", 100),
                                    Tooltip = fNode.GetString("tooltip", "")
                                };
                                t.Fields.Add(f);
                            }
                        }
                        _templates.Add(t);
                        _lbTemplates.Items.Add(t.ClassName);
                        _cbInheritsFrom.Items.Add(t.ClassName);
                    }
                }
            }

            // If empty, add standard starter templates
            if (_templates.Count == 0)
            {
                var staffT = new ItemClassTemplate
                {
                    ClassName = "MagicStaff",
                    Category = "Weapon",
                    Description = "Two-handed magic channeling focus",
                    DefaultSprite = "weapon"
                };
                staffT.Fields.Add(new TemplateField { Name = "manaCost", Type = "int", DefaultValue = "5", MinValue = 0, MaxValue = 50, Tooltip = "Mana consumed per cast" });
                staffT.Fields.Add(new TemplateField { Name = "magicElement", Type = "enum", DefaultValue = "Arcane", Tooltip = "Primary spell affinity" });
                staffT.Fields.Add(new TemplateField { Name = "spellEffect", Type = "string", DefaultValue = "Arcane Bolt", Tooltip = "Spell projectile effect" });
                _templates.Add(staffT);
                _lbTemplates.Items.Add(staffT.ClassName);
                _cbInheritsFrom.Items.Add(staffT.ClassName);
            }

            if (_lbTemplates.Items.Count > 0)
                _lbTemplates.SelectedIndex = 0;
        }

        private void OnTemplateSelected()
        {
            int idx = _lbTemplates.SelectedIndex;
            if (idx < 0 || idx >= _templates.Count) return;

            _selectedTemplate = _templates[idx];
            _txtClassName.Text = _selectedTemplate.ClassName;
            _cbCategory.SelectedItem = _selectedTemplate.Category;
            _cbInheritsFrom.SelectedItem = string.IsNullOrEmpty(_selectedTemplate.InheritsFrom) ? "(None)" : _selectedTemplate.InheritsFrom;
            _txtDescription.Text = _selectedTemplate.Description;
            _cbDefaultSprite.SelectedItem = _selectedTemplate.DefaultSprite;

            _gridFields.Rows.Clear();
            foreach (var f in _selectedTemplate.Fields)
            {
                _gridFields.Rows.Add(f.Name, f.Type, f.DefaultValue, f.MinValue, f.MaxValue, f.Tooltip);
            }
        }

        private void ApplySelectedTemplate()
        {
            if (_selectedTemplate == null) return;

            _selectedTemplate.ClassName = _txtClassName.Text.Trim();
            _selectedTemplate.Category = _cbCategory.SelectedItem?.ToString() ?? "Weapon";
            string inh = _cbInheritsFrom.SelectedItem?.ToString();
            _selectedTemplate.InheritsFrom = (inh == "(None)" || string.IsNullOrEmpty(inh)) ? "" : inh;
            _selectedTemplate.Description = _txtDescription.Text.Trim();
            _selectedTemplate.DefaultSprite = _cbDefaultSprite.SelectedItem?.ToString() ?? "weapon";

            _selectedTemplate.Fields.Clear();
            foreach (DataGridViewRow r in _gridFields.Rows)
            {
                if (r.IsNewRow) continue;
                string name = r.Cells["Name"].Value?.ToString();
                if (string.IsNullOrEmpty(name)) continue;

                string type = r.Cells["Type"].Value?.ToString() ?? "string";
                string defVal = r.Cells["Default"].Value?.ToString() ?? "";
                float.TryParse(r.Cells["Min"].Value?.ToString() ?? "0", out float min);
                float.TryParse(r.Cells["Max"].Value?.ToString() ?? "100", out float max);
                string tooltip = r.Cells["Tooltip"].Value?.ToString() ?? "";

                _selectedTemplate.Fields.Add(new TemplateField
                {
                    Name = name,
                    Type = type,
                    DefaultValue = defVal,
                    MinValue = min,
                    MaxValue = max,
                    Tooltip = tooltip
                });
            }

            int idx = _lbTemplates.SelectedIndex;
            _lbTemplates.Items[idx] = _selectedTemplate.ClassName;
            MessageBox.Show($"Class '{_selectedTemplate.ClassName}' updated in memory.", "Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CreateNewTemplate()
        {
            var t = new ItemClassTemplate
            {
                ClassName = "NewClass" + (_templates.Count + 1),
                Category = "Weapon",
                Description = "Custom item class blueprint",
                DefaultSprite = "weapon"
            };
            _templates.Add(t);
            _lbTemplates.Items.Add(t.ClassName);
            _cbInheritsFrom.Items.Add(t.ClassName);
            _lbTemplates.SelectedIndex = _lbTemplates.Items.Count - 1;
        }

        private void DeleteSelectedTemplate()
        {
            int idx = _lbTemplates.SelectedIndex;
            if (idx < 0 || idx >= _templates.Count) return;

            var t = _templates[idx];
            if (MessageBox.Show($"Delete class template '{t.ClassName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _templates.RemoveAt(idx);
                _lbTemplates.Items.RemoveAt(idx);
                if (_lbTemplates.Items.Count > 0)
                    _lbTemplates.SelectedIndex = Math.Min(idx, _lbTemplates.Items.Count - 1);
            }
        }

        private void SaveTemplatesJson()
        {
            if (string.IsNullOrEmpty(_projectDir)) return;
            ApplySelectedTemplate();

            string path = Path.Combine(_projectDir, "item_classes.json");
            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < _templates.Count; i++)
            {
                var t = _templates[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"className\": \"{t.ClassName}\",");
                sb.AppendLine($"    \"category\": \"{t.Category}\",");
                if (!string.IsNullOrEmpty(t.InheritsFrom))
                    sb.AppendLine($"    \"inheritsFrom\": \"{t.InheritsFrom}\",");
                sb.AppendLine($"    \"description\": \"{t.Description}\",");
                sb.AppendLine($"    \"defaultSprite\": \"{t.DefaultSprite}\",");
                sb.AppendLine("    \"fields\": [");
                for (int j = 0; j < t.Fields.Count; j++)
                {
                    var f = t.Fields[j];
                    sb.Append($"      {{ \"name\": \"{f.Name}\", \"type\": \"{f.Type}\", \"default\": \"{f.DefaultValue}\", \"min\": {f.MinValue}, \"max\": {f.MaxValue}, \"tooltip\": \"{f.Tooltip}\" }}");
                    if (j < t.Fields.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("    ]");
                sb.Append("  }");
                if (i < _templates.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("]");

            File.WriteAllText(path, sb.ToString());
            MessageBox.Show($"Saved {path}", "Class Templates Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
