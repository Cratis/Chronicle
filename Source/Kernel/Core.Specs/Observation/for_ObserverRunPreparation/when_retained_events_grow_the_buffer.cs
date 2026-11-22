// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_retained_events_grow_the_buffer : Specification
{
    AppendedEvent[] _events;
    AppendedEvent[] _result;

    static AppendedEvent CreateEvent(int index) =>
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new("event", 1), (ulong)index);

    void Establish() => _events = [.. Enumerable.Range(0, 257).Select(CreateEvent)];

    void Because()
    {
        ObserverRunBuffer buffer = default;
        try
        {
            foreach (var appendedEvent in _events)
            {
                buffer.Add(appendedEvent);
            }

            _result = buffer.ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact] void should_keep_every_event_after_multiple_growths() => _result.Length.ShouldEqual(_events.Length);
    [Fact] void should_preserve_order_after_returning_the_rented_buffer() => _result.ShouldEqual<AppendedEvent>(_events);
    [Fact] void should_keep_original_references_in_the_independent_result() => Enumerable.Range(0, _events.Length).All(index => ReferenceEquals(_result[index], _events[index])).ShouldBeTrue();
}
