// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.when_deferring;

public class and_the_registration_succeeds_on_a_later_attempt : given.a_retry_loop
{
    void Establish()
    {
        _jitter = 0.5;
        _kernelProjections.DiscoverAndRegister(_eventStore).Returns(
            _ => Task.FromException(new TimeoutException()),
            _ => Task.FromException(new TimeoutException()),
            _ => Task.CompletedTask);
    }

    async Task Because()
    {
        _retries.Defer(_eventStore);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            (await _timeProvider.Next()).Complete();
            await _logger.Next();
        }
    }

    [Fact] async Task should_retry_until_successful() => await _kernelProjections.Received(3).DiscoverAndRegister(_eventStore);
    [Fact] void should_apply_growing_jittered_delays() => _timeProvider.Delays.ShouldEqual([TimeSpan.FromSeconds(3.75), TimeSpan.FromSeconds(7.5), TimeSpan.FromSeconds(15)]);
    [Fact] void should_remove_the_pending_store() => _retries.Pending.ShouldBeEmpty();
    [Fact] async Task should_be_healthy_after_success() => (await new KernelProjectionsRegistrationHealthCheck(_retries).CheckHealthAsync(new())).Status.ShouldEqual(HealthStatus.Healthy);
}
