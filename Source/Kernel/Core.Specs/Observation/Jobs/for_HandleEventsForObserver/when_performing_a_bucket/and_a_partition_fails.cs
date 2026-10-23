// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_a_partition_fails : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.Bucket = new(0, 1, 1);
        _eventCursor.Current.Returns([CreateEvent(1UL, "module"), CreateEvent(2UL, "feature"), CreateEvent(3UL, "module"), CreateEvent(4UL, "feature")]);
        _observerSubscriber.OnNext((Key)"feature", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "bad feature"));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_the_partition_once() => _observer.Received(1).PartitionFailed((Key)"feature", (EventSequenceNumber)2UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling, failurePolicy: Arg.Any<WebhookDeliveryFailurePolicy>());
    [Fact] void should_continue_to_the_later_partition() => _handledBatches.SelectMany(batch => batch.SequenceNumbers).ShouldContain((EventSequenceNumber)3UL);
    [Fact] void should_skip_later_events_for_the_failed_partition() => _observerSubscriber.Received(1).OnNext((Key)"feature", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_checkpoint_the_later_success() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_finish_the_bucket()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        result.TryGetFullResult<HandleEventsForPartitionResult>(out _, out _, new System.Text.Json.JsonSerializerOptions()).ShouldBeTrue();
    }
}
