// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_the_keep_alive_reports_the_kernel_status;

public class and_the_kernel_is_busy_and_recovers : given.a_connection_with_several_endpoints
{
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    async Task Because()
    {
        _streams[_a] = () => Observable.Return(new ConnectionKeepAlive { Status = KernelStatus.Accepting })
            .Concat(Observable.Return(new ConnectionKeepAlive { Status = KernelStatus.Degraded }))
            .Concat(Observable.Return(new ConnectionKeepAlive { Status = KernelStatus.Degraded }))
            .Concat(Observable.Return(new ConnectionKeepAlive { Status = KernelStatus.Accepting }))
            .Concat(Observable.Never<ConnectionKeepAlive>());
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await WaitUntil(() => _logger.Messages.Any(_ => _.Contains("accepting calls normally again")));
    }

    [Fact] void should_warn_once_that_the_kernel_is_busy() => _logger.Entries.Count(_ => _.Level == LogLevel.Warning && _.Message.Contains("Chronicle reports that it is Degraded")).ShouldEqual(1);
    [Fact] void should_say_when_the_kernel_is_accepting_again() => _logger.Messages.Count(_ => _.Contains("accepting calls normally again")).ShouldEqual(1);
    [Fact] void should_not_say_anything_while_the_kernel_is_accepting() => _logger.Messages.Count(_ => _.Contains("Chronicle reports")).ShouldEqual(2);
}
