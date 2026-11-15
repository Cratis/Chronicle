// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.when_deferring;

public class and_jitter_is_at_its_lower_bound : given.a_retry_loop
{
    void Establish() => _jitter = 0;

    async Task Because()
    {
        _retries.Defer(_eventStore);
        await _timeProvider.Next();
    }

    [Fact] void should_wait_half_the_initial_backoff() => _timeProvider.Delays.Single().ShouldEqual(TimeSpan.FromSeconds(2.5));
    [Fact] void should_start_with_zero_background_attempts() => _retries.GetAttemptCount(_eventStore).ShouldEqual(0);
}
