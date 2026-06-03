using System.Security.Cryptography;
using System.Text;
using LaboPass.Models;

namespace LaboPass.Services;

public sealed class TotpService
{
    public TotpDisplay GetDisplay(string? totpUri)
    {
        if (string.IsNullOrWhiteSpace(totpUri))
        {
            return TotpDisplay.Empty;
        }

        if (!TryParse(totpUri, out TotpParameters? parameters, out string error))
        {
            return TotpDisplay.Invalid(error);
        }

        try
        {
            TotpParameters parsed = parameters!;
            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long counter = unixTime / parsed.Period;
            int remaining = parsed.Period - (int)(unixTime % parsed.Period);
            string code = GenerateCode(parsed.Secret, counter, parsed.Digits, parsed.Algorithm);
            return new TotpDisplay(code, remaining, true, "");
        }
        catch
        {
            return TotpDisplay.Invalid("Impossible de générer le code MFA avec cette URI.");
        }
    }

    private static bool TryParse(string totpUri, out TotpParameters? parameters, out string error)
    {
        parameters = null;
        error = "";

        if (!Uri.TryCreate(totpUri, UriKind.Absolute, out Uri? uri) ||
            !uri.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase))
        {
            error = "L'URI doit commencer par otpauth://totp/.";
            return false;
        }

        Dictionary<string, string> query = ParseQuery(uri.Query);
        if (!query.TryGetValue("secret", out string? secretText) || string.IsNullOrWhiteSpace(secretText))
        {
            error = "Le paramètre secret est absent.";
            return false;
        }

        byte[] secret;
        try
        {
            secret = DecodeBase32(secretText);
        }
        catch
        {
            error = "Le secret TOTP n'est pas un Base32 valide.";
            return false;
        }

        int period = ReadPositiveInt(query, "period", 30);
        int digits = ReadPositiveInt(query, "digits", 6);
        string algorithm = query.TryGetValue("algorithm", out string? value) ? value.ToUpperInvariant() : "SHA1";

        if (digits is < 6 or > 8)
        {
            error = "Le nombre de chiffres TOTP doit être entre 6 et 8.";
            return false;
        }

        if (algorithm is not ("SHA1" or "SHA256" or "SHA512"))
        {
            error = "L'algorithme TOTP doit être SHA1, SHA256 ou SHA512.";
            return false;
        }

        parameters = new TotpParameters(secret, period, digits, algorithm);
        return true;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        string cleanQuery = query.TrimStart('?');
        if (cleanQuery.Length == 0)
        {
            return values;
        }

        foreach (string pair in cleanQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(parts[0].Replace("+", " "));
            string value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace("+", " ")) : "";
            values[key] = value;
        }

        return values;
    }

    private static int ReadPositiveInt(Dictionary<string, string> query, string key, int defaultValue)
    {
        return query.TryGetValue(key, out string? value) &&
            int.TryParse(value, out int parsed) &&
            parsed > 0
                ? parsed
                : defaultValue;
    }

    private static string GenerateCode(byte[] secret, long counter, int digits, string algorithm)
    {
        byte[] counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using HMAC hmac = algorithm switch
        {
            "SHA256" => new HMACSHA256(secret),
            "SHA512" => new HMACSHA512(secret),
            _ => new HMACSHA1(secret)
        };

        byte[] hash = hmac.ComputeHash(counterBytes);
        int offset = hash[^1] & 0x0f;
        int binary =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);

        int divisor = (int)Math.Pow(10, digits);
        int otp = binary % divisor;
        return otp.ToString(new string('0', digits));
    }

    private static byte[] DecodeBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        string clean = input.Trim().Replace(" ", "").TrimEnd('=').ToUpperInvariant();
        List<byte> bytes = [];
        int buffer = 0;
        int bitsLeft = 0;

        foreach (char c in clean)
        {
            int value = alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException("Invalid Base32 character.");
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                bitsLeft -= 8;
            }
        }

        return bytes.Count == 0 ? throw new FormatException("Empty Base32 value.") : bytes.ToArray();
    }

    private sealed record TotpParameters(byte[] Secret, int Period, int Digits, string Algorithm);
}
