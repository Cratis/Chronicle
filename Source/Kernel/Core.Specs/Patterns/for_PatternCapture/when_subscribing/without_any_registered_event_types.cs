// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing;

/// <summary>
/// An event store nobody has registered an event type with yet has nothing to observe. Subscribing to an empty
/// list would create an observer that can never receive anything and would still have to be reconciled later.
/// </summary>
public class without_any_registered_event_types : given.a_pattern_capture
{
    async Task Because()
    {
        EventTypesAre();
        await _capture.Subscribe(_eventStore, _namespace);
    }

    [Fact] async Task should_not_subscribe_an_observer() =>
        await _observer.DidNotReceive().Subscribe<IPatternCaptureSubscriber>(
            Arg.Any<ObserverType>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>());
}
