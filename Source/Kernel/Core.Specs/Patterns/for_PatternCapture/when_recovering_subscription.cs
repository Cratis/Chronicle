// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture;

public class when_recovering_subscription : given.a_pattern_capture
{
    void Establish() => EventTypesAre("CustomerNamed");

    Task Because() => _capture.RecoverSubscription(_eventStore, _namespace);

    [Fact] async Task should_delegate_the_decision_to_the_observer() => await _observer.Received(1).RecoverStalledSubscription<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Is<IEnumerable<EventType>>(types => types.Single().Id == "CustomerNamed"), SiloAddress.Zero, isReplayable: false);
    [Fact] async Task should_not_use_the_quarantine_releasing_subscribe() => await _observer.DidNotReceive().Subscribe<IPatternCaptureSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object>(), Arg.Any<bool>(), Arg.Any<ObserverFilters>());
}
