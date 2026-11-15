// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.when_deferring;

public class and_it_keeps_failing : given.a_retry_loop
{
    void Establish() => _kernelProjections.DiscoverAndRegister(_eventStore).Returns(_ => Task.FromException(new TimeoutException()));

    async Task Because()
    {
        _retries.Defer(_eventStore);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            (await _timeProvider.Next()).Complete();
            await _logger.Next();
        }
        await _timeProvider.Next();
    }

    [Fact] async Task should_keep_retrying() => await _kernelProjections.Received(10).DiscoverAndRegister(_eventStore);
    [Fact] void should_count_background_attempts() => _retries.GetAttemptCount(_eventStore).ShouldEqual(10);
    [Fact] void should_keep_the_store_pending() => _retries.Pending.ShouldContain(_eventStore);
    [Fact] void should_cap_the_backoff() => _timeProvider.Delays.ShouldEqual(new[] { 5, 10, 20, 40, 60, 60, 60, 60, 60, 60, 60 }.Select(_ => TimeSpan.FromSeconds(_)));
}
