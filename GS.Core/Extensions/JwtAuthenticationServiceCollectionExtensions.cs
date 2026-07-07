using System.Text;
using GS.Core.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using GS.Core.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GS.Core.Extensions;

public static class JwtAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication for validating tokens issued by the platform.
    /// </summary>
    public static IServiceCollection AddGsJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool issueTokens = false)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
        {
            throw new InvalidOperationException($"Jwt:{nameof(JwtOptions.SigningKey)} must be configured.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        if (issueTokens)
        {
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
        }

        return services;
    }
}
