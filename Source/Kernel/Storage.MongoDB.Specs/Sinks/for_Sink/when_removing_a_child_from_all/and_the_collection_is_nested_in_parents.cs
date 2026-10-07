// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_the_collection_is_nested_in_parents : given.a_child_removal_from_all
{
    void Establish() => _changeset.Changes.Returns([new ChildRemovedFromAll("[parents].[items]", "id", "item", ArrayIndexers.NoIndexers)]);

    async Task Because() => await _sink.ApplyChanges(new Key("order", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.First);

    [Fact] void should_pull_from_every_matching_parent_element() => _update.ShouldEqual(BsonDocument.Parse("""{ "$pull": { "parents.$[a0].items": { "_id": "item" } } }"""));
    [Fact] void should_skip_documents_without_a_parent_array() => _filter.ShouldEqual(BsonDocument.Parse("""{ "parents": { "$type": 4 } }"""));
    [Fact] void should_skip_parents_without_a_child_array() => _arrayFilters.ShouldEqual<IEnumerable<BsonDocument>>([BsonDocument.Parse("""{ "a0.items": { "$type": "array" } }""")]);
}
