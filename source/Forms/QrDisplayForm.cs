using System.Reflection;
using LaboPass.Models;
using LaboPass.Services;

namespace LaboPass.Forms;

public sealed class QrDisplayForm : Form
{
    private const string NoQrResourceName = "LaboPass.Resources.no-qr.png";

    private readonly QrService qrService;
    private readonly TotpService totpService;
    private readonly PictureBox pictureBox = new();
    private readonly TextBox uriTextBox = new();
    private readonly Image noQrImage;
    private Image? qrImage;

    public QrDisplayForm(string label, string totpUri, QrService qrService, TotpService totpService)
    {
        this.qrService = qrService;
        this.totpService = totpService;
        noQrImage = LoadNoQrImage();
        TotpUri = totpUri;

        Text = $"QR TOTP - {label}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 620);
        Size = new Size(580, 680);
        Font = new Font("Segoe UI", 10F);
        Icon = AppIconProvider.GetApplicationIcon();
        BackColor = UiTheme.AppBackColor;

        BuildInterface();
        UpdateQrImage(totpUri);
        uriTextBox.TextChanged += (_, _) => UpdateQrImage(uriTextBox.Text.Trim());
    }

    public string TotpUri { get; private set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            pictureBox.Image = null;
            qrImage?.Dispose();
            noQrImage.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildInterface()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
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
        qrPanel.Controls.Add(pictureBox);
        layout.Controls.Add(qrPanel, 0, 1);

        uriTextBox.Dock = DockStyle.Fill;
        uriTextBox.Multiline = true;
        uriTextBox.ScrollBars = ScrollBars.Vertical;
        uriTextBox.Text = TotpUri;
        UiTheme.StyleTextBox(uriTextBox);
        layout.Controls.Add(uriTextBox, 0, 2);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        Button saveButton = new() { Text = "Enregistrer", Width = 120, Height = 40 };
        Button cancelButton = new() { Text = "Annuler", Width = 110, Height = 40 };
        Button deleteButton = new() { Text = "Supprimer le QR", Width = 145, Height = 40 };
        UiTheme.StylePrimaryButton(saveButton);
        UiTheme.StyleSecondaryButton(cancelButton);
        UiTheme.StyleDangerButton(deleteButton);
        saveButton.Click += SaveButton_Click;
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;
        deleteButton.Click += DeleteButton_Click;
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(deleteButton);
        layout.Controls.Add(buttons, 0, 3);

        Controls.Add(layout);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        string uri = uriTextBox.Text.Trim();
        TotpDisplay display = totpService.GetDisplay(uri);
        if (string.IsNullOrWhiteSpace(uri) || !display.IsValid || display.Code.Length == 0)
        {
            MessageBox.Show(
                this,
                display.Message.Length == 0 ? "L'URI TOTP est invalide." : display.Message,
                "URI TOTP invalide",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        TotpUri = uri;
        DialogResult = DialogResult.OK;
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        DialogResult confirm = MessageBox.Show(
            this,
            "Supprimer le QR et l'URI TOTP de cet identifiant?",
            "Supprimer le QR",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        TotpUri = "";
        DialogResult = DialogResult.OK;
    }

    private void UpdateQrImage(string totpUri)
    {
        qrImage?.Dispose();
        qrImage = null;

        TotpDisplay display = totpService.GetDisplay(totpUri);
        if (string.IsNullOrWhiteSpace(totpUri) || !display.IsValid || display.Code.Length == 0)
        {
            pictureBox.Image = noQrImage;
            return;
        }

        qrImage = qrService.CreateQrImage(totpUri, 10);
        pictureBox.Image = qrImage;
    }

    private static Image LoadNoQrImage()
    {
        Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(NoQrResourceName);
        if (stream is null)
        {
            return CreateFallbackNoQrImage();
        }

        using (stream)
        {
            return Image.FromStream(stream);
        }
    }

    private static Image CreateFallbackNoQrImage()
    {
        Bitmap bitmap = new(220, 220);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(242, 244, 247));
        using Pen pen = new(Color.FromArgb(210, 216, 224), 2);
        graphics.DrawRectangle(pen, 24, 24, 172, 172);
        using Font font = new("Segoe UI", 11F, FontStyle.Bold);
        TextRenderer.DrawText(graphics, "Aucun QR", font, new Rectangle(0, 92, 220, 30), UiTheme.MutedTextColor, TextFormatFlags.HorizontalCenter);
        return bitmap;
    }
}
