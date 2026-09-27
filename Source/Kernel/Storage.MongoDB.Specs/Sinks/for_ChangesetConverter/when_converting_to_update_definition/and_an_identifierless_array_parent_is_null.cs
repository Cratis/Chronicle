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

public class and_an_identifierless_array_parent_is_null : Specification
{
    UpdateDefinitionAndArrayFilters _result;
    BsonDocument _update;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"items":{"type":"array","items":{"type":"object","properties":{"info":{"type":"object","properties":{"name":{"type":"string"}}}}}}}}
            """);
        var readModel = new ReadModelDefinition("array-model", "ArrayModel", "array-model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        var changesetConverter = new ChangesetConverter(readModel, converter, Substitute.For<ISinkCollections>(), expando);
        var indexers = new ArrayIndexers([new ArrayIndexer("[items]", PropertyPath.Root, 0)]);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[items].info.name", null, "Again", indexers)])]);
        _result = await changesetConverter.ToUpdateDefinition(new Key("root", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _update = _result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_keep_the_original_leaf_path() => _update["$set"]["items.$[items].info.name"].AsString.ShouldEqual("Again");
    [Fact] void should_keep_the_original_array_filter() => _result.ArrayFilters.Single().Document.ShouldEqual(new BsonDocument("items.", 0));
    [Fact] void should_not_prepare_a_root_pre_unset() => _result.NullParentPaths.ShouldBeEmpty();
    [Fact] void should_not_prepare_an_indexed_pre_unset() => _result.NullArrayParents.ShouldBeEmpty();
}
