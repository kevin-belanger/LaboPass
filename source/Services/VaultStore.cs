using System.Text.Json;
using LaboPass.Models;

namespace LaboPass.Services;

public sealed class VaultStore
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public VaultStore()
    {
        VaultPath = Path.Combine(AppContext.BaseDirectory, "vault.json");
    }

    public string VaultPath { get; }
    public string? LastWarning { get; private set; }

    public List<VaultEntry> Load()
    {
        LastWarning = null;

        if (!File.Exists(VaultPath))
        {
            Save([]);
            return [];
        }

        try
        {
            string json = File.ReadAllText(VaultPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                LastWarning = "Le fichier vault.json est vide. LaboPass repart avec une liste vide.";
                Save([]);
                return [];
            }

            List<VaultEntry>? entries = JsonSerializer.Deserialize<List<VaultEntry>>(json, jsonOptions);
            return entries ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LastWarning = "Le fichier vault.json est invalide ou illisible. LaboPass repart avec une liste vide.";
            TryResetVaultFile();
            return [];
        }
    }

    public void Save(IReadOnlyCollection<VaultEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(VaultPath)!);

        // Future encryption can be added here before writing the serialized payload.
        string json = JsonSerializer.Serialize(entries.OrderBy(e => e.Label).ToList(), jsonOptions);
        File.WriteAllText(VaultPath, json);
    }

    private void TryResetVaultFile()
    {
        try
        {
            Save([]);
        }
        catch
        {
            // The user-facing warning from Load already explains that the vault could not be used.
        }
    }
}
