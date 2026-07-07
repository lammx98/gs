namespace GS.Core.Auth;

public interface IJwtTokenService
{
    AuthTokenResult CreateToken(JwtTokenRequest request);
}
