// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_webhook_defers_after_partial_progress : given.a_performing_job_step
{
    PartitionDeferral _deferral;
    Catch<JobStepResult> _result;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now, now.AddMinutes(1), new(43));
        _eventCursor.Current.Returns([
            AppendedEvent.EmptyWithEventSequenceNumber(42),
            AppendedEvent.EmptyWithEventSequenceNumber(43)]);
        var subscriber = Substitute.For<IWebhookObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(new ObserverSubscriberResult(ObserverSubscriberState.Deferred, new(42), [], string.Empty) { Deferral = _deferral });
        _jobStep.SetupForTesting(_observer, subscriber, "some-partition");
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_persist_the_first_unhandled_event() => _observer.Received(1).PartitionDeferred("some-partition", _deferral, null, null);
    [Fact] void should_not_report_an_ordinary_failure() => _observer.DidNotReceive().PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>(), null, null);
    [Fact] void should_stop_without_claiming_completion() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_count_only_the_handled_prefix() => _observer.Received(1).ReportHandledEvents(Arg.Any<Key>(), Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(counts => counts.Values.Sum(count => (long)count.Value) == 1), Arg.Any<JobStepId>(), new(42));
}
