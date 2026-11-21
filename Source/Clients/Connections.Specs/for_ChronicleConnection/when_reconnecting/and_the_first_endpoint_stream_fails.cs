// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class and_the_first_endpoint_stream_fails : given.a_connection_with_several_endpoints
{
    async Task Because()
    {
        _streams[_a] = () => Observable.Throw<ConnectionKeepAlive>(Unavailable());
        _streams[_b] = Healthy;
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_be_connected() => _isConnected.ShouldBeTrue();
    [Fact] void should_try_the_first_endpoint_then_the_second() => TriedInOrder.ShouldEqual("node-a,node-b");
    [Fact] void should_warn_about_the_failed_candidate() => _logger.AtLevel(LogLevel.Warning).ShouldContain(entry => entry.Message.Contains("node-a:35000", StringComparison.Ordinal));
}
