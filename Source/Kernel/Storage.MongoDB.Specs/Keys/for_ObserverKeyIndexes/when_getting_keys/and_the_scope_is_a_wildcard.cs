// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Keys.for_ObserverKeyIndexes.when_getting_keys;

public class and_the_scope_is_a_wildcard : given.a_namespace_collection
{
    async Task Because()
    {
        var index = await _indexes.GetFor(new ObserverKey("observer", "store", "namespace", EventSequenceId.Log), ObserverReaderScope.Wildcard);
        await index.GetKeys(EventSequenceNumber.First).GetAsyncEnumerator().MoveNextAsync();
    }

    [Fact] void should_not_narrow_by_event_type() => RenderedFilter.ToString().ShouldNotContain("$in");
    [Fact] void should_still_start_from_the_requested_sequence_number() => RenderedFilter.ToString().ShouldContain("$gte");
}
