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

    public string VaultPath { get; private set; }
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

    public bool TrySwitchVault(string vaultPath, out List<VaultEntry> loadedEntries, out string errorMessage)
    {
        loadedEntries = [];
        errorMessage = "";

        try
        {
            if (!File.Exists(vaultPath))
            {
                errorMessage = "Le fichier sélectionné est introuvable.";
                return false;
            }

            string json = File.ReadAllText(vaultPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = "Le fichier sélectionné est vide.";
                return false;
            }

            List<VaultEntry>? entries = JsonSerializer.Deserialize<List<VaultEntry>>(json, jsonOptions);
            loadedEntries = entries ?? [];
            VaultPath = vaultPath;
            LastWarning = null;
            return true;
        }
        catch (JsonException)
        {
            errorMessage = "Le fichier sélectionné n'est pas un coffre JSON valide.";
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errorMessage = "Le fichier sélectionné est impossible à lire.";
            return false;
        }
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
