// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_redaction_marker : given.a_pinned_subscriber
{
    EventType _redaction;
    void Establish()
    {
        _redaction = new(GlobalEventTypes.Redaction, 1);
        _events = [_events[0] with { Context = _events[0].Context with { EventType = _redaction, ObservationState = EventObservationState.Replay } }];
        var types = _storage.GetEventStore("Lobby").EventTypes;
        types.HasFor(_redaction.Id, _redaction.Generation).Returns(true);
        types.GetDefinition(_redaction.Id).Returns(new EventTypeDefinition(_redaction.Id, EventTypeOwner.Server, false, [new(1, _schema)], []));
        types.GetFor(_redaction.Id, _redaction.Generation).Returns(new EventTypeSchema(_redaction, EventTypeOwner.Server, EventTypeSource.Code, _schema));
    }
    async Task Because() => _result = await Forward();
    [Fact] void should_forward_the_marker_in_its_own_generation() => ForwardedType().ShouldEqual(_redaction);
    [Fact] void should_not_treat_the_marker_as_a_missing_consumer_pin() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_keep_the_marker_content_unchanged() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("private-content-marker");
}
