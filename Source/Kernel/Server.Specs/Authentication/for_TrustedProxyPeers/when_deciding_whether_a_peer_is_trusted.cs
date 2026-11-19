// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyPeers;

public class when_deciding_whether_a_peer_is_trusted : Specification
{
    [Theory]
    [InlineData("192.0.2.10", true)]
    [InlineData("::ffff:192.0.2.10", true)]
    [InlineData("10.0.0.42", true)]
    [InlineData("10.0.1.42", false)]
    [InlineData("203.0.113.9", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("::1", false)]
    void should_trust_only_the_configured_proxies(string address, bool expected) =>
        Peers(new() { KnownProxies = ["192.0.2.10"], KnownNetworks = ["10.0.0.0/24"] }).IsTrusted(IPAddress.Parse(address)).ShouldEqual(expected);

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    void should_trust_loopback_only_when_asked_to(string address) =>
        Peers(new() { TrustLoopback = true }).IsTrusted(IPAddress.Parse(address)).ShouldBeTrue();

    [Fact]
    void should_not_inherit_the_proxies_trusted_for_forwarded_headers() =>
        new TrustedProxyPeers(new ChronicleOptions
        {
            ForwardedHeaders = new() { KnownProxies = ["192.0.2.10"], KnownNetworks = ["10.0.0.0/24"] },
            Authentication = new() { Http = new() { Mode = HttpAuthenticationMode.TrustedProxy, TrustedProxy = new() { TrustLoopback = true } } }
        }).IsTrusted(IPAddress.Parse("192.0.2.10")).ShouldBeFalse();

    [Fact] void should_not_trust_an_unknown_peer() => Peers(new() { TrustLoopback = true }).IsTrusted(null).ShouldBeFalse();

    static TrustedProxyPeers Peers(TrustedProxyAuthentication proxy) => new(new ChronicleOptions
    {
        Authentication = new() { Http = new() { Mode = HttpAuthenticationMode.TrustedProxy, TrustedProxy = proxy } }
    });
}
