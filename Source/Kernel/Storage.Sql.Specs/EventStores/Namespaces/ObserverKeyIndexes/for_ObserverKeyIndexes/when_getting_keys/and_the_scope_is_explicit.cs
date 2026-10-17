// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes.for_ObserverKeyIndexes.when_getting_keys;

public class and_the_scope_is_explicit : given.events_of_two_types_from_two_event_sources
{
    IEnumerable<string> _keys;

    async Task Because() => _keys = await KeysOf(await IndexesFor(_namespace.Value).GetFor(_observerKey, ObserverReaderScope.Explicit([_firstType])));

    [Fact] void should_have_the_event_source_of_the_type_in_the_scope() => _keys.ShouldContain(_firstSource.Value);
    [Fact] void should_not_have_the_event_source_of_a_type_outside_the_scope() => _keys.ShouldNotContain(_secondSource.Value);
}
