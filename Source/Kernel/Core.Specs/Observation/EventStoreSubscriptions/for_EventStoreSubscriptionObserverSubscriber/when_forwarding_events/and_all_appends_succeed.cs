// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_all_appends_succeed : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result = null!;

    async Task Because() => _result = await Forward();

    [Fact] void should_report_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_report_the_last_forwarded_sequence_number() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_append_every_event() => AppendCalls().ShouldEqual(3);
}
