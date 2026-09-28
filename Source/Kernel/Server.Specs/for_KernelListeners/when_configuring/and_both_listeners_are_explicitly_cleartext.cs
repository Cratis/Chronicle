// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_both_listeners_are_explicitly_cleartext : given.listeners
{
    void Because() => Configure(
        new()
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Enabled = false },
            Health = new() { Port = 8080, Tls = false }
        },
        null);

    [Fact] void should_serve_the_main_port_over_http2() => _listeners[0].Protocols.ShouldEqual(HttpProtocols.Http2);
    [Fact] void should_serve_the_health_port_over_http1() => _listeners[1].Protocols.ShouldEqual(HttpProtocols.Http1);
    [Fact] void should_not_require_a_certificate_for_either_listener() => _listeners.TrueForAll(_ => _.Certificate is null).ShouldBeTrue();
}
