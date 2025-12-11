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

        public MainForm()
        {
            Text = "Gamepad MPC Controller";
            Width = 600;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;

            // icone personnalisee
            this.Icon = Properties.Resources.gamepad_230053;

            // grille principale redimensionnable
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };

            var colAction = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ActionName",
                HeaderText = "Action",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            var colButton = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ButtonIndex",
                HeaderText = "Button",
                Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            grid.Columns.Add(colAction);
            grid.Columns.Add(colButton);

            var colAxis = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "AxisName",
                HeaderText = "Axis",
                Width = 80
            };

            grid.Columns.Add(colAxis);


            var colPOV = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "POVDisplay",
                HeaderText = "POV",
                Width = 80
            };

            grid.Columns.Add(colPOV);

            // panneau inferieur pour bouton + label
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };

            btnLearn = new Button
            {
                Text = "Learn from the next action button",
                Left = 10,
                Top = 10,
                Width = 280,
                Height = 30
            };
            btnLearn.Click += BtnLearn_Click;

            lblStatus = new Label
            {
                Left = 300,
                Top = 15,
                Width = 260,
                Height = 30,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Text = "Ready"
            };

            bottomPanel.Controls.Add(btnLearn);
            bottomPanel.Controls.Add(lblStatus);

            Controls.Add(grid);
            Controls.Add(bottomPanel);

            // menu de la zone de notification
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

            // evenements fenetre
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;

            // initialisation logique
            gamepad = new GamepadManager();
            media = new MediaController();
            mapping = GamepadMapping.CreateDefault();

            entries = new BindingList<MappingEntry>(mapping.CreateEntries().ToList());
            grid.DataSource = entries;

            timer = new Timer();
            timer.Interval = 20;
            timer.Tick += UpdateLoop;
            timer.Start();

            // gestion clavier: Suppr = effacer bouton, Echap = annuler apprentissage
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

    }


}
