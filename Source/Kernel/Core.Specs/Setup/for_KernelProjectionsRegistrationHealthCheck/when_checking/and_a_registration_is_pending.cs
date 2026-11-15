// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Setup.for_KernelProjectionsRegistrationHealthCheck.when_checking;

public class and_a_registration_is_pending : Specification
{
    KernelProjectionsRegistrationHealthCheck _check;
    HealthCheckResult _result;

    void Establish()
    {
        var retries = Substitute.For<IKernelProjectionRegistrationRetries>();
        retries.Pending.Returns(["first-store", "second-store"]);
        retries.GetAttemptCount("first-store").Returns(2);
        retries.GetAttemptCount("second-store").Returns(4);
        _check = new(retries);
    }

    async Task Because() => _result = await _check.CheckHealthAsync(new());

    [Fact] void should_report_degraded() => _result.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_name_every_pending_store_and_attempt_count() => _result.Description.ShouldEqual("Kernel projections are not registered yet for event stores: first-store (background attempts: 2), second-store (background attempts: 4). Retrying in the background.");
}
