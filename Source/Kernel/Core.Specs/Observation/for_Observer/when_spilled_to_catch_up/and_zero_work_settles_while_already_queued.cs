// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_spilled_to_catch_up;

public class and_zero_work_settles_while_already_queued : given.an_observer_settling_after_zero_work
{
    Task Because() => _observer.CaughtUp(_jobId, 0UL);

    [Fact] void should_keep_the_physical_subscription() => _queued.ShouldBeTrue();
    [Fact] async Task should_not_subscribe_again() =>
        await _appendedEventsQueues.DidNotReceive().Subscribe(_observerKey, Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] async Task should_preserve_backoff() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    [Fact] async Task should_not_start_another_catch_up() =>
        await _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
