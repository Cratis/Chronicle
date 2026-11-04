// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Setup.for_KernelProjectionsRegistrationHealthCheck.when_checking;

public class and_nothing_is_pending : Specification
{
    KernelProjectionsRegistrationHealthCheck _check;
    HealthCheckResult _result;

    void Establish()
    {
        var retries = Substitute.For<IKernelProjectionRegistrationRetries>();
        retries.Pending.Returns([]);
        _check = new(retries);
    }

    async Task Because() => _result = await _check.CheckHealthAsync(new());

    [Fact] void should_be_healthy() => _result.Status.ShouldEqual(HealthStatus.Healthy);
}
