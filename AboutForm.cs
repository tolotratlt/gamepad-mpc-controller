using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GamepadMpcController
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "À propos";
            Width = 300;
            Height = 180;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var lblTitle = new Label
            {
                Text = "By TLT",
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                AutoSize = true,
                Left = 20,
                Top = 20
            };

            var lblVersion = new Label
            {
                Text = "Version: 1.0.0",
                AutoSize = true,
                Left = 20,
                Top = 50
            };

            var lblYear = new Label
            {
                Text = "2025",
                AutoSize = true,
                Left = 20,
                Top = 70
            };

            var linkGithub = new LinkLabel
            {
                Text = "https://github.com/tolotratlt",
                AutoSize = true,
                Left = 20,
                Top = 100
            };

            linkGithub.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/tolotratlt",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    MessageBox.Show("Impossible d'ouvrir le lien.", "Erreur");
                }
            };

            Controls.Add(lblTitle);
            Controls.Add(lblVersion);
            Controls.Add(lblYear);
            Controls.Add(linkGithub);
        }
    }
}
