// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes.for_ObserverKeyIndexes.when_getting_keys;

/// <summary>
/// The scope is the caller's, per namespace: the same observer in two namespaces reads what each namespace's scope says.
/// </summary>
public class and_two_namespaces_have_different_scopes : given.events_of_two_types_from_two_event_sources
{
    IEnumerable<string> _firstNamespaceKeys;
    IEnumerable<string> _secondNamespaceKeys;

    async Task Because()
    {
        _firstNamespaceKeys = await KeysOf(await IndexesFor("first-namespace").GetFor(_observerKey, ObserverReaderScope.Explicit([_firstType])));
        _secondNamespaceKeys = await KeysOf(await IndexesFor("second-namespace").GetFor(_observerKey, ObserverReaderScope.Explicit([_secondType])));
    }

    [Fact] void should_use_the_scope_of_the_first_namespace() => _firstNamespaceKeys.ShouldContainOnly([_firstSource.Value]);
    [Fact] void should_use_the_scope_of_the_second_namespace() => _secondNamespaceKeys.ShouldContainOnly([_secondSource.Value]);
}
