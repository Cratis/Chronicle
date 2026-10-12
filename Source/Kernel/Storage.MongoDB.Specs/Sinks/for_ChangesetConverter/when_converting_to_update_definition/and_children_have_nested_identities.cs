// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_children_have_nested_identities : Specification
{
    ChangesetConverter _converter;
    ExpandoObject _child;
    BsonDocument _addition;
    BsonDocument _update;
    BsonDocument _removal;
    UpdateDefinitionAndArrayFilters _changed;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"children":{"type":"array","items":{"type":"object","properties":{"identity":{"type":"object","properties":{"id":{"type":"string"},"topic":{"type":"string"}}}}}}}}
            """);
        var readModel = new ReadModelDefinition("nested-identities", "NestedIdentities", "NestedIdentities", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        _converter = new ChangesetConverter(readModel, new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance), Substitute.For<ISinkCollections>(), expando);
        dynamic child = new ExpandoObject();
        child.identity = new ExpandoObject();
        child.identity.id = "A";
        child.identity.topic = "First";
        _child = child;
    }

    async Task Because()
    {
        _addition = Render(await Convert(new ChildAdded(_child, "[children]", "identity.id", "A", ArrayIndexers.NoIndexers)));
        dynamic initial = new ExpandoObject();
        initial.children = new List<ExpandoObject> { _child };
        var indexers = new ArrayIndexers([new ArrayIndexer("[children]", "identity.id", "A")]);
        _changed = await Convert(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[children].identity.topic", "First", "Updated", indexers)]), initial);
        _update = Render(_changed);
        _removal = Render(await Convert(new ChildRemoved(_child, "[children]", "identity.id", "A")));
    }

    [Fact] void should_add_the_nested_identity() => _addition["$push"]["children"]["identity"]["_id"].AsString.ShouldEqual("A");
    [Fact] void should_update_the_nested_child_property() => _update["$set"]["children.$[children].identity.topic"].AsString.ShouldEqual("Updated");
    [Fact] void should_filter_updates_by_the_complete_identity_path() => _changed.ArrayFilters.Single().Document["children.identity._id"].AsString.ShouldEqual("A");
    [Fact] void should_remove_by_the_complete_identity_path() => _removal["$pull"]["children"]["identity._id"].AsString.ShouldEqual("A");

    Task<UpdateDefinitionAndArrayFilters> Convert(Change change, ExpandoObject? initial = null)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial ?? new ExpandoObject());
        changeset.Changes.Returns([change]);
        return _converter.ToUpdateDefinition(new Key("parent", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
    }

    static BsonDocument Render(UpdateDefinitionAndArrayFilters result) => result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
}
