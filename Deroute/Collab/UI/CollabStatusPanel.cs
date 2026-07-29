using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DerouteSharp.Collab;

namespace DerouteSharp.Collab.UI
{
    public partial class CollabStatusPanel : UserControl
    {
        private readonly CollabClient _client;
        private readonly CollabSettings _settings;
        private Timer _refreshTimer;
        private Color _onlineColor = Color.Green;
        private Color _offlineColor = Color.Gray;
        private Color _connectingColor = Color.Orange;

        public CollabStatusPanel(CollabClient client, CollabSettings settings)
        {
            _client = client;
            _settings = settings;
            InitializeComponent();
            SetupTimer();
            UpdateStatus();

            _client.OnConnected += (s, e) => InvokeOnUiThread(() => UpdateStatus());
            _client.OnDisconnected += (s, e) => InvokeOnUiThread(() => UpdateStatus());
            _client.OnUserJoined += (s, userId) => InvokeOnUiThread(() => RefreshUserList());
            _client.OnUserLeft += (s, userId) => InvokeOnUiThread(() => RefreshUserList());
            _client.OnError += (s, error) => InvokeOnUiThread(() => ShowError(error));
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Dock = DockStyle.Bottom;
            Height = 32;
            BackColor = Color.FromArgb(245, 245, 245);
            BorderStyle = BorderStyle.Fixed3D;

            statusLabel = new Label
            {
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Regular),
                ForeColor = _offlineColor,
                Text = "Disconnected"
            };

            userCountLabel = new Label
            {
                Location = new Point(120, 8),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Text = "0 users"
            };

            sessionIdLabel = new Label
            {
                Location = new Point(220, 8),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Text = ""
            };

            reconnectBtn = new Button
            {
                Location = new Point(450, 5),
                Size = new Size(75, 22),
                Text = "Reconnect",
                FlatStyle = FlatStyle.Flat,
                BackColor = SystemColors.Control,
                ForeColor = SystemColors.ControlText,
                Cursor = Cursors.Hand
            };
            reconnectBtn.Click += ReconnectBtn_Click;

            Controls.AddRange(new Control[] { statusLabel, userCountLabel, sessionIdLabel, reconnectBtn });
            ResumeLayout(false);
            PerformLayout();
        }

        private void SetupTimer()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 5000;
            _refreshTimer.Tick += (s, e) => InvokeOnUiThread(() => RefreshUserList());
            _refreshTimer.Start();
        }

        private void UpdateStatus()
        {
            if (_client.IsConnected)
            {
                statusLabel.ForeColor = _onlineColor;
                statusLabel.Text = "Connected";
                reconnectBtn.Visible = false;
            }
            else if (_settings.Enabled)
            {
                statusLabel.ForeColor = _connectingColor;
                statusLabel.Text = "Connecting...";
                reconnectBtn.Visible = true;
            }
            else
            {
                statusLabel.ForeColor = _offlineColor;
                statusLabel.Text = "Collab Disabled";
                reconnectBtn.Visible = false;
            }
        }

        private async void RefreshUserList()
        {
            if (_client.IsConnected && !string.IsNullOrEmpty(_settings.SessionId))
            {
                var users = await _client.GetConnectedUsersAsync();
                userCountLabel.Text = $"{users.Count} user{(users.Count != 1 ? "s" : "")}";
                sessionIdLabel.Text = $"Session: {_settings.SessionId.Substring(0, Math.Min(8, _settings.SessionId.Length))}...";
            }
            else
            {
                userCountLabel.Text = "0 users";
                sessionIdLabel.Text = "";
            }
        }

        private async void ReconnectBtn_Click(object sender, EventArgs e)
        {
            reconnectBtn.Enabled = false;
            reconnectBtn.Text = "Connecting...";
            await _client.ConnectAsync();
            reconnectBtn.Enabled = true;
            reconnectBtn.Text = "Reconnect";
        }

        private void ShowError(string error)
        {
            statusLabel.ForeColor = Color.Red;
            statusLabel.Text = $"Error: {error.Substring(0, Math.Min(30, error.Length))}";
            InvokeOnUiThread(() =>
            {
                statusLabel.ForeColor = _offlineColor;
                statusLabel.Text = "Disconnected";
            });
        }

        private void InvokeOnUiThread(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        private Label statusLabel;
        private Label userCountLabel;
        private Label sessionIdLabel;
        private Button reconnectBtn;
    }
}
