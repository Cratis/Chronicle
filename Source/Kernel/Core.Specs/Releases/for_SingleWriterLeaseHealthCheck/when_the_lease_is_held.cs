// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLeaseHealthCheck;

public class when_the_lease_is_held : given.a_lease_health_check
{
    void Establish()
    {
        _options.Clustering.SingleWriterLease.Standby = true;
        _lease.IsHeld.Returns(true);
    }

    async Task Because() => _result = await _check.CheckHealthAsync(new HealthCheckContext());

    [Fact] void should_report_healthy() => _result.Status.ShouldEqual(HealthStatus.Healthy);
    [Fact] void should_not_mark_standby() => _result.Data.ContainsKey(SingleWriterLeaseStandby.HealthDataKey).ShouldBeFalse();
}
