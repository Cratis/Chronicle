// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.InMemory.Keys.for_ObserverKeyIndexes.when_getting_keys;

/// <summary>
/// The in-memory index holds the keys that were added to it and has no events to read, so it enumerates what it holds
/// whichever scope is asked for - and each namespace holds its own.
/// </summary>
public class and_two_namespaces_have_different_scopes : Specification
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    static readonly ObserverKey _observerKey = new("observer", "store", "namespace", EventSequenceId.Log);
    List<Key> _wildcardNamespaceKeys;
    List<Key> _explicitNamespaceKeys;

    async Task Because()
    {
        var wildcardNamespace = new ObserverKeyIndexes();
        var explicitNamespace = new ObserverKeyIndexes();

        var wildcardIndex = await wildcardNamespace.GetFor(_observerKey, ObserverReaderScope.Wildcard);
        var explicitIndex = await explicitNamespace.GetFor(_observerKey, ObserverReaderScope.Explicit([_eventType]));
        await wildcardIndex.Add((Key)"one");
        await explicitIndex.Add((Key)"two");

        _wildcardNamespaceKeys = await Enumerate(wildcardIndex);
        _explicitNamespaceKeys = await Enumerate(explicitIndex);
    }

    [Fact] void should_enumerate_the_keys_of_the_wildcard_namespace() => _wildcardNamespaceKeys.ShouldEqual([(Key)"one"]);
    [Fact] void should_enumerate_the_keys_of_the_explicit_namespace() => _explicitNamespaceKeys.ShouldEqual([(Key)"two"]);

    static async Task<List<Key>> Enumerate(IObserverKeyIndex index)
    {
        var keys = new List<Key>();
        await foreach (var key in index.GetKeys(EventSequenceNumber.First))
        {
            keys.Add(key);
        }

        return keys;
    }
}
