using System;
using System.Drawing;
using System.Windows.Forms;
using DerouteSharp.Collab;

namespace DerouteSharp.Collab.UI
{
    public partial class CollabSettingsForm : Form
    {
        private readonly CollabSettings _settings;
        private CheckBox enabledCheck;
        private TextBox serverUrlBox;
        private TextBox apiKeyBox;
        private TextBox userIdBox;
        private TextBox usernameBox;
        private Label statusLabel;

        public CollabSettingsForm(CollabSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "CollabMCP Settings";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(450, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AcceptButton = okBtn;
            CancelButton = cancelBtn;

            var y = 20;
            var labelWidth = 120;
            var controlLeft = 130;

            var titleLbl = new Label
            {
                Text = "CollabMCP Collaboration Settings",
                Location = new Point(20, y),
                Font = new Font(Font, FontStyle.Bold),
                Size = new Size(400, 20)
            };
            y += 30;

            enabledCheck = new CheckBox
            {
                Text = "Enable collaboration",
                Location = new Point(20, y),
                AutoSize = true,
                Checked = _settings.Enabled
            };
            y += 30;

            AddLabel(y, "Server URL:", labelWidth);
            serverUrlBox = new TextBox
            {
                Location = new Point(controlLeft, y - 3),
                Width = 280,
                Text = _settings.ServerUrl
            };
            y += 28;

            AddLabel(y, "API Key:", labelWidth);
            apiKeyBox = new TextBox
            {
                Location = new Point(controlLeft, y - 3),
                Width = 280,
                Text = _settings.ApiKey,
                PasswordChar = '*'
            };
            y += 28;

            AddLabel(y, "User ID:", labelWidth);
            userIdBox = new TextBox
            {
                Location = new Point(controlLeft, y - 3),
                Width = 280,
                Text = _settings.UserId
            };
            y += 28;

            AddLabel(y, "Display Name:", labelWidth);
            usernameBox = new TextBox
            {
                Location = new Point(controlLeft, y - 3),
                Width = 280,
                Text = _settings.Username
            };
            y += 40;

            statusLabel = new Label
            {
                Text = "",
                Location = new Point(20, y),
                Size = new Size(400, 20),
                ForeColor = Color.Green
            };
            y += 30;

            okBtn = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(200, y),
                Size = new Size(75, 25)
            };
            this.AcceptButton = okBtn;
            okBtn.Click += OkBtn_Click;

            cancelBtn = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(285, y),
                Size = new Size(75, 25)
            };

            Controls.AddRange(new Control[] { titleLbl, enabledCheck, okBtn, cancelBtn, statusLabel });
        }

        private void AddLabel(int y, string text, int width)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(20, y),
                Size = new Size(width, 20),
                AutoSize = false
            };
            Controls.Add(lbl);
        }

        private void LoadSettings()
        {
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            _settings.Enabled = enabledCheck.Checked;
            _settings.ServerUrl = serverUrlBox.Text.Trim();
            _settings.ApiKey = apiKeyBox.Text.Trim();
            _settings.UserId = userIdBox.Text.Trim();
            _settings.Username = usernameBox.Text.Trim();

            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                statusLabel.Text = "API Key is required for collaboration";
                statusLabel.ForeColor = Color.Red;
                return;
            }

            if (string.IsNullOrEmpty(_settings.ServerUrl))
            {
                statusLabel.Text = "Server URL is required";
                statusLabel.ForeColor = Color.Red;
                return;
            }

            statusLabel.Text = "Settings saved";
            statusLabel.ForeColor = Color.Green;

            System.Threading.Thread.Sleep(500);
            DialogResult = DialogResult.OK;
            Close();
        }

        private Button okBtn;
        private Button cancelBtn;
    }
}
