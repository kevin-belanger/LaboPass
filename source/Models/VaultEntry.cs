namespace LaboPass.Models;

public sealed class VaultEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string TotpUri { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
