// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_the_dedicated_health_port_uses_tls_while_the_main_port_does_not : given.listeners
{
    void Because() => Configure(
        new()
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Enabled = false },
            Health = new() { Port = 8080 }
        },
        _certificate);

    [Fact] void should_serve_the_main_port_over_cleartext_http2() => _listeners[0].Protocols.ShouldEqual(HttpProtocols.Http2);
    [Fact] void should_not_use_the_health_certificate_on_the_main_port() => _listeners[0].Certificate.ShouldBeNull();
    [Fact] void should_serve_the_health_port_over_https() => _listeners[1].Certificate.ShouldEqual(_certificate);
    [Fact] void should_serve_the_health_port_over_http1() => _listeners[1].Protocols.ShouldEqual(HttpProtocols.Http1);
}
