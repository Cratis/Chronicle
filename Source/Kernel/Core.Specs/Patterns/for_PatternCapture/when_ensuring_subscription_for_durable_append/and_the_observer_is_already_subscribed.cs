// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_ensuring_subscription_for_durable_append;

public class and_the_observer_is_already_subscribed : given.a_pattern_capture
{
    bool _subscribed;

    void Establish()
    {
        var key = new ObserverKey(PatternCapture.ObserverIdentifier, _eventStore, _namespace, EventSequenceId.Log);
        _observer.GetSubscription().Returns(new ObserverSubscription(key.ObserverId, key, [], typeof(IPatternCaptureSubscriber), SiloAddress.Zero));
    }

    async Task Because() => _subscribed = await _capture.EnsureSubscribedForDurableAppend(_eventStore, _namespace);

    [Fact] void should_report_the_existing_subscription() => _subscribed.ShouldBeTrue();
    [Fact] async Task should_not_read_event_types_again() => await _eventTypes.DidNotReceive().GetLatestForAllEventTypes();
    [Fact] async Task should_not_rewrite_the_definition() => await _reactors.DidNotReceive().Save(Arg.Any<ReactorDefinition>());
    [Fact] async Task should_not_resubscribe_or_lift_quarantine() => await _observer.DidNotReceive().Subscribe<IPatternCaptureSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());
}
