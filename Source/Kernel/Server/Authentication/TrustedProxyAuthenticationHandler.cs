// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Authenticates HTTP callers from the user a trusted reverse proxy has already authenticated and forwarded in headers.
/// </summary>
/// <remarks>
/// It refuses gRPC calls outright - clients authenticate with their own credentials - and accepts the headers only
/// when the immediate peer is a trusted proxy and, if one is configured, the proxy presented the shared secret.
/// </remarks>
/// <param name="options">The scheme options.</param>
/// <param name="chronicleOptions">The Chronicle options.</param>
/// <param name="peers">Decides which immediate peers are trusted proxies.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
public class TrustedProxyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptions<ChronicleOptions> chronicleOptions,
    TrustedProxyPeers peers,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// The name of the authentication scheme.
    /// </summary>
    public const string SchemeName = "Chronicle.TrustedProxy";

    /// <summary>
    /// Checks whether a request carries trusted proxy identity headers this scheme should authenticate.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="authentication">The authentication configuration.</param>
    /// <returns>True if trusted proxy authentication is on, the request is not a gRPC call and it carries the user header.</returns>
    public static bool Applies(HttpRequest request, Configuration.Authentication authentication) =>
        authentication.Http.IsTrustedProxy &&
        !IsGrpc(request) &&
        request.Headers.ContainsKey(authentication.Http.TrustedProxy.UserHeader);

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authentication = chronicleOptions.Value.Authentication;
        if (!authentication.Http.IsTrustedProxy || IsGrpc(Request)) return Task.FromResult(AuthenticateResult.NoResult());

        var proxy = authentication.Http.TrustedProxy;
        var user = Single(proxy.UserHeader);
        if (user is null) return Task.FromResult(AuthenticateResult.NoResult());

        if (!peers.IsTrusted(Context.Features.Get<IImmediatePeerFeature>()?.Address))
        {
            return Task.FromResult(AuthenticateResult.Fail("Identity headers were sent by a peer that is not a trusted proxy."));
        }

        if (!string.IsNullOrEmpty(proxy.SharedSecret) && !SecretMatches(Single(proxy.SharedSecretHeader), proxy.SharedSecret))
        {
            return Task.FromResult(AuthenticateResult.Fail("The trusted proxy did not present the shared secret."));
        }

        var principal = new ClaimsPrincipal(CreateIdentity(user, proxy));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    static bool IsGrpc(HttpRequest request) =>
        request.ContentType?.Contains("grpc", StringComparison.OrdinalIgnoreCase) is true;

    static bool SecretMatches(string? presented, string expected) =>
        presented is not null &&
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(presented)), SHA256.HashData(Encoding.UTF8.GetBytes(expected)));

    ClaimsIdentity CreateIdentity(string user, TrustedProxyAuthentication proxy)
    {
        var issuer = proxy.Issuer;
        var name = Optional(proxy.NameHeader) ?? user;
        var claims = new List<Claim>
        {
            new("sub", user, ClaimValueTypes.String, issuer),
            new(ClaimTypes.NameIdentifier, user, ClaimValueTypes.String, issuer),
            new("name", name, ClaimValueTypes.String, issuer),
            new("preferred_username", name, ClaimValueTypes.String, issuer)
        };

        if (Optional(proxy.EmailHeader) is { } email) claims.Add(new("email", email, ClaimValueTypes.String, issuer));

        if (Optional(proxy.GroupsHeader) is { } groups)
        {
            foreach (var group in groups.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal))
            {
                claims.Add(new(TrustedProxyUsers.GroupClaimType, group, ClaimValueTypes.String, issuer));
            }
        }

        return new ClaimsIdentity(claims, TrustedProxyUsers.AuthenticationType, "name", TrustedProxyUsers.GroupClaimType);
    }

    string? Optional(string? header) => string.IsNullOrWhiteSpace(header) ? null : Single(header);

    string? Single(string header)
    {
        // Several values mean the request did not come from the proxy alone; never pick one of them.
        var values = Request.Headers[header];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
