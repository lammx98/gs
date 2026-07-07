using Microsoft.AspNetCore.Identity;

namespace GS.Core.Security;

public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password)
            is not PasswordVerificationResult.Failed;
}
