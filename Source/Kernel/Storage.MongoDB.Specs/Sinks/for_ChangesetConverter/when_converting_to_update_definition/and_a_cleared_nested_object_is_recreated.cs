// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_cleared_nested_object_is_recreated : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    BsonDocument _clear;
    BsonDocument _recreate;
    IReadOnlyList<string> _nullParents;

    void Establish()
    {
        var path = new PropertyPath("outer.info");
        var leaf = new PropertyPath("outer.info.name");
        dynamic cleared = new ExpandoObject();
        cleared.outer = new ExpandoObject();
        cleared.outer.info = null;
        dynamic changed = new ExpandoObject();
        changed.outer = new ExpandoObject();
        changed.outer.info = new ExpandoObject();
        changed.outer.info.name = "Again";
        changed.outer.info.other = "Also";

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns((ExpandoObject)cleared);
        _changeset.Changes.Returns([
            new PropertiesChanged<ExpandoObject>((ExpandoObject)changed, [
                new PropertyDifference(leaf, null, "Again")]),
            new PropertiesChanged<ExpandoObject>((ExpandoObject)changed, [
                new PropertyDifference(new PropertyPath("outer.info.other"), null, "Also")])]);
        _mongoDBConverter.ToMongoDBProperty(Arg.Any<PropertyPath>(), Arg.Any<ArrayIndexers>())
            .Returns(call => new MongoDBProperty(((PropertyPath)call[0]).Path, []));
        _mongoDBConverter.ToBsonValue(Arg.Any<object?>(), Arg.Any<PropertyPath>())
            .Returns(call => new BsonString((string)call[0]));
        _mongoDBConverter.ToBsonValue(Arg.Any<EventSequenceNumber>()).Returns(new BsonInt64(42));
    }

    async Task Because()
    {
        var key = new Key("key", ArrayIndexers.NoIndexers);
        var clearChangeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        clearChangeset.Changes.Returns([new NestedCleared(new PropertyPath("outer.info"), ArrayIndexers.NoIndexers)]);
        var clearResult = await _converter.ToUpdateDefinition(key, clearChangeset, 42UL);
        _clear = clearResult.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
        var recreateResult = await _converter.ToUpdateDefinition(key, _changeset, 43UL);
        _recreate = recreateResult.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
        _nullParents = recreateResult.NullParentPaths;
    }

    [Fact] void should_unset_the_nested_path_on_clear() => _clear["$unset"].AsBsonDocument.Contains("outer.info").ShouldBeTrue();
    [Fact] void should_not_write_null_on_clear() => (_clear.Contains("$set") && _clear["$set"].AsBsonDocument.Contains("outer.info")).ShouldBeFalse();
    [Fact] void should_set_the_first_leaf() => _recreate["$set"]["outer.info.name"].AsString.ShouldEqual("Again");
    [Fact] void should_set_the_leaf_from_the_later_change() => _recreate["$set"]["outer.info.other"].AsString.ShouldEqual("Also");
    [Fact] void should_never_set_the_plaintext_parent() => _recreate["$set"].AsBsonDocument.Contains("outer.info").ShouldBeFalse();
    [Fact] void should_conditionally_unset_the_missing_parent_before_writing() => _nullParents.ShouldContain("outer.info");
    [Fact] void should_not_unset_the_existing_outer_object() => _nullParents.ShouldNotContain("outer");
}
