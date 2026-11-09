// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using ChildrenSortDirection = Cratis.Chronicle.Changes.SortDirection;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_there_is_an_ordered_child_added : given.a_changeset_converter
{
    BsonDocument _appended;
    BsonDocument _prepended;
    BsonDocument _sortedAscending;
    BsonDocument _sortedDescending;

    void Establish()
    {
        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(new MongoDBProperty("children", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(new BsonInt64(42));
        _expandoObjectConverter.ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(new BsonDocument("name", "child"));
    }

    async Task Because()
    {
        _appended = await Push(null);
        _prepended = await Push(new(ChildrenPlacement.Prepend, PropertyPath.NotSet));
        _sortedAscending = await Push(new(ChildrenPlacement.Sorted, new("sortOrder")));
        _sortedDescending = await Push(new(ChildrenPlacement.Sorted, new("sortOrder"), ChildrenSortDirection.Descending));
    }

    [Fact] void should_push_the_child_as_is_for_the_default() => _appended["$push"]["children"].AsBsonDocument.ShouldEqual(new BsonDocument("name", "child"));
    [Fact] void should_push_each_for_prepend() => _prepended["$push"]["children"]["$each"].AsBsonArray.Count.ShouldEqual(1);
    [Fact] void should_push_at_position_zero_for_prepend() => _prepended["$push"]["children"]["$position"].AsInt32.ShouldEqual(0);
    [Fact] void should_not_sort_for_prepend() => _prepended["$push"]["children"].AsBsonDocument.Contains("$sort").ShouldBeFalse();
    [Fact] void should_sort_ascending() => _sortedAscending["$push"]["children"]["$sort"]["sortOrder"].AsInt32.ShouldEqual(1);
    [Fact] void should_not_set_a_position_when_sorted() => _sortedAscending["$push"]["children"].AsBsonDocument.Contains("$position").ShouldBeFalse();
    [Fact] void should_sort_descending() => _sortedDescending["$push"]["children"]["$sort"]["sortOrder"].AsInt32.ShouldEqual(-1);

    async Task<BsonDocument> Push(ChildrenOrdering? ordering)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new ChildAdded(new ExpandoObject(), new PropertyPath("children"), new PropertyPath("id"), "child-key", ArrayIndexers.NoIndexers, ordering)]);
        var result = await _converter.ToUpdateDefinition(new Key("key", ArrayIndexers.NoIndexers), changeset, 42UL);
        return result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
