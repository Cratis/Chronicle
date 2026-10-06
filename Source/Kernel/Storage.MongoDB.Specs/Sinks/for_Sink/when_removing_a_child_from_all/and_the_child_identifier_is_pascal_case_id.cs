// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_the_child_identifier_is_pascal_case_id : given.a_child_removal_from_all
{
    void Establish() => _changeset.Changes.Returns([new ChildRemovedFromAll("[items]", "Id", "item", ArrayIndexers.NoIndexers)]);

    async Task Because() => await _sink.ApplyChanges(new Key("order", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.First);

    [Fact] void should_pull_children_using_the_stored_identifier_name() => _update.ShouldEqual(BsonDocument.Parse("""{ "$pull": { "items": { "_id": "item" } } }"""));
}
