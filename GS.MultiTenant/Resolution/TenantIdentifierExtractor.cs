using GS.MultiTenant.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GS.MultiTenant.Resolution;

internal static class TenantIdentifierExtractor
{
    public static string? FromHeader(HttpContext httpContext, MultiTenantOptions options)
    {
        if (!httpContext.Request.Headers.TryGetValue(options.TenantHeaderName, out var values))
        {
            return null;
        }

        return values.FirstOrDefault();
    }

    public static string? FromHost(HttpContext httpContext, MultiTenantOptions options)
    {
        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var template = options.HostTemplate;
        if (template.StartsWith("__tenant__.", StringComparison.Ordinal))
        {
            var suffix = template["__tenant__.".Length..];
            if (suffix == "*" && host.Contains('.'))
            {
                return host.Split('.')[0];
            }
        }

        if (template.Contains("__tenant__", StringComparison.Ordinal))
        {
            var pattern = template.Replace("__tenant__", "(?<tenant>[^.]+)", StringComparison.Ordinal)
                .Replace(".", "\\.", StringComparison.Ordinal)
                .Replace("*", ".*", StringComparison.Ordinal);
            var match = System.Text.RegularExpressions.Regex.Match(host, $"^{pattern}$");
            if (match.Success && match.Groups["tenant"].Success)
            {
                return match.Groups["tenant"].Value;
            }
        }

        return null;
    }

    public static string? FromJwt(HttpContext httpContext, MultiTenantOptions options)
    {
        var principal = httpContext.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return principal.FindFirstValue(options.JwtTenantClaimType)
            ?? principal.FindFirstValue("tid")
            ?? principal.FindFirstValue("__tenant__");
    }

    public static void ValidateConsistency(IReadOnlyCollection<string> identifiers)
    {
        var distinct = identifiers
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinct.Count > 1)
        {
            throw new Exceptions.TenantMismatchException(
                $"Conflicting tenant identifiers detected: {string.Join(", ", distinct)}.");
        }
    }
}
