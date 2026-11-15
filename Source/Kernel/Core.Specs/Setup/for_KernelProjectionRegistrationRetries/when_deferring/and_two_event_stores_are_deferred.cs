// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.when_deferring;

public class and_two_event_stores_are_deferred : given.a_retry_loop
{
    readonly EventStoreName _otherStore = "other-store";

    async Task Because()
    {
        _retries.Defer(_eventStore);
        _retries.Defer(_otherStore);
        (await _timeProvider.Next()).Complete();
        await _logger.Next();
    }

    [Fact] void should_remove_only_the_successful_store() => _retries.Pending.ShouldEqual<EventStoreName>([_otherStore]);
    [Fact] async Task should_not_wait_for_the_other_store() => await _kernelProjections.Received(1).DiscoverAndRegister(_eventStore);
    [Fact] async Task should_leave_the_other_registration_scheduled() => await _kernelProjections.DidNotReceive().DiscoverAndRegister(_otherStore);
}
