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
/// unspecified, private (RFC 1918), benchmarking, documentation or otherwise internal-only address, including such an IPv4 address
/// embedded in an IPv6 one (IPv4-mapped, IPv4-compatible, NAT64 and 6to4). The address that was checked is the address that is connected to,
/// so the name cannot resolve to something else between the check and the connection.
/// <para>
/// A peer on a private network is possible with AllowRestrictedAddresses, which lifts the address check for every host. The real
/// control is AllowedHosts: once it lists the hosts an enrollment may name, nothing else is connected to, whatever it resolves to.
/// </para>
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

        if (address.AddressFamily == AddressFamily.InterNetworkV6 && TryGetEmbeddedIPv4(address, out var embedded))
        {
            return IsRestricted(embedded);
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
                || (bytes[0] & 0xFE) == 0xFC
                || IsIPv6Special(bytes);
        }

        return bytes[0] == 0
            || bytes[0] >= 224
            || bytes[0] == 10
            || (bytes[0] == 172 && (bytes[1] & 0xF0) == 16)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 169 && bytes[1] == 254)
            || (bytes[0] == 100 && (bytes[1] & 0xC0) == 64)
            || (bytes[0] == 198 && (bytes[1] & 0xFE) == 18)
            || (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0)
            || (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2)
            || (bytes[0] == 192 && bytes[1] == 88 && bytes[2] == 99)
            || (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100)
            || (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
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

    static bool TryGetEmbeddedIPv4(IPAddress address, out IPAddress embedded)
    {
        var bytes = address.GetAddressBytes();

        // 64:ff9b::/96 NAT64, 2002::/16 6to4, ::a.b.c.d IPv4-compatible and ::ffff:0:a.b.c.d IPv4-translated.
        if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xFF && bytes[3] == 0x9B && bytes.AsSpan(4, 8).IndexOfAnyExcept((byte)0) < 0)
        {
            embedded = new IPAddress(bytes.AsSpan(12, 4));
            return true;
        }

        if (bytes[0] == 0x20 && bytes[1] == 0x02)
        {
            embedded = new IPAddress(bytes.AsSpan(2, 4));
            return true;
        }

        // :: and ::1 are the unspecified and loopback addresses and are decided as such, not as an embedded address.
        var compatible = bytes.AsSpan(0, 12).IndexOfAnyExcept((byte)0) < 0 && bytes.AsSpan(12, 3).IndexOfAnyExcept((byte)0) >= 0;
        var translated = bytes.AsSpan(0, 8).IndexOfAnyExcept((byte)0) < 0 && bytes[8] == 0xFF && bytes[9] == 0xFF && bytes[10] == 0 && bytes[11] == 0;
        if (compatible || translated)
        {
            embedded = new IPAddress(bytes.AsSpan(12, 4));
            return true;
        }

        embedded = IPAddress.None;
        return false;
    }

    static bool IsIPv6Special(byte[] bytes) =>

        // The reserved ::/8 block, which holds what embeds an address that was not unwrapped, such as the local-use NAT64 prefix 64:ff9b:1::/48,
        // Teredo 2001::/32 and documentation 2001:db8::/32 never name a public peer.
        bytes[0] == 0x00
        || (bytes[0] == 0x20 && bytes[1] == 0x01 && ((bytes[2] == 0 && bytes[3] == 0) || (bytes[2] == 0x0D && bytes[3] == 0xB8)));
}
