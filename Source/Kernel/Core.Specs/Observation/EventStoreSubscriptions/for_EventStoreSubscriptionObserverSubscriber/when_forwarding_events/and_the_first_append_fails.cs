// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_the_first_append_fails : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result = null!;

    void Establish() => _appendResultFor = _ => AppendResult.Failed(CorrelationId.New(), [new AppendError("private-error-marker")]);

    async Task Because() => _result = await Forward();

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_acknowledge_any_event() => _result.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_name_the_failure_kind() => _result.ExceptionMessages.Single().ShouldContain("append error");
    [Fact] void should_name_the_event_type() => _result.ExceptionMessages.Single().ShouldContain("event-type-1");
    [Fact] void should_not_expose_the_append_error_or_event_content() => string.Join(' ', _result.ExceptionMessages).ShouldNotContain("private-");
    [Fact] void should_not_attempt_later_events() => AppendCalls().ShouldEqual(1);
}
