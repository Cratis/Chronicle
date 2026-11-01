// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class and_the_same_observer_fails_twice_before_it_is_spilled : given.an_unreachable_observer
{
    async Task Because()
    {
        await _queue.Enqueue([_firstEvent, _laterEvent]);
        await _queue.AwaitQueueDepletion(500, 10);
    }

    [Fact] void should_trigger_catch_up_once() => _observer.Received(1).CatchUp();
}
