// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Validates the trusted proxy settings, so a misconfigured proxy fails at startup rather than on the first request.
/// </summary>
public class TrustedProxyAuthenticationValidation : IValidateOptions<ChronicleOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ChronicleOptions options)
    {
        var authentication = options.Authentication;
        if (!authentication.Enabled || !authentication.Http.IsTrustedProxy) return ValidateOptionsResult.Success;

        var proxy = authentication.Http.TrustedProxy;
        var failures = new List<string>();
        if (!IsHeaderName(proxy.UserHeader)) failures.Add("Authentication:Http:TrustedProxy:UserHeader must be a header name.");
        if (!IsOptionalHeaderName(proxy.EmailHeader)) failures.Add("Authentication:Http:TrustedProxy:EmailHeader must be a header name when set.");
        if (!IsOptionalHeaderName(proxy.NameHeader)) failures.Add("Authentication:Http:TrustedProxy:NameHeader must be a header name when set.");
        if (!IsOptionalHeaderName(proxy.GroupsHeader)) failures.Add("Authentication:Http:TrustedProxy:GroupsHeader must be a header name when set.");
        if (!proxy.TrustLoopback && !proxy.KnownProxies.Any() && !proxy.KnownNetworks.Any())
        {
            failures.Add("Authentication:Http:TrustedProxy needs the proxy it trusts: KnownProxies, KnownNetworks or TrustLoopback.");
        }

        failures.AddRange(proxy.KnownProxies
            .Where(proxyAddress => !IPAddress.TryParse(proxyAddress, out _))
            .Select(proxyAddress => $"Authentication:Http:TrustedProxy:KnownProxies must contain IP addresses. Invalid value: '{proxyAddress}'."));
        failures.AddRange(proxy.KnownNetworks
            .Where(network => !network.Contains('/', StringComparison.Ordinal) || !IPNetwork.TryParse(network, out _))
            .Select(network => $"Authentication:Http:TrustedProxy:KnownNetworks must contain CIDR ranges. Invalid value: '{network}'."));

        if (!IsExact(proxy.Issuer) || IsReservedIssuer(proxy.Issuer, authentication))
        {
            failures.Add($"Authentication:Http:TrustedProxy:Issuer must be a non-empty exact value, distinct from '{PrincipalKey.LocalIssuer}' and the configured authority.");
        }

        if (proxy.AdministratorGroup is not null && !IsExact(proxy.AdministratorGroup))
        {
            failures.Add("Authentication:Http:TrustedProxy:AdministratorGroup must be a non-empty exact value when set.");
        }

        if (!string.IsNullOrEmpty(proxy.SharedSecret) && !IsHeaderName(proxy.SharedSecretHeader))
        {
            failures.Add("Authentication:Http:TrustedProxy:SharedSecretHeader must be a header name when a shared secret is set.");
        }

        if (!string.IsNullOrEmpty(proxy.SignOutUrl) && !IsSignOutUrl(proxy.SignOutUrl))
        {
            failures.Add("Authentication:Http:TrustedProxy:SignOutUrl must be an absolute HTTP(S) URL or a path starting with '/'.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    static bool IsReservedIssuer(string issuer, Authentication authentication) =>
        string.Equals(issuer, PrincipalKey.LocalIssuer, StringComparison.Ordinal) ||
        string.Equals(issuer, ClaimsIdentity.DefaultIssuer, StringComparison.Ordinal) ||
        (!string.IsNullOrEmpty(authentication.Authority) && string.Equals(issuer.TrimEnd('/'), authentication.Authority.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

    static bool IsExact(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length == value.Length;

    static bool IsOptionalHeaderName(string? value) => value is null || IsHeaderName(value);

    static bool IsHeaderName(string? value) =>
        IsExact(value) && value!.All(_ => _ > ' ' && _ < 127 && _ != ':');

    static bool IsSignOutUrl(string value) =>
        (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) && !value.StartsWith("/\\", StringComparison.Ordinal)) ||
        (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}
