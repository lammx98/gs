using System.Security.Cryptography;
using System.Text;

namespace GS.Core.Auth;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenByteLength = 32;

    public RefreshTokenValue Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var token = Base64UrlEncode(bytes);
        return new RefreshTokenValue(token, Hash(token));
    }

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Base64UrlEncode(hashBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
