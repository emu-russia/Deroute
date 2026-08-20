using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DerouteSharp.Collab;

namespace DerouteSharp
{
    /// <summary>
    /// Participants window: lists connected users with their palette color, plus the
    /// 15-color palette legend. Auto-refreshes every 5 seconds while open.
    /// </summary>
    public class FormCollabUsers : Form
    {
        private static readonly string[] Palette = new[]
        {
            "#FF6B6B","#4ECDC4","#45B7D1","#96CEB4","#FFEAA7","#DDA0DD","#98D8C8","#F7DC6F",
            "#BB8FCE","#85C1E9","#F8C471","#82E0AA","#F1948A","#85929E","#73C6B6"
        };

        private readonly CollabClient _client;
        private readonly CollabSettings _settings;
        private ListView lstUsers;
        private Button btnRefresh;
        private Label lblStatus;
        private Timer _timer;

        public FormCollabUsers(CollabClient client, CollabSettings settings)
        {
            _client = client;
            _settings = settings;
            InitializeUi();

            Load += async (s, e) => await RefreshAsync();
            _timer = new Timer { Interval = 5000 };
            _timer.Tick += async (s, e) => await RefreshAsync();
            _timer.Start();
            FormClosed += (s, e) => _timer?.Stop();
        }

        private void InitializeUi()
        {
            Text = "CollabMCP Users";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 380);

            lstUsers = new ListView
            {
                Location = new Point(12, 12),
                Size = new Size(396, 210),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lstUsers.Columns.Add("User", 120);
            lstUsers.Columns.Add("User ID", 160);
            lstUsers.Columns.Add("Color", 100);

            btnRefresh = new Button { Text = "Refresh", Location = new Point(12, 230), Size = new Size(80, 26) };
            btnRefresh.Click += async (s, e) => await RefreshAsync();

            lblStatus = new Label { Location = new Point(104, 234), Size = new Size(300, 18), ForeColor = Color.DimGray };

            var lblLegend = new Label { Text = "User colors:", Location = new Point(12, 268), AutoSize = true };

            // 15-color palette legend as small colored boxes
            var legendPanel = new Panel { Location = new Point(12, 290), Size = new Size(396, 78) };
            for (int i = 0; i < Palette.Length; i++)
            {
                var box = new Panel
                {
                    Location = new Point((i % 5) * 80, (i / 5) * 26),
                    Size = new Size(18, 18),
                    BackColor = ColorTranslator.FromHtml(Palette[i]),
                    Tag = Palette[i]
                };
                var tooltip = new ToolTip();
                tooltip.SetToolTip(box, Palette[i]);
                legendPanel.Controls.Add(box);

                if (i < 5)
                {
                    var num = new Label
                    {
                        Text = (i + 1).ToString(),
                        Location = new Point((i % 5) * 80 + 22, (i / 5) * 26),
                        AutoSize = true
                    };
                    legendPanel.Controls.Add(num);
                }
            }

            Controls.AddRange(new Control[] { lstUsers, btnRefresh, lblStatus, lblLegend, legendPanel });
        }

        private async System.Threading.Tasks.Task RefreshAsync()
        {
            try
            {
                var users = new List<string>();
                if (_client != null && _client.IsConnected)
                    users.AddRange(await _client.GetConnectedUsersAsync());
                if (!users.Contains(_settings.UserId))
                    users.Add(_settings.UserId);

                lstUsers.BeginUpdate();
                lstUsers.Items.Clear();
                foreach (var uid in users)
                {
                    var color = _client != null ? _client.GetUserColor(uid) : "#888888";
                    var item = new ListViewItem(uid == _settings.UserId ? _settings.Username + " (you)" : uid);
                    item.SubItems.Add(uid);
                    item.SubItems.Add(color);
                    lstUsers.Items.Add(item);
                }
                lstUsers.EndUpdate();

                lblStatus.Text = users.Count == 1
                    ? "Only you are connected"
                    : $"{users.Count} user(s) connected";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
        }
    }
}
