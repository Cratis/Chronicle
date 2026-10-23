// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_the_run_is_cancelled_mid_page : given.a_performing_job_step
{
    readonly CancellationTokenSource _cancellation = new();

    void Establish() => _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(async _ =>
        {
            await _cancellation.CancelAsync();
            return ObserverSubscriberResult.Ok(1UL);
        });

    async Task Because() => await _jobStep.InvokePerformStep(_performState, _cancellation.Token);
    void Destroy() => _cancellation.Dispose();

    [Fact] void should_flush_the_already_handled_prefix() => _observer.Received(1).ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Is<IReadOnlyList<HandledEventsReport>>(r => r.Count == 1 && r[0].HandledUpTo == (EventSequenceNumber)1UL));
    [Fact] void should_not_dispatch_a_later_run() => _observerSubscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
