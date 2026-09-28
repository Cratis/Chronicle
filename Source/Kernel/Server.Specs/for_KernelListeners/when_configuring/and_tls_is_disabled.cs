// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelListeners.when_configuring;

public class and_tls_is_disabled : given.listeners
{
    void Because() => Configure(
        new()
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Enabled = false }
        },
        null);

    [Fact] void should_serve_http2_only() => _listeners.Single().Protocols.ShouldEqual(HttpProtocols.Http2);
    [Fact] void should_not_use_https() => _listeners.Single().Certificate.ShouldBeNull();
    [Fact] void should_warn_about_cleartext_and_the_proxy() => _logger.ReceivedCalls()
        .Any(_ => _.GetMethodInfo().Name == "Log" && _.GetArguments()[0] is LogLevel.Warning &&
                  _.GetArguments()[1] is EventId eventId && eventId.Name == "TlsDisabledBehindProxy")
        .ShouldBeTrue();
}
