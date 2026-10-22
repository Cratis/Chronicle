// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_the_run_is_stopped_mid_page : given.a_performing_job_step
{
    void Establish() => _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(call =>
        {
            _jobStep.Run.Close();
            return Task.FromResult(ObserverSubscriberResult.Ok(call.ArgAt<IEnumerable<AppendedEvent>>(1).Last().Context.SequenceNumber));
        });

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_count_work_after_the_checkpoint_is_rejected() => _observer.DidNotReceive().ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>());
    [Fact] void should_not_advance_the_durable_checkpoint() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
