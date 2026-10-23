// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes.for_ObserverKeyIndexes.when_getting_keys;

public class and_the_scope_is_explicit_and_empty : given.events_of_two_types_from_two_event_sources
{
    IEnumerable<string> _keys;

    async Task Because() => _keys = await KeysOf(await IndexesFor(_namespace.Value).GetFor(_observerKey, ObserverReaderScope.Explicit([])));

    [Fact] void should_enumerate_no_keys() => _keys.ShouldBeEmpty();
}
