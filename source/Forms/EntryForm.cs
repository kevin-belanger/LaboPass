using LaboPass.Models;
using LaboPass.Services;

namespace LaboPass.Forms;

public sealed class EntryForm : Form
{
    private readonly QrService qrService;
    private readonly TotpService totpService;
    private readonly TextBox labelTextBox = new();
    private readonly TextBox usernameTextBox = new();
    private readonly TextBox passwordTextBox = new();
    private readonly TextBox totpUriTextBox = new();
    private readonly TextBox notesTextBox = new();
    private readonly Label totpStatusLabel = new();
    private readonly System.Windows.Forms.Timer totpRefreshTimer = new();
    private bool passwordVisible;

    public EntryForm(QrService qrService, TotpService totpService, VaultEntry? entry = null)
    {
        this.qrService = qrService;
        this.totpService = totpService;
        Entry = entry is null ? new VaultEntry() : Clone(entry);

        Text = entry is null ? "Ajouter un identifiant" : "Modifier l'identifiant";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 660);
        Size = new Size(840, 720);
        Font = new Font("Segoe UI", 10F);
        Icon = AppIconProvider.GetApplicationIcon();

        BuildInterface();
        LoadEntry();

        totpRefreshTimer.Interval = 1000;
        totpRefreshTimer.Tick += (_, _) => RefreshTotpPreview();
        totpRefreshTimer.Start();
    }

    public VaultEntry Entry { get; private set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            totpRefreshTimer.Stop();
            totpRefreshTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildInterface()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "Libellé", labelTextBox);
        AddRow(layout, 1, "Nom d'utilisateur", CreateTextBoxWithButtons(usernameTextBox, [
            MakeSmallButton("Copier", (_, _) => CopyToClipboard(usernameTextBox.Text, "Nom d'utilisateur copié."))
        ]));

        passwordTextBox.UseSystemPasswordChar = true;
        AddRow(layout, 2, "Mot de passe", CreateTextBoxWithButtons(passwordTextBox, [
            MakeSmallButton("Voir", TogglePasswordVisibility),
            MakeSmallButton("Copier", (_, _) => CopyToClipboard(passwordTextBox.Text, "Mot de passe copié."))
        ]));

        AddRow(layout, 3, "URI TOTP complète", totpUriTextBox);
        totpUriTextBox.TextChanged += (_, _) => RefreshTotpPreview();

        FlowLayoutPanel qrButtons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        Button pasteQrButton = new()
        {
            Text = "Coller le QR depuis le presse-papiers",
            Height = 40,
            Width = 260
        };
        Button showQrButton = new()
        {
            Text = "Afficher le QR",
            Height = 40,
            Width = 140
        };
        pasteQrButton.Click += PasteQrButton_Click;
        showQrButton.Click += ShowQrButton_Click;
        qrButtons.Controls.Add(pasteQrButton);
        qrButtons.Controls.Add(showQrButton);
        AddRow(layout, 4, "", qrButtons);

        totpStatusLabel.Dock = DockStyle.Fill;
        totpStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        totpStatusLabel.Font = new Font(Font.FontFamily, 12F, FontStyle.Bold);
        AddRow(layout, 5, "MFA actuel", totpStatusLabel);

        notesTextBox.Multiline = true;
        notesTextBox.ScrollBars = ScrollBars.Vertical;
        notesTextBox.Height = 130;
        AddRow(layout, 6, "Notes", notesTextBox);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        Button saveButton = new() { Text = "Enregistrer", Width = 130, Height = 40 };
        Button cancelButton = new() { Text = "Annuler", Width = 110, Height = 40 };
        saveButton.Click += SaveButton_Click;
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.Controls.Add(new Label(), 0, 7);
        layout.Controls.Add(buttons, 1, 7);

        Controls.Add(layout);
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        Label labelControl = new()
        {
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 8, 0, 8);
        layout.Controls.Add(labelControl, 0, row);
        layout.Controls.Add(control, 1, row);
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
            Width = 76,
            Height = 28
        };
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
                "Aucun QR code otpauth:// valide n'a été trouvé dans le presse-papiers.",
                "QR introuvable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        totpUriTextBox.Text = uri;
    }

    private void ShowQrButton_Click(object? sender, EventArgs e)
    {
        string uri = totpUriTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(uri))
        {
            MessageBox.Show(this, "Aucune URI TOTP n'est configurée.", "QR non disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        TotpDisplay display = totpService.GetDisplay(uri);
        if (!display.IsValid || display.Code.Length == 0)
        {
            MessageBox.Show(this, display.Message.Length == 0 ? "L'URI TOTP est invalide." : display.Message, "URI TOTP invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using QrDisplayForm form = new(labelTextBox.Text.Trim(), uri, qrService);
            form.ShowDialog(this);
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
            button.Text = passwordVisible ? "Masquer" : "Voir";
        }
    }

    private void RefreshTotpPreview()
    {
        TotpDisplay display = totpService.GetDisplay(totpUriTextBox.Text);
        if (string.IsNullOrWhiteSpace(totpUriTextBox.Text))
        {
            totpStatusLabel.Text = "Aucun TOTP configuré";
            totpStatusLabel.ForeColor = SystemColors.ControlText;
            return;
        }

        if (!display.IsValid)
        {
            totpStatusLabel.Text = display.Message;
            totpStatusLabel.ForeColor = Color.DarkRed;
            return;
        }

        totpStatusLabel.Text = $"{display.Code} - {display.SecondsRemaining} s";
        totpStatusLabel.ForeColor = SystemColors.ControlText;
    }

    private void CopyToClipboard(string text, string statusMessage)
    {
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show(this, "Le champ est vide.", "Rien à copier", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(text);
        totpStatusLabel.Text = statusMessage;
        totpStatusLabel.ForeColor = SystemColors.ControlText;
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
