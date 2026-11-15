// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLeaseHealthCheck.when_waiting_for_the_lease;

public class and_standby_is_disabled : given.a_lease_health_check
{
    void Establish() => _lease.IsWaiting.Returns(true);

    async Task Because() => _result = await _check.CheckHealthAsync(new HealthCheckContext());

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
}
