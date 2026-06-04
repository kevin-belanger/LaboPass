using LaboPass.Forms;
using LaboPass.Models;
using LaboPass.Services;
using System.Diagnostics;

namespace LaboPass;

public sealed class MainForm : Form
{
    private readonly VaultStore vaultStore = new();
    private readonly TotpService totpService = new();
    private readonly QrService qrService = new();
    private readonly List<VaultEntry> entries;
    private readonly DataGridView grid = new();
    private readonly ContextMenuStrip gridContextMenu = new();
    private readonly System.Windows.Forms.Timer refreshTimer = new();
    private readonly System.Windows.Forms.Timer statusResetTimer = new();
    private readonly Label statusLabel = new();
    private readonly Button openVaultButton = new();
    private readonly Label versionLabel = new();

    public MainForm()
    {
        entries = vaultStore.Load();

        Text = "LaboPass";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1020, 620);
        Size = new Size(1180, 720);
        Font = new Font("Segoe UI", 10F);
        Icon = AppIconProvider.GetApplicationIcon();
        BackColor = UiTheme.AppBackColor;

        BuildInterface();
        RefreshGrid();

        refreshTimer.Interval = 1000;
        refreshTimer.Tick += (_, _) => RefreshTotpCells();
        refreshTimer.Start();

        statusResetTimer.Interval = 4000;
        statusResetTimer.Tick += (_, _) =>
        {
            statusResetTimer.Stop();
            UpdateStorageStatus();
        };

