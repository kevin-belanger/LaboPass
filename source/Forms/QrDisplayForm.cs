using LaboPass.Services;

namespace LaboPass.Forms;

public sealed class QrDisplayForm : Form
{
    private readonly PictureBox pictureBox = new();
    private readonly Image qrImage;

    public QrDisplayForm(string label, string totpUri, QrService qrService)
    {
        Text = $"QR TOTP - {label}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(420, 500);
        Size = new Size(480, 560);
        Font = new Font("Segoe UI", 10F);
        Icon = AppIconProvider.GetApplicationIcon();

        qrImage = qrService.CreateQrImage(totpUri, 10);
        BuildInterface(totpUri);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pictureBox.Image = null;
            qrImage.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildInterface(string totpUri)
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        pictureBox.Dock = DockStyle.Fill;
        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBox.Image = qrImage;
        layout.Controls.Add(pictureBox, 0, 0);

        TextBox uriBox = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = totpUri
        };
        layout.Controls.Add(uriBox, 0, 1);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        Button closeButton = new() { Text = "Fermer", Width = 110, Height = 38 };
        Button copyButton = new() { Text = "Copier l'image", Width = 140, Height = 38 };
        closeButton.Click += (_, _) => Close();
        copyButton.Click += (_, _) => Clipboard.SetImage(qrImage);
        buttons.Controls.Add(closeButton);
        buttons.Controls.Add(copyButton);
        layout.Controls.Add(buttons, 0, 2);

        Controls.Add(layout);
    }
}
