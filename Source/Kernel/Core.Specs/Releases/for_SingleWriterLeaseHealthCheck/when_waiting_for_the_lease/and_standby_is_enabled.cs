// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLeaseHealthCheck.when_waiting_for_the_lease;

public class and_standby_is_enabled : given.a_lease_health_check
{
    void Establish()
    {
        _options.Clustering.SingleWriterLease.Standby = true;
        _lease.IsWaiting.Returns(true);
    }

    async Task Because() => _result = await _check.CheckHealthAsync(new HealthCheckContext());

    [Fact] void should_report_degraded_so_readiness_answers_ok() => _result.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_mark_standby() => _result.Data.ContainsKey(SingleWriterLeaseStandby.HealthDataKey).ShouldBeTrue();
}
