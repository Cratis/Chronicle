// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Decides whether this kernel may connect to the authority endpoint an enrollment code names, and to which address.
/// </summary>
/// <remarks>
/// Redeeming a code makes the kernel connect to an endpoint chosen by whoever issued the code. The endpoint must be https and,
/// unless the configuration or a Development environment allows it, must not resolve to a loopback, link-local, cloud metadata,
/// unspecified or otherwise internal-only address. The address that was checked is the address that is connected to, so the name
/// cannot resolve to something else between the check and the connection.
/// </remarks>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the enrollment configuration.</param>
/// <param name="environment">The <see cref="IHostEnvironment"/>.</param>
[Singleton]
public class FederationEndpointPolicy(IOptions<ChronicleOptions> options, IHostEnvironment environment)
{
    /// <summary>
    /// Gets whether an address is one a kernel must not be pointed at by an enrollment code.
    /// </summary>
    /// <param name="address">The <see cref="IPAddress"/> to check.</param>
    /// <returns>True when the address is loopback, link-local, unspecified, multicast, a cloud metadata address or otherwise internal-only.</returns>
    public static bool IsRestricted(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast
                || (bytes[0] & 0xFE) == 0xFC;
        }

        return bytes[0] == 0
            || bytes[0] >= 224
            || (bytes[0] == 169 && bytes[1] == 254)
            || (bytes[0] == 100 && (bytes[1] & 0xC0) == 64)
            || (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0);
    }

    /// <summary>
    /// Resolve the endpoint to the addresses this kernel may connect to.
    /// </summary>
    /// <param name="endpoint">The endpoint an enrollment code or an authority names.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The addresses to connect to, in order, or null when the endpoint is not permitted. A refusal never says why.</returns>
    public async Task<IPAddress[]?> Resolve(Uri endpoint, CancellationToken cancellationToken)
    {
        var enrollment = options.Value.Federation.Enrollment;
        if (endpoint.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(endpoint.Host) || !string.IsNullOrEmpty(endpoint.UserInfo))
        {
            return null;
        }

        var allowedHosts = enrollment.AllowedHosts.ToArray();
        if (allowedHosts.Length > 0 && !allowedHosts.Contains(endpoint.DnsSafeHost, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        IPAddress[] addresses;
        try
        {
            addresses = IPAddress.TryParse(endpoint.DnsSafeHost, out var literal)
                ? [literal]
                : await ResolveHost(endpoint.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            return null;
        }

        if (addresses.Length == 0)
        {
            return null;
        }

        var allowRestricted = enrollment.AllowRestrictedAddresses || environment.IsDevelopment();
        return allowRestricted || addresses.All(_ => !IsRestricted(_)) ? addresses : null;
    }

    /// <summary>
    /// Resolve a host name to its addresses.
    /// </summary>
    /// <param name="host">The host name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The addresses of the host.</returns>
    protected virtual Task<IPAddress[]> ResolveHost(string host, CancellationToken cancellationToken) =>
        Dns.GetHostAddressesAsync(host, cancellationToken);
}
