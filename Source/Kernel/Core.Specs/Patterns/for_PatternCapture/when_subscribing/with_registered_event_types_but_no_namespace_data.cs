// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing;

/// <summary>
/// Event types being registered somewhere in the store does not mean this particular namespace has ever been
/// used - it could be a namespace registered up front (for example one per tenant) that has never had a single
/// event appended to it. Subscribing an observer for it would materialize its storage for nothing.
/// </summary>
public class with_registered_event_types_but_no_namespace_data : given.a_pattern_capture
{
    async Task Because()
    {
        EventTypesAre("ExpenseReportSubmitted");
        _namespaceStorage.HasData().Returns(Task.FromResult(false));
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
