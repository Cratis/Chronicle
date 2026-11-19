// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyPeers;

public class when_deciding_whether_a_peer_is_trusted : Specification
{
    static readonly TrustedProxyPeers _peers = new(new Configuration.ChronicleOptions
    {
        ForwardedHeaders = new() { KnownProxies = ["192.0.2.10"], KnownNetworks = ["10.0.0.0/24"] }
    });

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("192.0.2.10", true)]
    [InlineData("::ffff:192.0.2.10", true)]
    [InlineData("10.0.0.42", true)]
    [InlineData("10.0.1.42", false)]
    [InlineData("203.0.113.9", false)]
    void should_trust_only_loopback_and_configured_proxies(string address, bool expected) =>
        _peers.IsTrusted(IPAddress.Parse(address)).ShouldEqual(expected);

    [Fact] void should_not_trust_an_unknown_peer() => _peers.IsTrusted(null).ShouldBeFalse();
}
