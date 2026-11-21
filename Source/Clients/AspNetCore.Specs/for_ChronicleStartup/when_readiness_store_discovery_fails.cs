// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup;

public class when_readiness_store_discovery_fails : Diagnostics.for_ChronicleHealthCheck.given.a_health_check
{
    readonly Exception _failure = new InvalidOperationException("Store discovery failed");
    HealthCheckResult _result;

    void Establish() => _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromException<IEventStore>(_failure));

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_carry_the_original_failure() => _result.Exception.ShouldEqual(_failure);
}
