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

public class and_a_legacy_null_inside_an_array_is_recreated : Specification
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
        var indexers = new ArrayIndexers([new ArrayIndexer("[items]", "id", "first")]);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[items].info.name", null, "Again", indexers)])]);
        _result = await changesetConverter.ToUpdateDefinition(new Key("root", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _update = _result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_keep_the_leaf_write() => _update["$set"]["items.$[items].info.name"].AsString.ShouldEqual("Again");
    [Fact] void should_prepare_an_unset_for_the_null_element_parent() => _result.NullArrayParents.Single().Path.ShouldEqual("items.$[items].info");
    [Fact] void should_filter_on_the_same_identifier_and_null_parent() => _result.NullArrayParents.Single().ArrayFilters.Single().Document.ShouldEqual(new BsonDocument { { "items._id", "first" }, { "items.info", new BsonDocument("$type", "null") } });
    [Fact] void should_not_unset_the_entire_array() => _result.NullParentPaths.ShouldBeEmpty();
}
