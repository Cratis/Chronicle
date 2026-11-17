// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_run_is_stopped_while_a_failed_batch_is_being_reported : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish() =>
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ =>
            {
                _jobStep.Run!.Close();
                return Task.FromResult(new ObserverSubscriberResult(ObserverSubscriberState.Failed, new(42), ["boom"], "stack"));
            });

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_end_the_step_as_cancelled() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_record_the_batch_as_handled() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.IsActualValue.ShouldBeFalse();
    [Fact] void should_not_report_the_partition_as_failed() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed)).ShouldEqual(0);
}
