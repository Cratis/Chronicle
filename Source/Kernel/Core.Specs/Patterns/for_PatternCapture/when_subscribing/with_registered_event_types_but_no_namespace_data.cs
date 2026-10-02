// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing;

/// <summary>
/// A new namespace has no data yet. Capture must subscribe before its first append rather than wait for
/// another event type registration or a restart.
/// </summary>
public class with_registered_event_types_but_no_namespace_data : given.a_pattern_capture
{
    void Establish()
    {
        EventTypesAre("ExpenseReportSubmitted");
        _namespaceStorage.HasData().Returns(Task.FromResult(false));
    }

    Task Because() => _capture.Subscribe(_eventStore, _namespace);

    [Fact]
    async Task should_subscribe_an_observer_for_the_first_append() =>
        await _observer.Received(1).SubscribeAdditively<IPatternCaptureSubscriber>(
            Arg.Any<ObserverType>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>());
}
