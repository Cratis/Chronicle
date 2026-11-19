// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Decides whether an immediate peer is a reverse proxy whose identity headers may be trusted.
/// </summary>
/// <remarks>
/// It trusts exactly <see cref="TrustedProxyAuthentication.KnownProxies"/>, <see cref="TrustedProxyAuthentication.KnownNetworks"/>
/// and, when <see cref="TrustedProxyAuthentication.TrustLoopback"/> is on, loopback. It deliberately does not inherit the peers
/// trusted for forwarded headers. Values that do not parse are ignored here; configuration validation refuses them at startup.
/// </remarks>
public class TrustedProxyPeers
{
    readonly IPAddress[] _proxies;
    readonly System.Net.IPNetwork[] _networks;
    readonly bool _trustLoopback;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrustedProxyPeers"/> class.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    public TrustedProxyPeers(ChronicleOptions options)
    {
        var proxy = options.Authentication.Http.TrustedProxy;
        _proxies = [.. proxy.KnownProxies.Select(_ => IPAddress.TryParse(_, out var address) ? Normalize(address) : null).OfType<IPAddress>()];
        _networks = [.. proxy.KnownNetworks.Where(network => network.Contains('/', StringComparison.Ordinal) && System.Net.IPNetwork.TryParse(network, out _)).Select(System.Net.IPNetwork.Parse)];
        _trustLoopback = proxy.TrustLoopback;
    }

    /// <summary>
    /// Checks whether the peer is trusted.
    /// </summary>
    /// <param name="address">The address of the immediate peer.</param>
    /// <returns>True if the peer is a configured proxy, or loopback when that is trusted.</returns>
    public bool IsTrusted(IPAddress? address)
    {
        if (address is null) return false;
        address = Normalize(address);
        if (_proxies.Contains(address) || _networks.Any(_ => _.Contains(address))) return true;
        return _trustLoopback && IPAddress.IsLoopback(address);
    }

    static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
