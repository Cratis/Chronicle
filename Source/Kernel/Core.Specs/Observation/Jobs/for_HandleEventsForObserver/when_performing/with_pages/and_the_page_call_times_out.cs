// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_the_page_call_times_out : given.a_page_job_step
{
    void Establish() => _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(Task.FromException<ObserverSubscriberResult>(new SubscriberCallTimedOut("module", TimeSpan.FromSeconds(1))));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_fall_back_to_consecutive_runs() => _handledBatches.Select(_ => _.Partition).ShouldEqual([(Key)"module", (Key)"feature", (Key)"module"]);
    [Fact] void should_finish_at_the_last_event() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
