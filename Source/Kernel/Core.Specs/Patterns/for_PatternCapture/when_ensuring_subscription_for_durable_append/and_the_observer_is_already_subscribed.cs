// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_ensuring_subscription_for_durable_append;

public class and_the_observer_is_already_subscribed : given.a_pattern_capture
{
    bool _subscribed;

    void Establish() => EventTypesAre("CustomerNamed");

    async Task Because() => _subscribed = await _capture.EnsureSubscribedForDurableAppend(_eventStore, _namespace);

    [Fact] void should_report_the_completed_subscription() => _subscribed.ShouldBeTrue();
    [Fact] async Task should_ensure_initialization_serially() => await _observer.Received(1).EnsureSubscribed<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false);
    [Fact] async Task should_not_trust_an_interleaving_snapshot() => await _observer.DidNotReceive().GetSubscription();
    [Fact] async Task should_not_resubscribe_or_lift_quarantine() => await _observer.DidNotReceive().Subscribe<IPatternCaptureSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());
}
