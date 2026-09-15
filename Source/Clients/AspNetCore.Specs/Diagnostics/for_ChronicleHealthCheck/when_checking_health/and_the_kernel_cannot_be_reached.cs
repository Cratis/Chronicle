// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

/// <summary>
/// Getting at the event store is itself a call that fails while the kernel is unreachable (#3948). A health check
/// that let that through would report nothing at all - and an unreachable kernel is exactly what it exists to say.
/// </summary>
public class and_the_kernel_cannot_be_reached : given.a_health_check
{
    HealthCheckResult _result;

    void Establish() =>
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns<IEventStore>(_ => throw new ConnectionUnavailable("chronicle://the-kernel"));

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_carry_the_failure() => _result.Exception.ShouldBeOfExactType<ConnectionUnavailable>();
}
