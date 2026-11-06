using System;
using System.Drawing;
using System.Windows.Forms;

namespace Metraik
{
    internal sealed class SettingsForm : Form
    {
        private readonly ComboBox provider;
        private readonly ComboBox source;
        private readonly NumericUpDown active;
        private readonly NumericUpDown weekly;
        private readonly DateTimePicker activeReset;
        private readonly DateTimePicker weeklyReset;
        private readonly CheckBox startup;
        private readonly CheckBox alerts;
        private readonly NumericUpDown threshold;
        private readonly Label sourceNote;
        private readonly AppSettings initial;
        public AppSettings ResultSettings { get; private set; }
        public bool StartWithWindows { get { return startup.Checked; } }

        public SettingsForm(AppSettings existing, bool startWithWindows)
        {
            initial = existing.Copy();
            Text = "Metraik - Settings";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Ui.Background;
            ForeColor = Ui.Text;
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(22);
            TableLayoutPanel grid = new TableLayoutPanel();
            grid.AutoSize = true;
            grid.ColumnCount = 2;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            grid.Dock = DockStyle.Fill;
            Controls.Add(grid);
            AddWide(grid, Label("Usage settings", true));
            AddWide(grid, Label("Service (manual tracking, one at a time)", false));
            provider = Combo("provider", ProviderCatalog.Names);
            provider.SelectedItem = initial.Provider;
            AddWide(grid, provider);
            AddWide(grid, Label("Data source", false));
            source = Combo("source", new string[] { "Demo - example numbers", "Manual - entered by you" });
            source.SelectedIndex = initial.IsDemo ? 0 : 1;
            AddWide(grid, source);
            sourceNote = Label("", false);
            sourceNote.MaximumSize = new Size(412, 0);
            sourceNote.ForeColor = Ui.Muted;
            AddWide(grid, sourceNote);
            active = Number("active", initial.ActiveRemaining, 0, 100);
            AddRow(grid, Label("Active allowance remaining (%)", false), active);
            weekly = Number("weekly", initial.WeeklyRemaining, 0, 100);
            AddRow(grid, Label("Weekly allowance remaining (%)", false), weekly);
            AddWide(grid, Label("Active reset (local time)", false));
            activeReset = Date("activeReset", initial.ActiveResetLocal);
            AddWide(grid, activeReset);
            AddWide(grid, Label("Weekly reset (local time)", false));
            weeklyReset = Date("weeklyReset", initial.WeeklyResetLocal);
            AddWide(grid, weeklyReset);
            alerts = new CheckBox { Name = "alerts", Text = "Notify at or below (%)", Checked = initial.AlertsEnabled, AutoSize = true };
            threshold = Number("threshold", initial.AlertThreshold, 1, 50);
            AddRow(grid, alerts, threshold);
            startup = new CheckBox { Name = "startup", Text = "Start automatically with Windows", Checked = startWithWindows, AutoSize = true };
            AddWide(grid, startup);
            FlowLayoutPanel actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            Button save = MakeButton("Save");
            save.Name = "save";
            save.Click += delegate { SaveAndClose(); };
            Button cancel = MakeButton("Cancel");
            cancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(save);
            actions.Controls.Add(cancel);
            AddWide(grid, actions);
            AcceptButton = save;
            CancelButton = cancel;
            source.SelectedIndexChanged += delegate { UpdateMode(); };
            alerts.CheckedChanged += delegate { UpdateMode(); };
            provider.SelectedIndexChanged += delegate
            {
                // A new label must never re-label another service's manual readings.
                source.SelectedIndex = 0;
                active.Value = 72;
                weekly.Value = 48;
                activeReset.Value = DateTime.Now.AddMinutes(135);
                weeklyReset.Value = DateTime.Now.AddDays(3);
                UpdateMode();
            };
            UpdateMode();
        }
        private static Label Label(string text, bool title)
        {
            Label label = new Label { Text = text, AutoSize = true, Margin = new Padding(0, 8, 0, 6) };
            if (title) label.Font = new Font("Segoe UI", 17, FontStyle.Bold);
            return label;
        }
        private static void AddWide(TableLayoutPanel grid, Control control)
        {
            int row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(control, 0, row);
            grid.SetColumnSpan(control, 2);
            if (!(control is Label)) control.Margin = new Padding(0, 3, 0, 7);
        }
        private static void AddRow(TableLayoutPanel grid, Control left, Control right)
        {
            int row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(left, 0, row);
            grid.Controls.Add(right, 1, row);
            left.Margin = new Padding(0, 8, 0, 6);
            right.Margin = new Padding(4, 3, 0, 7);
        }
        private static ComboBox Combo(string name, string[] values)
        {
            ComboBox combo = new ComboBox { Name = name, AccessibleName = name, DropDownStyle = ComboBoxStyle.DropDownList, Width = 412 };
            combo.Items.AddRange(values);
            return combo;
        }
        private static NumericUpDown Number(string name, int value, int min, int max)
        {
            return new NumericUpDown { Name = name, AccessibleName = name + " percentage", Width = 108,
                Minimum = min, Maximum = max, Value = Math.Max(min, Math.Min(max, value)), TextAlign = HorizontalAlignment.Right };
        }
        private static DateTimePicker Date(string name, DateTime value)
        {
            DateTimePicker date = new DateTimePicker { Name = name, AccessibleName = name + " local date and time", Width = 412,
                Format = DateTimePickerFormat.Custom, CustomFormat = "ddd dd MMM yyyy    HH:mm", ShowUpDown = true };
            date.Value = value < date.MinDate || value > date.MaxDate ? DateTime.Now : value;
            return date;
        }
        private static Button MakeButton(string text)
        {
            Button button = new Button { Text = text, Width = 98, Height = 34, FlatStyle = FlatStyle.Flat,
                BackColor = Ui.Panel, ForeColor = Ui.Text, Margin = new Padding(8, 10, 0, 0) };
            button.FlatAppearance.BorderColor = Ui.Border;
            return button;
        }
        private void UpdateMode()
        {
            bool manual = source.SelectedIndex == 1;
            active.Enabled = weekly.Enabled = activeReset.Enabled = weeklyReset.Enabled = manual;
            threshold.Enabled = manual && alerts.Checked;
            sourceNote.Text = manual
                ? "Values stay on this computer. Nothing is fetched from your account. Enter new readings after each reset."
                : "Example readings only. Changing service starts in Demo mode; enter manual values for that service.";
        }
        private void SaveAndClose()
        {
            if (source.SelectedIndex == 1 && (activeReset.Value <= DateTime.Now || weeklyReset.Value <= DateTime.Now))
            {
                MessageBox.Show(this, "Enter future reset times for both allowances, or choose Demo mode.", "Metraik", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ResultSettings = initial.Copy();
            ResultSettings.Provider = (string)provider.SelectedItem;
            ResultSettings.Mode = source.SelectedIndex == 1 ? "Manual" : "Demo";
            ResultSettings.ActiveRemaining = (int)active.Value;
            ResultSettings.WeeklyRemaining = (int)weekly.Value;
            ResultSettings.ActiveResetLocal = activeReset.Value;
            ResultSettings.WeeklyResetLocal = weeklyReset.Value;
            ResultSettings.AlertsEnabled = alerts.Checked;
            ResultSettings.AlertThreshold = (int)threshold.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
