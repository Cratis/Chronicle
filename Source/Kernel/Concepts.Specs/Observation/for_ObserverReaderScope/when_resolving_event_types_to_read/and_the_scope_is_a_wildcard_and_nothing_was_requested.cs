// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_resolving_event_types_to_read;

public class and_the_scope_is_a_wildcard_and_nothing_was_requested : Specification
{
    IReadOnlyList<EventType>? _result;

    void Because() => _result = ObserverReaderScope.Wildcard.EventTypesToRead([]);

    [Fact] void should_read_every_event_type() => _result.ShouldBeNull();
}
