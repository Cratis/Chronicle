// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable;

public class when_dispatch_to_an_observer_fails : given.an_unreachable_observer
{
    Task Because() => DispatchFirstEvent();

    [Fact] async Task should_unsubscribe_the_observer_from_live_delivery() => (await _queue.GetSubscriptions()).ShouldBeEmpty();
    [Fact] void should_trigger_catch_up_for_the_observer() => _observer.Received(1).CatchUp();
}
