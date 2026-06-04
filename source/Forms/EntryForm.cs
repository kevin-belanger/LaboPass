using System.Reflection;
using LaboPass.Models;
using LaboPass.Services;

namespace LaboPass.Forms;

public sealed class EntryForm : Form
{
    private const string NoQrResourceName = "LaboPass.Resources.no-qr.png";

    private readonly QrService qrService;
    private readonly TotpService totpService;
    private readonly Action<VaultEntry>? saveTotpChange;
    private readonly TextBox labelTextBox = new();
    private readonly TextBox usernameTextBox = new();
    private readonly TextBox passwordTextBox = new();
    private readonly TextBox totpUriTextBox = new();
    private readonly TextBox notesTextBox = new();
    private readonly Label mfaCodeLabel = new();
    private readonly Label mfaRemainingLabel = new();
    private readonly Label copyStatusLabel = new();
    private readonly PictureBox qrPreviewBox = new();
    private readonly Button importQrButton;
    private readonly Button copyMfaButton;
    private readonly Image noQrImage;
    private readonly System.Windows.Forms.Timer totpRefreshTimer = new();
    private readonly System.Windows.Forms.Timer copyStatusTimer = new();
    private Image? qrPreviewImage;
    private string? currentQrUri;
    private string currentMfaCode = "";
    private bool passwordVisible;

    public EntryForm(QrService qrService, TotpService totpService, VaultEntry? entry = null, Action<VaultEntry>? saveTotpChange = null)
    {
        this.qrService = qrService;
        this.totpService = totpService;
        this.saveTotpChange = saveTotpChange;
        noQrImage = LoadNoQrImage();
        importQrButton = MakeActionButton("Importer le QR depuis le presse-papier", 250, PasteQrButton_Click);
        copyMfaButton = MakeSmallButton("Copier", (_, _) => CopyMfaCode());
        Entry = entry is null ? new VaultEntry() : Clone(entry);

        Text = entry is null ? "Ajouter un identifiant" : "Modifier l'identifiant";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(940, 680);
        Size = new Size(980, 720);
        Font = new Font("Segoe UI", 10F);
        Icon = AppIconProvider.GetApplicationIcon();
        BackColor = UiTheme.AppBackColor;

        totpUriTextBox.TextChanged += (_, _) => RefreshTotpPreview();
        BuildInterface();
        LoadEntry();

        totpRefreshTimer.Interval = 1000;
        totpRefreshTimer.Tick += (_, _) => RefreshTotpPreview();
        totpRefreshTimer.Start();

        copyStatusTimer.Interval = 4000;
        copyStatusTimer.Tick += (_, _) =>
        {
            copyStatusTimer.Stop();
            copyStatusLabel.Text = "";
        };
    }

