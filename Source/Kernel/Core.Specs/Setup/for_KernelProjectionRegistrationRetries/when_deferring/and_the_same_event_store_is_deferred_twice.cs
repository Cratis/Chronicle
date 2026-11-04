// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.when_deferring;

public class and_the_same_event_store_is_deferred_twice : given.a_retry_loop
{
    async Task Because()
    {
        Parallel.For(0, 20, _ => _retries.Defer(_eventStore));
        (await _timeProvider.Next()).Complete();
        await _logger.Next();
    }

    [Fact] async Task should_start_only_one_registration() => await _kernelProjections.Received(1).DiscoverAndRegister(_eventStore);
    [Fact] void should_start_only_one_delay() => _timeProvider.Delays.Count.ShouldEqual(1);
}
