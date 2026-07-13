// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ServerEndpointResolver;

public class when_tls_is_disabled : Specification
{
    ChronicleOptions _options;
    IReadOnlyList<ServerEndpoint> _result;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        ManagementPort = 8080,
        Tls = new Tls { Enabled = false }
    };

    void Because() => _result = ServerEndpointResolver.Resolve(_options);

    [Fact] void should_bind_two_endpoints() => _result.Count.ShouldEqual(2);
    [Fact] void should_serve_cleartext_grpc_on_the_main_port() =>
        _result.Any(_ => _.Port == 35000 && _.Protocols == EndpointProtocols.Http2 && !_.UseTls).ShouldBeTrue();
    [Fact] void should_serve_cleartext_http1_on_the_management_port() =>
        _result.Any(_ => _.Port == 8080 && _.Protocols == EndpointProtocols.Http1 && !_.UseTls).ShouldBeTrue();
    [Fact] void should_not_use_tls_on_any_endpoint() => _result.Any(_ => _.UseTls).ShouldBeFalse();
}