    public VaultEntry Entry { get; private set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            totpRefreshTimer.Stop();
            totpRefreshTimer.Dispose();
            copyStatusTimer.Stop();
            copyStatusTimer.Dispose();
            qrPreviewImage?.Dispose();
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
            ColumnCount = 3,
            RowCount = 10
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));

        Label heading = new()
        {
            Text = "Informations de l'identifiant",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 40, 46)
        };
        layout.Controls.Add(heading, 0, 0);
        layout.SetColumnSpan(heading, 3);

        AddRow(layout, 1, "Libellé", labelTextBox);
        AddRow(layout, 2, "Nom d'utilisateur", CreateTextBoxWithButtons(usernameTextBox, [
            MakeSmallButton("Copier", (_, _) => CopyToClipboard(usernameTextBox.Text, "Nom d'utilisateur copié."))
        ]));

        passwordTextBox.UseSystemPasswordChar = true;
        AddRow(layout, 3, "Mot de passe", CreateTextBoxWithButtons(passwordTextBox, [
            MakeSmallButton("Afficher", TogglePasswordVisibility),
            MakeSmallButton("Copier", (_, _) => CopyToClipboard(passwordTextBox.Text, "Mot de passe copié."))
        ]));

        layout.Controls.Add(new Label
        {
            Text = "Code MFA",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.MutedTextColor
        }, 0, 4);
        layout.Controls.Add(CreateMfaPanel(), 1, 4);

        layout.Controls.Add(new Label(), 0, 5);
        layout.Controls.Add(importQrButton, 1, 5);

        Panel qrPanel = CreateQrPreviewPanel();
        layout.Controls.Add(qrPanel, 2, 4);
        layout.SetRowSpan(qrPanel, 3);

        notesTextBox.Multiline = true;
        notesTextBox.ScrollBars = ScrollBars.Vertical;
        notesTextBox.Height = 130;
        AddRow(layout, 7, "Notes", notesTextBox, columnSpan: 2);

        copyStatusLabel.Dock = DockStyle.Fill;
        copyStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        copyStatusLabel.ForeColor = UiTheme.MutedTextColor;
        copyStatusLabel.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular);
        copyStatusLabel.Margin = new Padding(0);
        layout.Controls.Add(new Label(), 0, 8);
        layout.Controls.Add(copyStatusLabel, 1, 8);
        layout.SetColumnSpan(copyStatusLabel, 2);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        Button saveButton = new() { Text = "Enregistrer", Width = 130, Height = 40 };
        Button cancelButton = new() { Text = "Annuler", Width = 110, Height = 40 };
        UiTheme.StylePrimaryButton(saveButton);
        UiTheme.StyleSecondaryButton(cancelButton);
        saveButton.Click += SaveButton_Click;
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.Controls.Add(new Label(), 0, 9);
        layout.Controls.Add(buttons, 1, 9);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
    }

    private Panel CreateMfaPanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.SurfaceColor,
            Padding = new Padding(10, 6, 10, 6),
            Margin = new Padding(0, 8, 0, 8)
        };
        panel.Paint += (_, e) =>
        {
            using Pen pen = new(Color.FromArgb(210, 216, 224));
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

        mfaCodeLabel.Dock = DockStyle.Fill;
        mfaCodeLabel.TextAlign = ContentAlignment.MiddleLeft;
        mfaCodeLabel.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);

        mfaRemainingLabel.Dock = DockStyle.Fill;
        mfaRemainingLabel.TextAlign = ContentAlignment.MiddleLeft;
        mfaRemainingLabel.ForeColor = UiTheme.MutedTextColor;
        mfaRemainingLabel.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular);

        copyMfaButton.Dock = DockStyle.Fill;
        copyMfaButton.Margin = new Padding(8, 3, 0, 3);
        content.Controls.Add(mfaCodeLabel, 0, 0);
        content.Controls.Add(copyMfaButton, 1, 0);
        content.SetRowSpan(copyMfaButton, 2);
        content.Controls.Add(mfaRemainingLabel, 0, 1);

        panel.Controls.Add(content);
        return panel;
    }

    private Panel CreateQrPreviewPanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.SurfaceColor,
            Padding = new Padding(12),
            Margin = new Padding(16, 8, 0, 8)
        };
        panel.Paint += (_, e) =>
        {
            using Pen pen = new(Color.FromArgb(210, 216, 224));
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        qrPreviewBox.Dock = DockStyle.Fill;
        qrPreviewBox.SizeMode = PictureBoxSizeMode.Zoom;
        qrPreviewBox.Image = noQrImage;
        qrPreviewBox.Cursor = Cursors.Default;
        qrPreviewBox.Click += QrPreviewBox_Click;
        panel.Controls.Add(qrPreviewBox);
        return panel;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control, int columnSpan = 1)
    {
        Label labelControl = new()
        {
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.MutedTextColor
        };

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 8, 0, 8);
        if (control is TextBox textBox)
        {
            UiTheme.StyleTextBox(textBox);
        }

        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
        if (columnSpan > 1)
        {
            layout.SetColumnSpan(control, columnSpan);
        }
    }

    private static Control CreateTextBoxWithButtons(TextBox textBox, IReadOnlyList<Button> buttons)
    {
        TableLayoutPanel panel = new()
        {
            ColumnCount = buttons.Count + 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        textBox.Dock = DockStyle.Fill;
        textBox.Margin = Padding.Empty;
        UiTheme.StyleTextBox(textBox);
        panel.Controls.Add(textBox, 0, 0);

        for (int i = 0; i < buttons.Count; i++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, buttons[i].Width + 8));
            buttons[i].Margin = new Padding(8, 0, 0, 0);
            panel.Controls.Add(buttons[i], i + 1, 0);
        }

        return panel;
    }

    private static Button MakeSmallButton(string text, EventHandler handler)
    {
        Button button = new()
        {
            Text = text,
            Width = text.Length > 6 ? 88 : 76,
            Height = 28
        };
        UiTheme.StyleSecondaryButton(button);
        button.Click += handler;
        return button;
    }

    private static Button MakeActionButton(string text, int width, EventHandler handler)
    {
        Button button = new()
        {
            Text = text,
            Width = width,
            Height = 38,
            Margin = new Padding(0, 5, 10, 5)
        };
        UiTheme.StyleSecondaryButton(button);
        button.Click += handler;
        return button;
    }

    private void LoadEntry()
    {
        labelTextBox.Text = Entry.Label;
        usernameTextBox.Text = Entry.Username;
        passwordTextBox.Text = Entry.Password;
        totpUriTextBox.Text = Entry.TotpUri;
        notesTextBox.Text = Entry.Notes;
        RefreshTotpPreview();
    }

    private void PasteQrButton_Click(object? sender, EventArgs e)
    {
        string? uri = qrService.ReadOtpAuthUriFromClipboard();
        if (uri is null)
        {
            MessageBox.Show(
                this,
                "Aucun QR code otpauth:// valide n'a été trouvé dans le presse-papier.",
                "QR introuvable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        totpUriTextBox.Text = uri;
    }

    private void QrPreviewBox_Click(object? sender, EventArgs e)
    {
        try
        {
            using QrDisplayForm form = new(labelTextBox.Text.Trim(), totpUriTextBox.Text.Trim(), qrService, totpService);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ApplyTotpUriChange(form.TotpUri);
            }
        }
        catch
        {
            MessageBox.Show(this, "Impossible de générer le QR code avec cette URI.", "Erreur QR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void TogglePasswordVisibility(object? sender, EventArgs e)
    {
        passwordVisible = !passwordVisible;
        passwordTextBox.UseSystemPasswordChar = !passwordVisible;

        if (sender is Button button)
        {
            button.Text = passwordVisible ? "Masquer" : "Afficher";
        }
    }

    private void RefreshTotpPreview()
    {
        string uri = totpUriTextBox.Text.Trim();
        TotpDisplay display = totpService.GetDisplay(uri);
        if (string.IsNullOrWhiteSpace(uri))
        {
            currentMfaCode = "";
            mfaCodeLabel.Text = "Aucun TOTP configuré";
            mfaCodeLabel.ForeColor = UiTheme.MutedTextColor;
            mfaRemainingLabel.Text = "";
            copyMfaButton.Enabled = false;
            importQrButton.Visible = true;
            UpdateQrPreview(null);
            return;
        }

        if (!display.IsValid)
        {
            currentMfaCode = "";
            mfaCodeLabel.Text = display.Message;
            mfaCodeLabel.ForeColor = UiTheme.ErrorColor;
            mfaRemainingLabel.Text = "";
            copyMfaButton.Enabled = false;
            importQrButton.Visible = true;
            UpdateQrPreview(null);
            return;
        }

        currentMfaCode = display.Code;
        mfaCodeLabel.Text = display.Code;
        mfaCodeLabel.ForeColor = SystemColors.ControlText;
        mfaRemainingLabel.Text = $"Renouvellement dans {display.SecondsRemaining} s";
        copyMfaButton.Enabled = true;
        importQrButton.Visible = false;
        UpdateQrPreview(uri);
    }

    private void UpdateQrPreview(string? validUri)
    {
        if (validUri == currentQrUri)
        {
            return;
        }

        currentQrUri = validUri;
        qrPreviewImage?.Dispose();
        qrPreviewImage = null;

        if (validUri is null)
        {
            qrPreviewBox.Image = noQrImage;
            qrPreviewBox.Cursor = Cursors.Hand;
            return;
        }

        try
        {
            qrPreviewImage = qrService.CreateQrImage(validUri, 6);
            qrPreviewBox.Image = qrPreviewImage;
            qrPreviewBox.Cursor = Cursors.Hand;
        }
        catch
        {
            currentQrUri = null;
            qrPreviewBox.Image = noQrImage;
            qrPreviewBox.Cursor = Cursors.Hand;
        }
    }

    private void CopyMfaCode()
    {
        if (string.IsNullOrEmpty(currentMfaCode))
        {
            MessageBox.Show(this, "Aucun code MFA valide à copier.", "Rien à copier", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(currentMfaCode);
        ShowCopyStatus("Code MFA copié.");
    }

    private void CopyToClipboard(string text, string statusMessage)
    {
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show(this, "Le champ est vide.", "Rien à copier", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(text);
        ShowCopyStatus(statusMessage);
    }

    private void ShowCopyStatus(string message)
    {
        copyStatusLabel.Text = message;
        copyStatusTimer.Stop();
        copyStatusTimer.Start();
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(labelTextBox.Text))
        {
            MessageBox.Show(this, "Le libellé est obligatoire.", "Information manquante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            labelTextBox.Focus();
            return;
        }

        Entry.Label = labelTextBox.Text.Trim();
        Entry.Username = usernameTextBox.Text.Trim();
        Entry.Password = passwordTextBox.Text;
        Entry.TotpUri = totpUriTextBox.Text.Trim();
        Entry.Notes = notesTextBox.Text;
        Entry.UpdatedAt = DateTime.Now;
        if (Entry.CreatedAt == default)
        {
            Entry.CreatedAt = Entry.UpdatedAt;
        }

        DialogResult = DialogResult.OK;
    }

    private void ApplyTotpUriChange(string totpUri)
    {
        totpUriTextBox.Text = totpUri;
        Entry.TotpUri = totpUri;
        Entry.UpdatedAt = DateTime.Now;
        saveTotpChange?.Invoke(Clone(Entry));
        RefreshTotpPreview();
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

    private static VaultEntry Clone(VaultEntry entry) => new()
    {
        Id = entry.Id,
        Label = entry.Label,
        Username = entry.Username,
        Password = entry.Password,
        TotpUri = entry.TotpUri,
        Notes = entry.Notes,
        CreatedAt = entry.CreatedAt,
        UpdatedAt = entry.UpdatedAt
    };
}
