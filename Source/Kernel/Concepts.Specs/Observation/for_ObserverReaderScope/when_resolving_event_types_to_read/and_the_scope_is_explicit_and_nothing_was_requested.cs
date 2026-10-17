// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_resolving_event_types_to_read;

public class and_the_scope_is_explicit_and_nothing_was_requested : Specification
{
    static readonly EventType _first = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    static readonly EventType _second = new("22222222-2222-2222-2222-222222222222", EventTypeGeneration.First);
    IReadOnlyList<EventType>? _result;

    void Because() => _result = ObserverReaderScope.Explicit([_first, _second]).EventTypesToRead([]);

    [Fact] void should_read_the_event_types_of_the_scope() => _result.ShouldContainOnly([_first, _second]);
}
