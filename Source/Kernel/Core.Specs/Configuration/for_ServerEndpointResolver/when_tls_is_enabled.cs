// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ServerEndpointResolver;

public class when_tls_is_enabled : Specification
{
    ChronicleOptions _options;
    IReadOnlyList<ServerEndpoint> _result;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        Tls = new Tls { Enabled = true }
    };

    void Because() => _result = ServerEndpointResolver.Resolve(_options);

    [Fact] void should_bind_a_single_endpoint() => _result.Count.ShouldEqual(1);
    [Fact] void should_bind_the_main_port() => _result[0].Port.ShouldEqual(35000);
    [Fact] void should_multiplex_http1_and_http2() => _result[0].Protocols.ShouldEqual(EndpointProtocols.Http1AndHttp2);
    [Fact] void should_use_tls() => _result[0].UseTls.ShouldBeTrue();
}
