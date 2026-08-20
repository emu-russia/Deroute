using System;
using System.Drawing;
using System.Windows.Forms;
using DerouteSharp.Collab;

namespace DerouteSharp
{
    /// <summary>
    /// Friendly CollabMCP settings form (replaces the raw PropertyGrid): masked API key,
    /// connection test, session picker. On OK the values are written into CollabSettings
    /// (persisted to Properties.Settings by the app's normal save flow).
    /// </summary>
    public class FormCollabSettings : Form
    {
        private readonly CollabClient _client;
        private readonly CollabSettings _settings;
        private CheckBox chkEnabled;
        private TextBox txtServerUrl;
        private TextBox txtApiKey;
        private CheckBox chkShowKey;
        private TextBox txtUsername;
        private TextBox txtSessionId;
        private NumericUpDown nudReconnectDelay;
        private NumericUpDown nudMaxAttempts;
        private Button btnTest;
        private Button btnPickSession;
        private Label lblStatus;

        public FormCollabSettings(CollabSettings settings)
        {
            _settings = settings;
            _client = new CollabClient(settings);
            InitializeUi();
        }

        private void InitializeUi()
        {
            Text = "CollabMCP Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 330);

            int y = 14;
            chkEnabled = new CheckBox { Text = "Enable collaboration", Location = new Point(14, y), AutoSize = true, Checked = _settings.Enabled };
            y += 30;

            AddLabel("Server URL:", y); y += 20;
            txtServerUrl = new TextBox { Location = new Point(14, y), Size = new Size(392, 23), Text = _settings.ServerUrl };
            y += 30;

            AddLabel("API key:", y); y += 20;
            txtApiKey = new TextBox { Location = new Point(14, y), Size = new Size(300, 23), Text = _settings.ApiKey, UseSystemPasswordChar = true };
            chkShowKey = new CheckBox { Text = "Show", Location = new Point(320, y), AutoSize = true };
            chkShowKey.CheckedChanged += (s, e) => txtApiKey.UseSystemPasswordChar = !chkShowKey.Checked;
            y += 30;

            AddLabel("Username:", y); y += 20;
            txtUsername = new TextBox { Location = new Point(14, y), Size = new Size(392, 23), Text = _settings.Username };
            y += 30;

            AddLabel("Session ID:", y); y += 20;
            txtSessionId = new TextBox { Location = new Point(14, y), Size = new Size(300, 23), Text = _settings.SessionId };
            btnPickSession = new Button { Text = "Pick...", Location = new Point(320, y), Size = new Size(86, 24) };
            btnPickSession.Click += (s, e) =>
            {
                using (var dlg = new FormCollabSession(_client, _settings))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        txtSessionId.Text = _settings.SessionId;
                }
            };
            y += 32;

            AddLabel("Reconnect delay (ms):", y); y += 20;
            nudReconnectDelay = new NumericUpDown
            {
                Location = new Point(14, y),
                Size = new Size(120, 23),
                Minimum = 500,
                Maximum = 60000,
                Increment = 500,
                Value = Math.Max(500, _settings.ReconnectDelayMs)
            };
            y += 30;

            AddLabel("Max reconnect attempts:", y); y += 20;
            nudMaxAttempts = new NumericUpDown
            {
                Location = new Point(14, y),
                Size = new Size(120, 23),
                Minimum = 1,
                Maximum = 1000,
                Value = Math.Max(1, _settings.MaxReconnectAttempts)
            };
            y += 32;

            btnTest = new Button { Text = "Test connection", Location = new Point(14, y), Size = new Size(130, 26) };
            btnTest.Click += async (s, e) =>
            {
                lblStatus.Text = "Testing...";
                _settings.ServerUrl = txtServerUrl.Text.Trim();
                var ok = await _client.TestConnectionAsync();
                lblStatus.ForeColor = ok ? Color.Green : Color.Red;
                lblStatus.Text = ok
                    ? "Server reachable (HTTP 200)."
                    : "Connection failed — check the URL and network.";
            };
            y += 32;

            lblStatus = new Label { Location = new Point(150, 246), Size = new Size(260, 20), ForeColor = Color.DimGray };

            var btnOk = new Button { Text = "OK", Location = new Point(232, 290), Size = new Size(86, 28), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(326, 290), Size = new Size(82, 28), DialogResult = DialogResult.Cancel };
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                chkEnabled, txtServerUrl, txtApiKey, chkShowKey, txtUsername, txtSessionId, btnPickSession,
                nudReconnectDelay, nudMaxAttempts, btnTest, lblStatus, btnOk, btnCancel
            });
        }

        private void AddLabel(string text, int y)
        {
            Controls.Add(new Label { Text = text, Location = new Point(14, y), AutoSize = true });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                _settings.Enabled = chkEnabled.Checked;
                _settings.ServerUrl = txtServerUrl.Text.Trim();
                _settings.ApiKey = txtApiKey.Text;
                _settings.Username = txtUsername.Text.Trim();
                _settings.SessionId = txtSessionId.Text.Trim();
                _settings.ReconnectDelayMs = (int)nudReconnectDelay.Value;
                _settings.MaxReconnectAttempts = (int)nudMaxAttempts.Value;
            }
            base.OnFormClosing(e);
        }
    }
}
