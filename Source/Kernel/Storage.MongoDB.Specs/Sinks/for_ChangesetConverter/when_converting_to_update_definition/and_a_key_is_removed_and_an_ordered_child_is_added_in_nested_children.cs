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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_key_is_removed_and_an_ordered_child_is_added_in_nested_children : given.a_changeset_converter
{
    static readonly BsonDocumentArrayFilterDefinition<BsonDocument> _parentFilter = new(new BsonDocument("parent._id", "parent-1"));
    static readonly ArrayIndexers _parentIndexers = new([new ArrayIndexer(new PropertyPath("[parents]"), new PropertyPath("id"), "parent-1")]);

    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    UpdateDefinitionAndArrayFilters _result;
    BsonDocument _update;

    void Establish()
    {
        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call =>
            {
                var path = ((PropertyPath)call[0]).Path;
                var property = path.StartsWith("[parents].slots", StringComparison.Ordinal)
                    ? $"parents.$[parent].{path["[parents].".Length..]}"
                    : "parents.$[parent].items";
                var filters = new ArrayFilters();
                filters.Add(_parentFilter);
                return new MongoDBProperty(property, filters);
            });
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(new BsonInt64(42));
        _expandoObjectConverter.ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(new BsonDocument { { "_id", "item-1" }, { "rank", 2 } });

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns(
        [
            new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [PropertyDifference.Removal(new PropertyPath("[parents].slots.first"), "series-1", _parentIndexers)]),
            new ChildAdded(
                new ExpandoObject(),
                new PropertyPath("[parents].[items]"),
                new PropertyPath("id"),
                "item-1",
                _parentIndexers,
                new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank")))
        ]);
        _changeset = changeset;
    }

    async Task Because()
    {
        _result = await _converter.ToUpdateDefinition(new Key("key", ArrayIndexers.NoIndexers), _changeset, 42UL);
        _update = _result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_unset_the_key_within_the_parent() => _update["$unset"].AsBsonDocument.Contains("parents.$[parent].slots.first").ShouldBeTrue();
    [Fact] void should_push_each_into_the_nested_children() => _update["$push"]["parents.$[parent].items"]["$each"].AsBsonArray.Count.ShouldEqual(1);
    [Fact] void should_sort_the_nested_children_with_the_identifier_breaking_ties() => _update["$push"]["parents.$[parent].items"]["$sort"].AsBsonDocument.ShouldEqual(new BsonDocument { { "rank", 1 }, { "_id", 1 } });
    [Fact] void should_carry_the_parent_array_filter() => _result.ArrayFilters.Count().ShouldEqual(1);
}
