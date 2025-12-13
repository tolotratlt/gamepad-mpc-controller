using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GamepadMpcController
{
    public class MainForm : Form
    {
        private readonly GamepadManager gamepad;
        private readonly MediaController media;
        private readonly GamepadMapping mapping;

        private readonly Timer timer;
        private readonly DataGridView grid;
        private readonly Button btnLearn;
        private readonly Label lblStatus;

        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;

        private BindingList<MappingEntry> entries;
        private bool[] previousButtons;

        private bool learningMode;
        private MappingEntry learningEntry;

        private GamepadState previousState;

        private RemoteHttpServer remoteServer;
        private CheckBox chkRemote;
        private Label lblRemoteStatus;

        private Button btnShowQr;

        private NumericUpDown numPort;


        public MainForm()
        {
            Program.FormRef = this;

            Text = "Gamepad MPC Controller";
            Width = 750;
            Height = 580;
            MinimumSize = new Size(750, 450);
            StartPosition = FormStartPosition.CenterScreen;

            // Icone personnalisee
            this.Icon = Properties.Resources.window_icon;

            // Grille principale redimensionnable avec marges
            grid = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(710, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            var colAction = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ActionName",
                HeaderText = "Action",
                ReadOnly = true,
                Width = 220,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            var colButton = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ButtonIndex",
                HeaderText = "Button",
                Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            var colAxis = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AxisName",
                HeaderText = "Axis",
                Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            var colPOV = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "POVDisplay",
                HeaderText = "POV",
                Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            grid.Columns.Add(colAction);
            grid.Columns.Add(colButton);
            grid.Columns.Add(colAxis);
            grid.Columns.Add(colPOV);

            // Panneau de configuration - Section 1: Button Mapping
            var mappingPanel = new GroupBox
            {
                Text = "Button Mapping",
                Location = new Point(10, 370),
                Size = new Size(710, 70),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            btnLearn = new Button
            {
                Text = "Learn Mapping",
                Location = new Point(15, 25),
                Size = new Size(150, 32),
                Font = new Font(Font.FontFamily, 9F, FontStyle.Regular)
            };
            btnLearn.Click += BtnLearn_Click;

            lblStatus = new Label
            {
                Location = new Point(180, 25),
                Size = new Size(510, 32),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Ready - Select an action and click 'Learn Mapping'",
                Font = new Font(Font.FontFamily, 9F, FontStyle.Regular),
                AutoEllipsis = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            mappingPanel.Controls.Add(btnLearn);
            mappingPanel.Controls.Add(lblStatus);

            // Panneau de configuration - Section 2: Remote Control
            var remotePanel = new GroupBox
            {
                Text = "Remote Control",
                Location = new Point(10, 450),
                Size = new Size(710, 85),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Ligne 1: Checkbox Enable
            chkRemote = new CheckBox
            {
                Text = "Enable Remote Control",
                Location = new Point(15, 25),
                Size = new Size(180, 25),
                Font = new Font(Font.FontFamily, 9F, FontStyle.Regular)
            };
            chkRemote.CheckedChanged += ChkRemote_CheckedChanged;

            lblRemoteStatus = new Label
            {
                Location = new Point(210, 25),
                Size = new Size(480, 25),
                Text = "Remote: Disabled",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Regular),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            // Ligne 2: Port + QR Code
            var lblPort = new Label
            {
                Text = "Port:",
                Location = new Point(15, 55),
                Size = new Size(50, 25),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Regular)
            };

            numPort = new NumericUpDown
            {
                Name = "numPort",
                Minimum = 1024,
                Maximum = 65535,
                Value = Properties.Settings.Default.RemotePort,
                Location = new Point(70, 53),
                Size = new Size(100, 25),
                Enabled = true,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Regular)
            };
            numPort.ValueChanged += NumPort_ValueChanged;

            btnShowQr = new Button
            {
                Text = "Show QR Code",
                Location = new Point(560, 50),
                Size = new Size(130, 28),
                Enabled = false,
                Anchor = AnchorStyles.Right,
                Font = new Font(Font.FontFamily, 9F, FontStyle.Regular)
            };
            btnShowQr.Click += BtnShowQr_Click;

            remotePanel.Controls.Add(chkRemote);
            remotePanel.Controls.Add(lblRemoteStatus);
            remotePanel.Controls.Add(lblPort);
            remotePanel.Controls.Add(numPort);
            remotePanel.Controls.Add(btnShowQr);

            Controls.Add(grid);
            Controls.Add(mappingPanel);
            Controls.Add(remotePanel);

            // Menu de la zone de notification
            trayMenu = new ContextMenuStrip();
            var itemOpen = new ToolStripMenuItem("Open");
            itemOpen.Click += (s, e) => RestoreFromTray();
            var itemExit = new ToolStripMenuItem("Quit application");
            itemExit.Click += (s, e) =>
            {
                trayIcon.Visible = false;
                Application.Exit();
            };
            var itemAbout = new ToolStripMenuItem("About");
            itemAbout.Click += (s, e) =>
            {
                using (var about = new AboutForm())
                {
                    about.ShowDialog(this);
                }
            };

            trayMenu.Items.Add(itemOpen);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(itemAbout);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(itemExit);

            trayIcon = new NotifyIcon
            {
                Text = "Gamepad MPC Controller",
                Visible = true,
                ContextMenuStrip = trayMenu,
                Icon = Properties.Resources.gamepad_20_89419,
            };
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            // Evenements fenetre
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;

            // Initialisation logique
            gamepad = new GamepadManager();
            media = new MediaController();
            mapping = GamepadMapping.CreateDefault();

            remoteServer = new RemoteHttpServer();

            entries = new BindingList<MappingEntry>(mapping.CreateEntries().ToList());
            grid.DataSource = entries;

            timer = new Timer();
            timer.Interval = 20;
            timer.Tick += UpdateLoop;
            timer.Start();

            // Gestion clavier: Suppr = effacer bouton, Echap = annuler apprentissage
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void BtnLearn_Click(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null)
            {
                lblStatus.Text = "Select a line action before mapping";
                return;
            }

            var entry = grid.CurrentRow.DataBoundItem as MappingEntry;
            if (entry == null)
            {
                lblStatus.Text = "Invalid line action";
                return;
            }

            learningMode = true;
            learningEntry = entry;
            lblStatus.Text = "Press a controller's button for this action";
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (learningMode && e.KeyCode == Keys.Escape)
            {
                learningMode = false;
                learningEntry = null;
                lblStatus.Text = "Mapping cancelled";
            }

            if (e.KeyCode == Keys.Delete)
            {
                if (grid.CurrentRow != null)
                {
                    var entry = grid.CurrentRow.DataBoundItem as MappingEntry;
                    if (entry != null)
                    {
                        entry.ButtonIndex = -1;       // supprime l’assignation
                        lblStatus.Text = "Button action deleted " + entry.ActionName;
                        grid.Refresh();
                    }
                }
            }
        }

        private void UpdateLoop(object sender, EventArgs e)
        {
            var state = gamepad.GetState();
            if (state == null)
            {
                lblStatus.Text = "No controller detected";
                //previousButtons = null;
                //previousState = null;
                return;
            }

            if (previousState == null)
            {
                previousState = state;
                previousButtons = state.Buttons != null ? (bool[])state.Buttons.Clone() : null;
                return;
            }

            if (learningMode)
            {
                LearnFromState(state);
            }
            else
            {
                ApplyMapping(state);
            }

            if (state.Buttons != null)
            {
                previousButtons = (bool[])state.Buttons.Clone();
            }

            previousState = state;
        }

        private void LearnFromState(GamepadState state)
        {
            if (state.Buttons == null || previousButtons == null)
                return;

            for (int i = 0; i < state.Buttons.Length; i++)
            {
                bool wasDown = previousButtons != null && i < previousButtons.Length && previousButtons[i];
                bool isDown = state.Buttons[i];

                if (!wasDown && isDown)
                {
                    // attribuer le bouton à l’entrée courante
                    learningEntry.ButtonIndex = i;

                    // enlever ce bouton des autres entrées
                    foreach (var other in entries)
                    {
                        if (other != learningEntry && other.Rule != null)
                        {
                            if (other.Rule.ButtonIndex == i)
                            {
                                other.Rule.ButtonIndex = -1;
                            }
                        }
                    }

                    learningMode = false;
                    learningEntry = null;
                    lblStatus.Text = "Button mapped: index " + i;
                    grid.Refresh();
                    return;
                }

            }

            // apprentissage d'un axe (si MappingEntry contient AxisRule)
            if (learningEntry.AxisRule != null)
            {
                DetectAxisChange(state);
            }

            if (learningEntry.PovRule != null)
            {
                int pov = state.POV;
                if (pov != -1 && (previousState.POV == -1))
                {
                    learningEntry.PovRule.Direction = pov;
                    learningMode = false;
                    lblStatus.Text = "POV assigned: " + pov;
                    grid.Refresh();
                    return;
                }
            }

        }

        private void ApplyMapping(GamepadState state)
        {
            if (state.Buttons == null)
                return;

            TryAction(mapping.PlayPause, state, () => media.PlayPause());
            TryAction(mapping.SeekForward, state, () => media.SeekForward());
            TryAction(mapping.SeekBackward, state, () => media.SeekBackward());
            TryAction(mapping.Fullscreen, state, () => media.Fullscreen());
            TryAction(mapping.Next, state, () => media.Next());
            TryAction(mapping.Previous, state, () => media.Previous());
            TryAction(mapping.VolumeUp, state, () => media.VolumeUp());
            TryAction(mapping.VolumeDown, state, () => media.VolumeDown());
            TryAxis(mapping.SeekForwardAxis, state, () => media.SeekForward());
            TryAxis(mapping.SeekBackwardAxis, state, () => media.SeekBackward());
            TryPOV(mapping.SeekForwardPOV, state, () => media.SeekForward());
            TryPOV(mapping.SeekBackwardPOV, state, () => media.SeekBackward());
            TryPOV(mapping.VolumeUpPOV, state, () => media.VolumeUp());
            TryPOV(mapping.VolumeDownPOV, state, () => media.VolumeDown());
            TryAction(mapping.StopAndMinimize, state, () => media.StopAndMinimize());

        }

        private void TryAction(MappingRule rule, GamepadState state, Action action)
        {
            if (rule == null)
                return;

            if (!rule.IsPressed(state))
                return;

            int idx = rule.ButtonIndex;
            bool wasDown = previousButtons != null && idx >= 0 && idx < previousButtons.Length && previousButtons[idx];

            if (!wasDown)
            {
                action();
                lblStatus.Text = "Action: " + rule.Name;
            }
        }

        private void TryAxis(AxisRule rule, GamepadState s, Action action)
        {
            if (rule == null)
                return;

            if (rule.IsActivated(s))
            {
                action();
                lblStatus.Text = "Action: " + rule.Name;
            }
        }

        private void DetectAxisChange(GamepadState s)
        {
            // sticks : variation > 2000
            if (Math.Abs(s.X - previousState.X) > 2000)
            {
                learningEntry.AxisRule.AxisName = "X";
                learningMode = false;
                lblStatus.Text = "X axis assigned";
                grid.Refresh();
                return;
            }

            if (Math.Abs(s.Y - previousState.Y) > 2000)
            {
                learningEntry.AxisRule.AxisName = "Y";
                learningMode = false;
                lblStatus.Text = "Y axis assigned";
                grid.Refresh();
                return;
            }

            if (Math.Abs(s.Z - previousState.Z) > 2000)
            {
                learningEntry.AxisRule.AxisName = "Z";
                learningMode = false;
                lblStatus.Text = "Z axis assigned";
                grid.Refresh();
                return;
            }

            if (Math.Abs(s.Rx - previousState.Rx) > 2000)
            {
                learningEntry.AxisRule.AxisName = "Rx";
                learningMode = false;
                lblStatus.Text = "Rx axis assigned";
                grid.Refresh();
                return;
            }

            if (Math.Abs(s.Ry - previousState.Ry) > 2000)
            {
                learningEntry.AxisRule.AxisName = "Ry";
                learningMode = false;
                lblStatus.Text = "Ry axis assigned";
                grid.Refresh();
                return;
            }

            if (Math.Abs(s.Rz - previousState.Rz) > 2000)
            {
                learningEntry.AxisRule.AxisName = "Rz";
                learningMode = false;
                lblStatus.Text = "Rz axis assigned";
                grid.Refresh();
                return;
            }

            // XINPUT TRIGGERS (Ry, Rz)
            // Variation > 10 suffit
            if (Math.Abs(s.Ry - previousState.Ry) > 10)
            {
                learningEntry.AxisRule.AxisName = "Ry"; // Left Trigger (0–255)
                learningMode = false;
                lblStatus.Text = "Left Trigger assigned";
                grid.Refresh();
                return;
            }

            if (Math.Abs(s.Rz - previousState.Rz) > 10)
            {
                learningEntry.AxisRule.AxisName = "Rz"; // Right Trigger (0–255)
                learningMode = false;
                lblStatus.Text = "Right Trigger assigned";
                grid.Refresh();
                return;
            }
        }



        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                remoteServer?.Stop();
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                lblStatus.Text = "The application continues in the notification bar";
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = false;
            }
        }

        private void ChkRemote_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRemote.Checked)
            {
                int port = Properties.Settings.Default.RemotePort;
                if (remoteServer.Start(port))
                {
                    lblRemoteStatus.Text = $"Remote: ON ({remoteServer.LocalIP}:{remoteServer.Port})";
                    btnShowQr.Enabled = true;
                    numPort.Enabled = false;
                }
                else
                {
                    lblRemoteStatus.Text = "Remote: FAILED (Port already in use?)";

                    // éviter l'événement boucle : on décoche mais sans relancer ce code
                    chkRemote.CheckedChanged -= ChkRemote_CheckedChanged;
                    chkRemote.Checked = false;
                    chkRemote.CheckedChanged += ChkRemote_CheckedChanged;

                    btnShowQr.Enabled = false;
                    numPort.Enabled = true;
                }
            }
            else
            {
                remoteServer.Stop();
                lblRemoteStatus.Text = "Remote: Disabled";
                btnShowQr.Enabled = false;
                numPort.Enabled = true;
            }
        }

        private void BtnShowQr_Click(object sender, EventArgs e)
        {
            if (!chkRemote.Checked)
                return;

            string url = $"http://{remoteServer.LocalIP}:{remoteServer.Port}/";

            using (var qf = new QrForm(url))
                qf.ShowDialog(this);
        }

        private void NumPort_ValueChanged(object sender, EventArgs e)
        {
            var num = sender as NumericUpDown;
            int newPort = (int)num.Value;

            // Sauvegarde immédiate
            Properties.Settings.Default.RemotePort = newPort;
            Properties.Settings.Default.Save();

            // Si remote OFF → rien à faire
            if (!chkRemote.Checked)
                return;

            // Remote ON → on doit redémarrer
            remoteServer.Stop();

            if (remoteServer.Start(newPort))
            {
                lblRemoteStatus.Text = $"Remote: ON ({remoteServer.LocalIP}:{newPort})";
            }
            else
            {
                lblRemoteStatus.Text = "Remote: FAILED (Port already in use?)";

                // rollback à l’ancien port
                num.ValueChanged -= NumPort_ValueChanged;
                num.Value = remoteServer.Port;
                num.ValueChanged += NumPort_ValueChanged;
            }
        }



        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            Activate();
        }

        private void TryPOV(POVRule rule, GamepadState s, Action action)
        {
            if (rule == null)
                return;

            if (rule.IsActivated(s))
            {
                action();
                lblStatus.Text = "Action: " + rule.Name;
            }
        }

        public void PlayPauseRemote() => media.PlayPause();
        public void NextRemote() => media.Next();
        public void PreviousRemote() => media.Previous();
        public void SeekForwardRemote() => media.SeekForward();
        public void SeekBackwardRemote() => media.SeekBackward();
        public void VolumeUpRemote() => media.VolumeUp();
        public void VolumeDownRemote() => media.VolumeDown();
        public void FullscreenRemote() => media.Fullscreen();
        public void StopRemote() => media.Stop();


    }


}
