// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Federation.for_FederationEndpointPolicy;

/// <summary>
/// Addresses that embed an IPv4 address (NAT64, 6to4, IPv4-compatible, IPv4-translated and IPv4-mapped) are decided by the address they embed.
/// </summary>
public class when_checking_an_address
{
    [Theory]
    [InlineData("64:ff9b::a9fe:a9fe")]
    [InlineData("64:ff9b::7f00:1")]
    [InlineData("64:ff9b::a00:1")]
    [InlineData("64:ff9b::c0a8:101")]
    [InlineData("64:ff9b:1::1")]
    [InlineData("2002:a9fe:a9fe::1")]
    [InlineData("2002:7f00:1::1")]
    [InlineData("::a9fe:a9fe")]
    [InlineData("::7f00:1")]
    [InlineData("::ffff:0:a9fe:a9fe")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:10.1.2.3")]
    [InlineData("2001:0:4136:e378:8000:63bf:3fff:fdd2")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.255")]
    [InlineData("192.0.2.1")]
    [InlineData("192.88.99.1")]
    public void should_be_restricted(string address) => FederationEndpointPolicy.IsRestricted(IPAddress.Parse(address)).ShouldBeTrue();

    [Theory]
    [InlineData("93.184.216.34")]
    [InlineData("172.15.0.1")]
    [InlineData("172.32.0.1")]
    [InlineData("198.17.0.1")]
    [InlineData("198.20.0.1")]
    [InlineData("64:ff9b::5db8:d822")]
    [InlineData("2002:5db8:d822::1")]
    [InlineData("::ffff:93.184.216.34")]
    [InlineData("2606:2800:220:1:248:1893:25c8:1946")]
    public void should_not_be_restricted(string address) => FederationEndpointPolicy.IsRestricted(IPAddress.Parse(address)).ShouldBeFalse();
}
