// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_the_run_is_stopped_while_a_batch_is_being_reported : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish() => _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(_ =>
        {
            _jobStep.Run!.Close();
            return Task.FromResult(ObserverSubscriberResult.Ok(1UL));
        });

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_end_the_step_as_cancelled() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_record_the_batch_as_durable() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_count_the_batch_the_next_run_handles_again() =>
        _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReportHandledEvents)).ShouldEqual(0);
}
