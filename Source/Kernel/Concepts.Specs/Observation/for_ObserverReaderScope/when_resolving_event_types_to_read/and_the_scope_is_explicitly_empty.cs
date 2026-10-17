// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_resolving_event_types_to_read;

public class and_the_scope_is_explicitly_empty : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ObserverReaderScope.Explicit([]).EventTypesToRead([]));

    [Fact] void should_refuse_to_supply_a_wildcard_storage_filter() => _error.ShouldBeOfExactType<EmptyObserverReaderScope>();
}
