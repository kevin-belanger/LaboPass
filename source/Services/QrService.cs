using QRCoder;
using ZXing;
using ZXing.Windows.Compatibility;

namespace LaboPass.Services;

public sealed class QrService
{
    public string? ReadOtpAuthUriFromClipboard()
    {
        if (!Clipboard.ContainsImage())
        {
            return null;
        }

        using Image? image = Clipboard.GetImage();
        if (image is null)
        {
            return null;
        }

        using Bitmap bitmap = new(image);
        BarcodeReader reader = new()
        {
            AutoRotate = true,
            Options = { TryHarder = true, TryInverted = true, PossibleFormats = [BarcodeFormat.QR_CODE] }
        };

        Result? result = reader.Decode(bitmap);
        string? text = result?.Text?.Trim();
        return text is not null && text.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase)
            ? text
            : null;
    }

    public Image CreateQrImage(string totpUri, int pixelsPerModule = 8)
    {
        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q);
        PngByteQRCode qrCode = new(data);
        byte[] png = qrCode.GetGraphic(pixelsPerModule);
        using MemoryStream stream = new(png);
        return Image.FromStream(stream);
    }
}
