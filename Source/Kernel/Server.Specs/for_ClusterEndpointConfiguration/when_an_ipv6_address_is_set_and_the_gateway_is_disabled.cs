// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterEndpointConfiguration;

public class when_an_ipv6_address_is_set_and_the_gateway_is_disabled : given.a_silo
{
    EndpointOptions _options;
    void Because() => _options = Configure(new() { Type = ClusteringType.Sql, AdvertisedIP = "fd00::1", GatewayEnabled = false, GatewayPort = 31000 });
    [Fact] void should_advertise_the_ipv6_address() => _options.AdvertisedIPAddress.ShouldEqual(IPAddress.Parse("fd00::1"));
    [Fact] void should_bind_the_silo_to_the_ipv6_address() => (_options.SiloListeningEndpoint?.Address ?? _options.AdvertisedIPAddress).ShouldEqual(IPAddress.Parse("fd00::1"));
    [Fact] void should_disable_the_gateway() => _options.GatewayPort.ShouldEqual(0);
    [Fact] void should_not_configure_an_active_gateway_listener() => (_options.GatewayListeningEndpoint?.Port ?? 0).ShouldEqual(0);
}
