// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_resolving_dedicated_health_port;

public class and_the_health_port_is_exclusive : Specification
{
    const int MainPort = 35000;
    const int HealthPort = 8080;
    int? _dedicatedPort;
    int? _dedicatedPortWhenEqualToMainPort;

    void Because()
    {
        _dedicatedPort = new ChronicleOptions
        {
            Port = MainPort,
            Health = new Health { Port = HealthPort, Exclusive = true }
        }.DedicatedHealthPort;

        _dedicatedPortWhenEqualToMainPort = new ChronicleOptions
        {
            Port = MainPort,
            Health = new Health { Port = MainPort, Exclusive = true }
        }.DedicatedHealthPort;
    }

    [Fact] void should_resolve_the_configured_health_port() => _dedicatedPort.ShouldEqual(HealthPort);
    [Fact] void should_not_resolve_a_port_equal_to_the_main_port() => _dedicatedPortWhenEqualToMainPort.ShouldBeNull();
}
