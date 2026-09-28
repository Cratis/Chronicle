// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_tls_is_enabled_with_a_certificate : given.listeners
{
    void Because() => Configure(new() { Authentication = new() { Enabled = false } }, _certificate);

    [Fact] void should_serve_both_http_protocols() => _listeners.Single().Protocols.ShouldEqual(HttpProtocols.Http1AndHttp2);
    [Fact] void should_use_the_tls_certificate() => _listeners.Single().Certificate.ShouldEqual(_certificate);
}
