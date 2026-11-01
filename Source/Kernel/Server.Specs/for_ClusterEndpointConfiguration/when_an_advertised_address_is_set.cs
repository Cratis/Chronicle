// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterEndpointConfiguration;

public class when_an_advertised_address_is_set : given.a_silo
{
    EndpointOptions _options;
    void Because() => _options = Configure(new() { Type = ClusteringType.MongoDB, AdvertisedIP = "127.0.0.2", SiloPort = 12111, GatewayPort = 31000 });
    [Fact] void should_advertise_the_address() => _options.AdvertisedIPAddress.ShouldEqual(IPAddress.Parse("127.0.0.2"));
    [Fact] void should_bind_the_silo_to_the_advertised_address() => (_options.SiloListeningEndpoint?.Address ?? _options.AdvertisedIPAddress).ShouldEqual(IPAddress.Parse("127.0.0.2"));
    [Fact] void should_bind_the_gateway_to_the_advertised_address() => (_options.GatewayListeningEndpoint?.Address ?? _options.AdvertisedIPAddress).ShouldEqual(IPAddress.Parse("127.0.0.2"));
    [Fact] void should_keep_the_silo_port() => _options.SiloPort.ShouldEqual(12111);
    [Fact] void should_keep_the_gateway_port() => _options.GatewayPort.ShouldEqual(31000);
}
