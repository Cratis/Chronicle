// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_the_parent_was_null_before_nested_properties_changed : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    UpdateDefinitionAndArrayFilters _result;
    BsonDocument _rendered;

    void Establish()
    {
        dynamic initial = new ExpandoObject();
        initial.outer = new ExpandoObject();
        initial.outer.info = null;
        dynamic info = new ExpandoObject();
        info.name = "Again";
        info.description = "Other";
        dynamic changed = new ExpandoObject();
        changed.outer = new ExpandoObject();
        changed.outer.info = info;
        var parent = new PropertyPath("outer.info");
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns((ExpandoObject)initial);
        _changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>((ExpandoObject)changed,
            [new PropertyDifference(new PropertyPath("outer.info.name"), null, "Again"),
             new PropertyDifference(new PropertyPath("outer.info.description"), null, "Other")])]);
        _mongoDBConverter.ToMongoDBProperty(parent, ArrayIndexers.NoIndexers).Returns(new MongoDBProperty("outer.info", []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), parent).Returns(new BsonDocument { ["name"] = "Again", ["description"] = "Other" });
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(BsonValue.Create(42UL));
    }

    async Task Because()
    {
        _result = await _converter.ToUpdateDefinition(new Key("probe-1", ArrayIndexers.NoIndexers), _changeset, 42UL);
        _rendered = _result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_set_the_whole_parent() => _rendered["$set"]["outer.info"].AsBsonDocument["name"].AsString.ShouldEqual("Again");
    [Fact] void should_preserve_the_other_property() => _rendered["$set"]["outer.info"].AsBsonDocument["description"].AsString.ShouldEqual("Other");
    [Fact] void should_not_set_a_leaf_beside_its_parent() => _rendered["$set"].AsBsonDocument.ElementCount.ShouldEqual(1);
    [Fact] void should_indicate_changes() => _result.hasChanges.ShouldBeTrue();
}
