// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_the_collection_is_nested_in_multiple_arrays : given.a_child_removal_from_all
{
    void Establish() => _changeset.Changes.Returns([new ChildRemovedFromAll("orders.[parents].details.[groups].[items]", "Id", "item", ArrayIndexers.NoIndexers)]);

    async Task Because() => await _sink.ApplyChanges(new Key("order", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.First);

    [Fact] void should_pull_from_every_parent_and_group() => _update.ShouldEqual(BsonDocument.Parse("""{ "$pull": { "orders.parents.$[a1].details.groups.$[a3].items": { "_id": "item" } } }"""));
    [Fact] void should_skip_documents_without_the_outer_array() => _filter.ShouldEqual(BsonDocument.Parse("""{ "orders.parents": { "$type": 4 } }"""));
    [Fact] void should_skip_missing_or_null_nested_arrays() => _arrayFilters.ShouldEqual<IEnumerable<BsonDocument>>([BsonDocument.Parse("""{ "a1.details.groups": { "$type": "array" } }"""), BsonDocument.Parse("""{ "a3.items": { "$type": "array" } }""")]);
}
