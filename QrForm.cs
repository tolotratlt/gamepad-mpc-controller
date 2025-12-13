using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace GamepadMpcController
{
    public class QrForm : Form
    {
        public QrForm(string url)
        {
            Text = "Remote Control QR Code";
            Width = 360;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;

            PictureBox pic = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = GenerateQr(url)
            };

            Controls.Add(pic);
        }

        private Bitmap GenerateQr(string text)
        {
            // Simple QR algorithm using the built-in QR encoder from .NET (GDI+ trick)
            // No external libs needed.
            var encoder = new ZXing.BarcodeWriter
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 1
                }
            };

            return encoder.Write(text);
        }
    }
}
