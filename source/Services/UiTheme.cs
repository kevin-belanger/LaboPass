namespace LaboPass.Services;

public static class UiTheme
{
    public static readonly Color AppBackColor = Color.FromArgb(246, 247, 249);
    public static readonly Color SurfaceColor = Color.White;
    public static readonly Color NoticeBackColor = Color.FromArgb(255, 248, 230);
    public static readonly Color NoticeBorderColor = Color.FromArgb(230, 209, 156);
    public static readonly Color NoticeTextColor = Color.FromArgb(83, 67, 32);
    public static readonly Color PrimaryColor = Color.FromArgb(38, 96, 150);
    public static readonly Color MutedTextColor = Color.FromArgb(85, 91, 99);
    public static readonly Color ErrorColor = Color.FromArgb(150, 45, 45);
    public static readonly Color GridHeaderColor = Color.FromArgb(232, 236, 242);
    public static readonly Color GridSelectionColor = Color.FromArgb(212, 228, 245);

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = PrimaryColor;
        button.ForeColor = Color.White;
        button.Font = new Font(button.Font, FontStyle.Bold);
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(190, 196, 204);
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = SurfaceColor;
        button.ForeColor = Color.FromArgb(35, 40, 46);
    }

    public static void StyleDangerButton(Button button)
    {
        StyleSecondaryButton(button);
        button.ForeColor = ErrorColor;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.BackgroundColor = SurfaceColor;
        grid.GridColor = Color.FromArgb(225, 229, 235);
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(35, 40, 46);
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(35, 40, 46);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        grid.ColumnHeadersHeight = 38;
        grid.RowTemplate.Height = 34;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
        grid.DefaultCellStyle.SelectionBackColor = GridSelectionColor;
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 32, 45);
    }
}
