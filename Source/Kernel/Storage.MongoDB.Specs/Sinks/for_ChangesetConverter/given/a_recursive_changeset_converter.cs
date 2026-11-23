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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.given;

public class a_recursive_changeset_converter : Specification
{
    protected ArrayIndexers _indexers;
    protected UpdateDefinitionAndArrayFilters _result;
    protected BsonDocument _update;
    ChangesetConverter _converter;
    ExpandoObject _initialState;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"features":{"type":"array","items":{"$ref":"#/definitions/feature"}}},
             "definitions":{"feature":{"type":"object","properties":{
                "id":{"type":"string"},"name":{"type":"string"},
                "subFeatures":{"type":"array","items":{"$ref":"#/definitions/feature"}},
                "slices":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"}}}}
             }}}}
            """);
        var readModel = new ReadModelDefinition("recursive", "Recursive", "Recursive", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        _converter = new ChangesetConverter(readModel, new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance), Substitute.For<ISinkCollections>(), expando);
        _indexers = new ArrayIndexers(
        [
            new("[features]", "id", "root"),
            new("[features].[subFeatures]", "id", "parent"),
            new("[features].[subFeatures]", "id", "child")
        ]);
        dynamic child = Child("child");
        child.subFeatures = new List<ExpandoObject>();
        child.slices = new List<ExpandoObject>();
        dynamic parent = Child("parent");
        parent.subFeatures = new List<ExpandoObject> { child };
        dynamic root = Child("root");
        root.subFeatures = new List<ExpandoObject> { parent };
        dynamic initial = new ExpandoObject();
        initial.features = new List<ExpandoObject> { root };
        _initialState = initial;
    }

    protected static ExpandoObject Child(string id)
    {
        dynamic child = new ExpandoObject();
        child.id = id;
        child.name = id;
        return child;
    }

    protected async Task Convert(Change change)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(_initialState);
        changeset.Changes.Returns([change]);
        _result = await _converter.ToUpdateDefinition(new Key("module", _indexers), changeset, EventSequenceNumber.Unavailable);
        _update = _result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
