// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_an_append_fails_in_the_middle : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result = null!;

    void Establish() => _appendResultFor = eventType => eventType.Id == (EventTypeId)"event-type-2"
        ? AppendResult.Failed(CorrelationId.New(), new ConcurrencyViolation("source-1", 1UL, 2UL))
        : AppendResult.Success(CorrelationId.New(), 1UL);

    async Task Because() => _result = await Forward();

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_acknowledge_only_the_forwarded_event() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_name_the_failure_kind() => _result.ExceptionMessages.Single().ShouldContain("concurrency violation");
    [Fact] void should_name_the_failed_event_type() => _result.ExceptionMessages.Single().ShouldContain("event-type-2");
    [Fact] void should_not_attempt_later_events() => AppendCalls().ShouldEqual(2);
}
