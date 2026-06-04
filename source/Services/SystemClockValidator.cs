namespace LaboPass.Services;

public static class SystemClockValidator
{
    private static readonly Uri TimeReferenceUri = new("https://www.microsoft.com");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaximumAllowedSkew = TimeSpan.FromSeconds(30);

    public static async Task<bool> IsClockSkewedAsync()
    {
        try
        {
            using HttpClient client = new()
            {
                Timeout = RequestTimeout
            };
            using HttpRequestMessage request = new(HttpMethod.Head, TimeReferenceUri);
            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            DateTimeOffset? serverDate = response.Headers.Date;
            if (serverDate is null)
            {
                return false;
            }

            TimeSpan skew = serverDate.Value.UtcDateTime - DateTime.UtcNow;
            return skew.Duration() > MaximumAllowedSkew;
        }
        catch
        {
            return false;
        }
    }
}
