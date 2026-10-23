// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_the_dispatched_page_throws : given.a_page_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.Bucket = new(0, 1, 1);
        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromException<ObserverSubscriberResult>(new InvalidOperationException("ambiguous page failure")));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_redispatch_any_events() => _pageSubscriber.Received(1).OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_fall_back_to_partition_calls() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_keep_the_durable_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_guess_a_failed_partition() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.PartitionFailed)).ShouldEqual(0);
    [Fact] void should_stop_with_a_transient_partial_result()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        result.TryGetFullResult<HandleEventsForPartitionResult>(out var partial, out _, new System.Text.Json.JsonSerializerOptions()).ShouldBeFalse();
        partial.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
        partial.TransientFailure.Partition.ShouldEqual((Key)"module");
    }
}
