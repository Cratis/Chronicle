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

public class and_the_identified_child_parent_exists : Specification
{
    UpdateDefinitionAndArrayFilters _result;
    BsonDocument _update;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"info":{"type":"object","properties":{"name":{"type":"string"}}}}}}}}
            """);
        var readModel = new ReadModelDefinition("array-model", "ArrayModel", "array-model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        var changesetConverter = new ChangesetConverter(readModel, converter, Substitute.For<ISinkCollections>(), expando);
        dynamic initial = new ExpandoObject();
        dynamic child = new ExpandoObject();
        child.id = "first";
        child.info = new ExpandoObject();
        child.info.name = "Before";
        initial.items = new List<ExpandoObject> { child };
        var indexers = new ArrayIndexers([new ArrayIndexer("[items]", "id", "first")]);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns((ExpandoObject)initial);
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[items].info.name", "Before", "After", indexers)])]);
        _result = await changesetConverter.ToUpdateDefinition(new Key("root", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _update = _result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_keep_the_leaf_write() => _update["$set"]["items.$[items].info.name"].AsString.ShouldEqual("After");
    [Fact] void should_not_unset_an_existing_parent() => _result.NullArrayParents.ShouldBeEmpty();
    [Fact] void should_not_unset_the_array_or_root() => _result.NullParentPaths.ShouldBeEmpty();
}
