// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes.for_ObserverKeyIndexes.when_getting_keys;

/// <summary>
/// A wildcard scope enumerates every event source's key without narrowing by event type, matching MongoDB.
/// </summary>
public class and_the_scope_is_a_wildcard : given.events_of_two_types_from_two_event_sources
{
    IEnumerable<string> _keys;

    async Task Because() => _keys = await KeysOf(await IndexesFor(_namespace.Value).GetFor(_observerKey, ObserverReaderScope.Wildcard));

    [Fact] void should_enumerate_every_event_source_key() => _keys.ShouldContainOnly([_firstSource.Value, _secondSource.Value]);
}
