// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_recovering;

public class and_the_subscription_is_healthy_and_the_definition_has_a_higher_generation : given.a_pattern_capture
{
    void Establish()
    {
        EventTypesAre("CustomerNamed");
        EventType[] subscriptionTypes = [new EventType("CustomerNamed", EventTypeGeneration.First)];
        var key = new ObserverKey(PatternCapture.ObserverIdentifier, _eventStore, _namespace, EventSequenceId.Log);
        _observer.NeedsSubscriptionRecovery(Arg.Any<IEnumerable<EventType>>()).Returns(false);
        _observer.GetSubscription().Returns(new ObserverSubscription(key.ObserverId, key, subscriptionTypes, typeof(IPatternCaptureSubscriber), SiloAddress.Zero, IsReplayable: false));
        _reactors.Has(key.ObserverId).Returns(true);
        _reactors.Get(key.ObserverId).Returns(new ReactorDefinition(
            key.ObserverId,
            ReactorOwner.Kernel,
            EventSequenceId.Log,
            [new EventTypeWithKeyExpression(new EventType("CustomerNamed", 2), WellKnownExpressions.EventSourceId)],
            false));
    }

    Task Because() => _capture.RecoverSubscription(_eventStore, _namespace);

    [Fact] async Task should_not_save_the_definition() => await _reactors.DidNotReceive().Save(Arg.Any<ReactorDefinition>());
}
