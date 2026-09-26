// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class with_degraded_failure_status_and_disconnected_client : given.a_health_check
{
    HealthCheckResult _result;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(false);
        _context.Registration = new HealthCheckRegistration(ChronicleHealthCheck.Name, Substitute.For<IHealthCheck>(), HealthStatus.Degraded, null);
    }

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_degraded() => _result.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_name_the_event_store() => _result.Description.ShouldContain("the-store");
}
