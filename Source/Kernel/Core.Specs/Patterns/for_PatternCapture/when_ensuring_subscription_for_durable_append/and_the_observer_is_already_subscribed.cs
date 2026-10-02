// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_ensuring_subscription_for_durable_append;

public class and_the_observer_is_already_subscribed : given.a_pattern_capture
{
    void Establish()
    {
        EventTypesAre("CustomerNamed");
        _observer.GetSubscription().Returns(new ObserverSubscription(ObserverId.Unspecified, ObserverKey.NotSet, [new EventType("CustomerNamed", EventTypeGeneration.First)], typeof(IPatternCaptureSubscriber), SiloAddress.Zero, null));
    }

    async Task Because() => await _capture.RecoverSubscription(_eventStore, _namespace);

    [Fact] async Task should_check_readiness() => await _observer.Received(1).NeedsSubscriptionRecovery(Arg.Any<IEnumerable<EventType>>());
    [Fact] async Task should_not_restart_healthy_initialization() => await _observer.DidNotReceive().RecoverStalledSubscription<IPatternCaptureSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());
    [Fact] async Task should_read_the_effective_subscription() => await _observer.Received(1).GetSubscription();
    [Fact] async Task should_not_resubscribe_or_lift_quarantine() => await _observer.DidNotReceive().Subscribe<IPatternCaptureSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());
}
