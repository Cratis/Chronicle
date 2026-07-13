// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ServerEndpointResolver;

public class when_the_health_port_matches_the_main_port : Specification
{
    ChronicleOptions _options;
    IReadOnlyList<ServerEndpoint> _result;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        HealthPort = 35000,
        Tls = new Tls { Enabled = true }
    };

    void Because() => _result = ServerEndpointResolver.Resolve(_options);

    [Fact] void should_not_bind_a_duplicate_endpoint() => _result.Count.ShouldEqual(1);
}
