// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_recovering;

public class and_the_subscription_is_healthy_but_the_definition_is_missing : given.a_pattern_capture
{
    EventType[] _subscriptionTypes;

    void Establish()
    {
        EventTypesAre("CustomerNamed");
        _subscriptionTypes = [new EventType("CustomerNamed", 2), new EventType("CustomerMoved", EventTypeGeneration.First)];
        var key = new ObserverKey(PatternCapture.ObserverIdentifier, _eventStore, _namespace, EventSequenceId.Log);
        _observer.NeedsSubscriptionRecovery(Arg.Any<IEnumerable<EventType>>()).Returns(false);
        _observer.GetSubscription().Returns(new ObserverSubscription(key.ObserverId, key, _subscriptionTypes, typeof(IPatternCaptureSubscriber), SiloAddress.Zero, IsReplayable: false));
        _reactors.Has(key.ObserverId).Returns(false);
    }

    Task Because() => _capture.RecoverSubscription(_eventStore, _namespace);

    [Fact] async Task should_save_the_subscriptions_event_types() => await _reactors.Received(1).Save(Arg.Is<ReactorDefinition>(definition => definition.EventTypes.Select(type => type.EventType).ToHashSet().SetEquals(_subscriptionTypes)));
    [Fact] async Task should_save_exactly_once() => await _reactors.Received(1).Save(Arg.Any<ReactorDefinition>());
}
