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
        MinimumSize = new Size(440, 520);
        Size = new Size(500, 580);
        Font = new Font("Segoe UI", 10F);
        Icon = AppIconProvider.GetApplicationIcon();
        BackColor = UiTheme.AppBackColor;

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
            Padding = new Padding(18),
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        Label heading = new()
        {
            Text = "QR code TOTP",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold)
        };
        layout.Controls.Add(heading, 0, 0);

        Panel qrPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.SurfaceColor,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12)
        };
        qrPanel.Paint += (_, e) =>
        {
            using Pen pen = new(Color.FromArgb(210, 216, 224));
            e.Graphics.DrawRectangle(pen, 0, 0, qrPanel.Width - 1, qrPanel.Height - 1);
        };

        pictureBox.Dock = DockStyle.Fill;
        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBox.Image = qrImage;
        qrPanel.Controls.Add(pictureBox);
        layout.Controls.Add(qrPanel, 0, 1);

        TextBox uriBox = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = totpUri
        };
        UiTheme.StyleTextBox(uriBox);
        layout.Controls.Add(uriBox, 0, 2);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        Button closeButton = new() { Text = "Fermer", Width = 110, Height = 40 };
        Button copyButton = new() { Text = "Copier l'image", Width = 145, Height = 40 };
        UiTheme.StylePrimaryButton(closeButton);
        UiTheme.StyleSecondaryButton(copyButton);
        closeButton.Click += (_, _) => Close();
        copyButton.Click += (_, _) => Clipboard.SetImage(qrImage);
        buttons.Controls.Add(closeButton);
        buttons.Controls.Add(copyButton);
        layout.Controls.Add(buttons, 0, 3);

        Controls.Add(layout);
    }
}
