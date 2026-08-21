using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DerouteSharp.Collab;

namespace DerouteSharp
{
    /// <summary>
    /// Session management dialog: lists sessions available on the CollabMCP server
    /// (GET /api/sessions), allows joining an existing one or creating a new id.
    /// On OK the chosen session id is written into CollabSettings.SessionId.
    /// </summary>
    public class FormCollabSession : Form
    {
        private readonly CollabClient _client;
        private readonly CollabSettings _settings;
        private TextBox txtSessionId;
        private ListView lstSessions;
        private Button btnRefresh;
        private Button btnJoin;
        private Button btnCreate;
        private Button btnCopy;
        private Button btnOk;
        private Button btnCancel;
        private Label lblStatus;

        public FormCollabSession(CollabClient client, CollabSettings settings)
        {
            _client = client;
            _settings = settings;
            InitializeUi();
            Load += async (s, e) => await RefreshSessionsAsync();
        }

        private void InitializeUi()
        {
            Text = "CollabMCP Session";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 360);

            var lblSession = new Label { Text = "Session ID:", Location = new Point(12, 12), AutoSize = true };
            txtSessionId = new TextBox
            {
                Location = new Point(12, 30),
                Size = new Size(416, 23),
                Text = _settings.SessionId
            };

            var lblList = new Label { Text = "Sessions on server:", Location = new Point(12, 62), AutoSize = true };
            lstSessions = new ListView
            {
                Location = new Point(12, 82),
                Size = new Size(416, 190),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = true
            };
            lstSessions.Columns.Add("Session ID", 260);
            lstSessions.Columns.Add("Current", 150);
            lstSessions.SelectedIndexChanged += (s, e) =>
            {
                if (lstSessions.SelectedItems.Count > 0)
                    txtSessionId.Text = lstSessions.SelectedItems[0].Text;
            };

            btnRefresh = new Button { Text = "Refresh", Location = new Point(12, 280), Size = new Size(80, 26) };
            btnRefresh.Click += async (s, e) => await RefreshSessionsAsync();

            btnJoin = new Button { Text = "Join", Location = new Point(100, 280), Size = new Size(80, 26) };
            btnJoin.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            btnCreate = new Button { Text = "Create new", Location = new Point(188, 280), Size = new Size(90, 26) };
            btnCreate.Click += (s, e) =>
            {
                txtSessionId.Text = "sess-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            };

            btnCopy = new Button { Text = "Copy id", Location = new Point(286, 280), Size = new Size(70, 26) };
            btnCopy.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtSessionId.Text))
                    Clipboard.SetText(txtSessionId.Text);
            };

            lblStatus = new Label { Location = new Point(12, 314), Size = new Size(416, 18), ForeColor = Color.DimGray };

            btnOk = new Button { Text = "OK", Location = new Point(252, 322), Size = new Size(86, 28), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Location = new Point(346, 322), Size = new Size(82, 28), DialogResult = DialogResult.Cancel };
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblSession, txtSessionId, lblList, lstSessions,
                btnRefresh, btnJoin, btnCreate, btnCopy, lblStatus, btnOk, btnCancel
            });
        }

        private async System.Threading.Tasks.Task RefreshSessionsAsync()
        {
            lblStatus.Text = "Loading...";
            btnRefresh.Enabled = false;
            try
            {
                var sessions = await _client.GetAvailableSessionsAsync();
                lstSessions.BeginUpdate();
                lstSessions.Items.Clear();
                foreach (var id in sessions)
                {
                    var item = new ListViewItem(id);
                    item.SubItems.Add(id == _settings.SessionId ? "yes" : "");
                    lstSessions.Items.Add(item);
                }
                lstSessions.EndUpdate();
                lblStatus.Text = sessions.Count == 0
                    ? "No sessions on the server. Use 'Create new'."
                    : $"{sessions.Count} session(s)";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }
    }
}
