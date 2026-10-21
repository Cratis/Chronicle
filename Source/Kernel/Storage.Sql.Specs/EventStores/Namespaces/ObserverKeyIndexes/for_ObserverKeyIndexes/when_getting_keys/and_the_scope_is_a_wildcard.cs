// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes.for_ObserverKeyIndexes.when_getting_keys;

/// <summary>
/// An empty type list has always enumerated no keys on SQL, whereas MongoDB enumerates every key for it. Introducing
/// the scope keeps that as it is; changing it is a behavior change for observers subscribed to all events.
/// </summary>
public class and_the_scope_is_a_wildcard : given.events_of_two_types_from_two_event_sources
{
    IEnumerable<string> _keys;

    async Task Because() => _keys = await KeysOf(await IndexesFor(_namespace.Value).GetFor(_observerKey, ObserverReaderScope.Wildcard));

    [Fact] void should_enumerate_no_keys_as_it_did_for_an_empty_type_list() => _keys.ShouldBeEmpty();
}