        Shown += MainForm_Shown;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            statusResetTimer.Stop();
            statusResetTimer.Dispose();
            gridContextMenu.Dispose();
        }

        base.Dispose(disposing);
    }

    private VaultEntry? SelectedEntry => grid.CurrentRow?.DataBoundItem as VaultEntry;

    private void BuildInterface()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

        root.Controls.Add(CreateNoticePanel(), 0, 0);

        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = true;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        UiTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Libellé", DataPropertyName = nameof(VaultEntry.Label), FillWeight = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nom d'utilisateur", DataPropertyName = nameof(VaultEntry.Username), FillWeight = 190 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Code MFA", Name = "TotpCode", FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Temps restant", Name = "TotpRemaining", FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Notes", DataPropertyName = nameof(VaultEntry.Notes), FillWeight = 180 });
        grid.CellDoubleClick += (_, _) => EditSelected();
        grid.CellMouseDown += Grid_CellMouseDown;
        BuildGridContextMenu();
        root.Controls.Add(grid, 0, 1);

        root.Controls.Add(CreateStatusPanel(), 0, 2);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        buttons.Controls.Add(MakeButton("Ajouter un identifiant", 190, (_, _) => AddEntry(), primary: true));
        buttons.Controls.Add(MakeButton("Modifier", 110, (_, _) => EditSelected()));
        buttons.Controls.Add(MakeButton("Copier utilisateur", 155, (_, _) => CopySelectedUsername()));
        buttons.Controls.Add(MakeButton("Copier mot de passe", 175, (_, _) => CopySelectedPassword()));
        buttons.Controls.Add(MakeButton("Copier code MFA", 155, (_, _) => CopySelectedMfaCode()));
        buttons.Controls.Add(MakeButton("Supprimer", 110, (_, _) => DeleteSelected(), danger: true));
        root.Controls.Add(buttons, 0, 3);

        versionLabel.Text = $"LaboPass v{AppInfo.AppVersion}";
        versionLabel.Dock = DockStyle.Fill;
        versionLabel.TextAlign = ContentAlignment.MiddleRight;
        versionLabel.ForeColor = UiTheme.MutedTextColor;
        versionLabel.Font = new Font(Font.FontFamily, 8.5F, FontStyle.Regular);
        versionLabel.Cursor = Cursors.Hand;
        versionLabel.Click += VersionLabel_Click;
        root.Controls.Add(versionLabel, 0, 4);

        Controls.Add(root);
    }

    private Control CreateStatusPanel()
    {
        TableLayoutPanel statusPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = Padding.Empty
        };
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = UiTheme.MutedTextColor;
        statusPanel.Controls.Add(statusLabel, 0, 0);

        openVaultButton.Text = "Ouvrir...";
        openVaultButton.Width = 88;
        openVaultButton.Height = 30;
        openVaultButton.Margin = new Padding(8, 5, 0, 5);
        openVaultButton.Anchor = AnchorStyles.Right;
        UiTheme.StyleSecondaryButton(openVaultButton);
        openVaultButton.Click += OpenVaultButton_Click;
        statusPanel.Controls.Add(openVaultButton, 1, 0);

        return statusPanel;
    }

    private void BuildGridContextMenu()
    {
        gridContextMenu.Items.Add("Modifier", null, (_, _) => EditSelected());
        gridContextMenu.Items.Add(new ToolStripSeparator());
        gridContextMenu.Items.Add("Copier utilisateur", null, (_, _) => CopySelectedUsername());
        gridContextMenu.Items.Add("Copier mot de passe", null, (_, _) => CopySelectedPassword());
        gridContextMenu.Items.Add("Copier code MFA", null, (_, _) => CopySelectedMfaCode());
        grid.ContextMenuStrip = gridContextMenu;
    }

    private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0)
        {
            return;
        }

        grid.ClearSelection();
        grid.Rows[e.RowIndex].Selected = true;
        grid.CurrentCell = grid.Rows[e.RowIndex].Cells[Math.Max(e.ColumnIndex, 0)];
    }

    private static Control CreateNoticePanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.NoticeBackColor,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 14)
        };
        panel.Paint += (_, e) =>
        {
            using Pen pen = new(UiTheme.NoticeBorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        Label title = new()
        {
            Text = "Utilisation prévue",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = UiTheme.NoticeTextColor
        };
        Label text = new()
        {
            Text = "LaboPass est conçu pour les environnements de test. Il permet de conserver des identifiants, des mots de passe et des codes MFA associés à des comptes de laboratoire. Les données sont stockées localement sans chiffrement; n'utilisez pas cette application pour des comptes personnels ou de production.",
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.NoticeTextColor,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(text);
        panel.Controls.Add(title);
        return panel;
    }

    private static Button MakeButton(string text, int width, EventHandler handler, bool primary = false, bool danger = false)
    {
        Button button = new()
        {
            Text = text,
            Width = width,
            Height = 40,
            Margin = new Padding(0, 5, 8, 5)
        };
        button.Click += handler;
        if (primary)
        {
            UiTheme.StylePrimaryButton(button);
        }
        else if (danger)
        {
            UiTheme.StyleDangerButton(button);
        }
        else
        {
            UiTheme.StyleSecondaryButton(button);
        }

        return button;
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(vaultStore.LastWarning))
        {
            MessageBox.Show(this, vaultStore.LastWarning, "vault.json", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        await WarnIfSystemClockLooksWrongAsync();
    }

    private async Task WarnIfSystemClockLooksWrongAsync()
    {
        if (!await SystemClockValidator.IsClockSkewedAsync())
        {
            return;
        }

        MessageBox.Show(
            this,
            "L'heure de cet ordinateur semble décalée.\n\n" +
            "Les codes MFA pourraient ne pas fonctionner.\n\n" +
            "Vérifiez que l’heure de Windows est exacte et synchronisée automatiquement",
            "Heure du PC incorrecte",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void RefreshGrid()
    {
        Guid? selectedId = SelectedEntry?.Id;
        grid.DataSource = null;
        grid.DataSource = entries.OrderBy(e => e.Label).ToList();
        RestoreSelection(selectedId);
        RefreshTotpCells();
        UpdateStorageStatus();
    }

    private void UpdateStorageStatus()
    {
        statusLabel.Text = $"{entries.Count} identifiant(s) - stockage: {vaultStore.VaultPath}";
    }

    private void OpenVaultButton_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Ouvrir un coffre LaboPass",
            Filter = "Fichiers JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = Path.GetDirectoryName(vaultStore.VaultPath)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!vaultStore.TrySwitchVault(dialog.FileName, out List<VaultEntry> loadedEntries, out string errorMessage))
        {
            MessageBox.Show(
                this,
                $"{errorMessage}\n\nLe coffre actuel reste ouvert.",
                "Coffre non chargé",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        entries.Clear();
        entries.AddRange(loadedEntries);
        RefreshGrid();
        ShowTemporaryStatus("Coffre chargé.");
    }

    private void VersionLabel_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppInfo.GitHubUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show(this, "Impossible d'ouvrir le lien GitHub.", "Lien non ouvert", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void RefreshTotpCells()
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.DataBoundItem is not VaultEntry entry)
            {
                continue;
            }

            TotpDisplay display = totpService.GetDisplay(entry.TotpUri);
            row.Cells["TotpCode"].Value = display.Code;
            row.Cells["TotpRemaining"].Value = display.Code.Length == 0 ? "" : $"{display.SecondsRemaining} s";
            row.DefaultCellStyle.ForeColor = display.IsValid ? SystemColors.ControlText : UiTheme.ErrorColor;
            row.Cells["TotpCode"].ToolTipText = display.Message;
        }
    }

    private void RestoreSelection(Guid? selectedId)
    {
        if (selectedId is null)
        {
            return;
        }

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.DataBoundItem is VaultEntry entry && entry.Id == selectedId)
            {
                row.Selected = true;
                grid.CurrentCell = row.Cells[0];
                return;
            }
        }
    }

    private void AddEntry()
    {
        using EntryForm form = new(qrService, totpService);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        entries.Add(form.Entry);
        SaveAndRefresh(form.Entry.Id);
    }

    private void EditSelected()
    {
        VaultEntry? selected = SelectedEntry;
        if (selected is null)
        {
            ShowSelectionRequired();
            return;
        }

        using EntryForm form = new(qrService, totpService, selected, SaveTotpChangeFromEditor);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        int index = entries.FindIndex(e => e.Id == selected.Id);
        if (index >= 0)
        {
            entries[index] = form.Entry;
        }

        SaveAndRefresh(form.Entry.Id);
    }

    private void DeleteSelected()
    {
        VaultEntry? selected = SelectedEntry;
        if (selected is null)
        {
            ShowSelectionRequired();
            return;
        }

        DialogResult confirm = MessageBox.Show(
            this,
            $"Supprimer l'identifiant \"{selected.Label}\"?",
            "Confirmer la suppression",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        entries.RemoveAll(e => e.Id == selected.Id);
        SaveAndRefresh(null);
    }

    private void CopySelectedUsername()
    {
        VaultEntry? selected = SelectedEntry;
        if (selected is null)
        {
            ShowSelectionRequired();
            return;
        }

        CopyTextToClipboard(selected.Username, "Nom d'utilisateur copié.", "Le nom d'utilisateur est vide.");
    }

    private void CopySelectedPassword()
    {
        VaultEntry? selected = SelectedEntry;
        if (selected is null)
        {
            ShowSelectionRequired();
            return;
        }

        CopyTextToClipboard(selected.Password, "Mot de passe copié.", "Le mot de passe est vide.");
    }

    private void CopySelectedMfaCode()
    {
        VaultEntry? selected = SelectedEntry;
        if (selected is null)
        {
            ShowSelectionRequired();
            return;
        }

        TotpDisplay display = totpService.GetDisplay(selected.TotpUri);
        if (string.IsNullOrWhiteSpace(selected.TotpUri))
        {
            MessageBox.Show(this, "Cette entrée ne contient pas d'URI TOTP.", "Code MFA non disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!display.IsValid || display.Code.Length == 0)
        {
            MessageBox.Show(this, display.Message.Length == 0 ? "L'URI TOTP est invalide." : display.Message, "Code MFA non disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Clipboard.SetText(display.Code);
        ShowTemporaryStatus("Code MFA copié.");
    }

    private void CopyTextToClipboard(string text, string successMessage, string emptyMessage)
    {
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show(this, emptyMessage, "Rien à copier", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(text);
        ShowTemporaryStatus(successMessage);
    }

    private void ShowTemporaryStatus(string message)
    {
        statusLabel.Text = message;
        statusResetTimer.Stop();
        statusResetTimer.Start();
    }

    private void ShowSelectedQr()
    {
        VaultEntry? selected = SelectedEntry;
        if (selected is null)
        {
            ShowSelectionRequired();
            return;
        }

        if (string.IsNullOrWhiteSpace(selected.TotpUri))
        {
            MessageBox.Show(this, "Cette entrée ne contient pas d'URI TOTP.", "QR non disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!selected.TotpUri.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "L'URI TOTP enregistrée ne commence pas par otpauth://.", "URI invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using QrDisplayForm form = new(selected.Label, selected.TotpUri, qrService, totpService);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                selected.TotpUri = form.TotpUri;
                selected.UpdatedAt = DateTime.Now;
                SaveAndRefresh(selected.Id);
            }
        }
        catch
        {
            MessageBox.Show(this, "Impossible de générer le QR code avec cette URI.", "Erreur QR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SaveAndRefresh(Guid? selectedId)
    {
        vaultStore.Save(entries);
        RefreshGrid();
        RestoreSelection(selectedId);
    }

    private void SaveTotpChangeFromEditor(VaultEntry updatedEntry)
    {
        int index = entries.FindIndex(e => e.Id == updatedEntry.Id);
        if (index >= 0)
        {
            entries[index] = updatedEntry;
            vaultStore.Save(entries);
            RefreshGrid();
            RestoreSelection(updatedEntry.Id);
        }
    }

    private void ShowSelectionRequired()
    {
        MessageBox.Show(this, "Sélectionne d'abord une entrée dans la liste.", "Aucune sélection", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
