// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_an_external_authority_is_configured_on_cleartext : given.listeners
{
    void Because() => Configure(
        new()
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Authority = "https://login.example.com" }
        },
        null);

    [Fact] void should_serve_cleartext_http2_with_explicit_https_authority() => _listeners.Single().Protocols.ShouldEqual(HttpProtocols.Http2);
}
