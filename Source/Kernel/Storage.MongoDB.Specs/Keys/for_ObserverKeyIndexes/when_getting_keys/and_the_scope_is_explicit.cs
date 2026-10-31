// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Keys.for_ObserverKeyIndexes.when_getting_keys;

public class and_the_scope_is_explicit : given.a_namespace_collection
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);

    async Task Because()
    {
        var index = await _indexes.GetFor(new ObserverKey("observer", "store", "namespace", EventSequenceId.Log), ObserverReaderScope.Explicit([_eventType]));
        await index.GetKeys(EventSequenceNumber.First).GetAsyncEnumerator().MoveNextAsync();
    }

    [Fact] void should_narrow_by_event_type() => RenderedFilter.ToString().ShouldContain("$in");
    [Fact] void should_narrow_to_the_event_type_of_the_scope() => RenderedFilter.ToString().ShouldContain(_eventType.Id.ToString());
}
