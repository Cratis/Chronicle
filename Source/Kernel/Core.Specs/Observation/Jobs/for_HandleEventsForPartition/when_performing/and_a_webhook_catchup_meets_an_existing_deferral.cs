// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_webhook_catchup_meets_an_existing_deferral : given.a_performing_job_step
{
    IWebhookObserverSubscriber _webhook;
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.EventObservationState = EventObservationState.None;
        _observer.CanResumeFailedPartition("some-partition", null, null, Guid.Empty).Returns(false);
        _webhook = Substitute.For<IWebhookObserverSubscriber>();
        _jobStep.SetupForTesting(_observer, _webhook, "some-partition");
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_deliver_before_the_deadline() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_stop_without_claiming_completion() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_advance_past_the_unhandled_events() => _observer.DidNotReceive().ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(), Arg.Any<JobStepId>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_not_change_the_inclusive_resume_position() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
