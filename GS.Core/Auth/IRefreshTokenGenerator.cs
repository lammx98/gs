namespace GS.Core.Auth;

public interface IRefreshTokenGenerator
{
    RefreshTokenValue Generate();

    string Hash(string token);
}

public sealed record RefreshTokenValue(string Token, string Hash);
