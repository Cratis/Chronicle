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

public class and_a_primitive_array_element_changes_without_changing_length : Specification
{
    BsonDocument _update;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"involvedUsers":{"type":"array","items":{"type":"string"}}}}
            """);
        var readModel = new ReadModelDefinition("users-model", "UsersModel", "users-model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Reducer, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        var changesetConverter = new ChangesetConverter(readModel, converter, Substitute.For<ISinkCollections>(), expando);

        var initial = new ExpandoObject();
        ((IDictionary<string, object?>)initial)["involvedUsers"] = new List<object> { "a", "cratis" };
        var changed = new ExpandoObject();
        ((IDictionary<string, object?>)changed)["involvedUsers"] = new List<object> { "woksin", "cratis" };
        new ObjectComparer().Compare(initial, changed, out var differences);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial);
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(changed, differences)]);
        var result = await changesetConverter.ToUpdateDefinition(new Key("root", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _update = result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    [Fact] void should_set_the_whole_array() => _update["$set"]["involvedUsers"].ShouldEqual(new BsonArray { "woksin", "cratis" });
}
