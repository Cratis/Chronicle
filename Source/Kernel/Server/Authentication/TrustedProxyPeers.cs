// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Decides whether an immediate peer is a reverse proxy whose identity headers may be trusted.
/// </summary>
/// <remarks>
/// It trusts exactly the peers forwarded headers are trusted from: loopback, plus the configured
/// <see cref="ForwardedHeaders.KnownProxies"/> and <see cref="ForwardedHeaders.KnownNetworks"/>.
/// </remarks>
/// <param name="options">The Chronicle options.</param>
public class TrustedProxyPeers(ChronicleOptions options)
{
    readonly ForwardedHeadersOptions _trust = ForwardedHeadersTrust.Create(options);

    /// <summary>
    /// Checks whether the peer is trusted.
    /// </summary>
    /// <param name="address">The address of the immediate peer.</param>
    /// <returns>True if the peer is loopback or a configured proxy.</returns>
    public bool IsTrusted(IPAddress? address)
    {
        if (address is null) return false;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return true;
        if (_trust.KnownProxies.Any(_ => (_.IsIPv4MappedToIPv6 ? _.MapToIPv4() : _).Equals(address))) return true;
        return _trust.KnownIPNetworks.Any(_ => _.Contains(address));
    }
}
