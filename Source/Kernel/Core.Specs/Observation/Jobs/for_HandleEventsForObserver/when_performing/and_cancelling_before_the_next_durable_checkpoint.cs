// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_cancelling_before_the_next_durable_checkpoint : given.a_debouncing_job_step
{
    readonly CancellationTokenSource _cancellation = new();
    Catch<JobStepResult> _result;
    Exception? _error;

    void Establish() => _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(async call =>
        {
            await _cancellation.CancelAsync();
            return ObserverSubscriberResult.Ok(1UL);
        });

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(async () =>
        _result = await _jobStep.InvokePerformStep(_performState, _cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_build_the_cancelled_result_without_accessing_persistent_state() => _error.ShouldBeNull();
    [Fact] void should_not_complete_the_cancelled_range() => _result.Match(result => result.TryGetError(out _), _ => false).ShouldBeTrue();
    [Fact] void should_keep_the_last_durable_checkpoint() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
