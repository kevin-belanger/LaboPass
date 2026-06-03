namespace LaboPass.Models;

public sealed record TotpDisplay(string Code, int SecondsRemaining, bool IsValid, string Message)
{
    public static TotpDisplay Empty { get; } = new("", 0, true, "");
    public static TotpDisplay Invalid(string message) => new("URI invalide", 0, false, message);
}
