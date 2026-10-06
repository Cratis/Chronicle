// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_removing_a_child_from_all;

public class and_leaf_values_include_null_and_scalars : given.a_child_removal_from_all
{
    void Establish()
    {
        _leafValues = [BsonNull.Value, new BsonString("scalar"), new BsonInt32(42), new BsonArray(), new BsonArray { new BsonDocument("_id", "item") }];
        _changeset.Changes.Returns([new ChildRemovedFromAll("details.[items]", "id", "item", ArrayIndexers.NoIndexers)]);
    }

    async Task Because() => await _sink.ApplyChanges(new Key("order", ArrayIndexers.NoIndexers), _changeset, EventSequenceNumber.First);

    [Fact] void should_target_all_array_leaves() => _matchedLeafValues.ShouldEqual<IEnumerable<BsonValue>>(_leafValues.Skip(3));
    [Fact] void should_not_target_null_leaves() => _matchedLeafValues.ShouldNotContain(BsonNull.Value);
    [Fact] void should_not_target_scalar_leaves() => _matchedLeafValues.Any(value => value.IsString || value.IsInt32).ShouldBeFalse();
    [Fact] void should_guard_the_nested_leaf_path() => _filter.ShouldEqual(BsonDocument.Parse("""{ "details.items": { "$type": 4 } }"""));
}
