// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ServerEndpointResolver;

public class when_a_health_port_is_configured : Specification
{
    ChronicleOptions _options;
    IReadOnlyList<ServerEndpoint> _result;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        HealthPort = 8081,
        Tls = new Tls { Enabled = true }
    };

    void Because() => _result = ServerEndpointResolver.Resolve(_options);

    [Fact] void should_keep_the_main_tls_port() =>
        _result.Any(_ => _.Port == 35000 && _.UseTls).ShouldBeTrue();
    [Fact] void should_add_a_dedicated_plaintext_health_endpoint() =>
        _result.Any(_ => _.Port == 8081 && _.Protocols == EndpointProtocols.Http1 && !_.UseTls).ShouldBeTrue();
}
