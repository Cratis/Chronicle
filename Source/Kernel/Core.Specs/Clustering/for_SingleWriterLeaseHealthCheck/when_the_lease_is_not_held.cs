// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLeaseHealthCheck;

public class when_the_lease_is_not_held : Specification
{
    SingleWriterLeaseHealthCheck _check;
    HealthCheckResult _result;

    void Establish()
    {
        var lease = Substitute.For<ISingleWriterLease>();
        lease.IsHeld.Returns(false);
        _check = new(lease);
    }

    async Task Because() => _result = await _check.CheckHealthAsync(new HealthCheckContext());

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
}
