// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.for_Database.given;
using KernelDefinition = Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.for_ReadModelDefinitionsStorage;

public class when_saving_a_read_model_definition : a_sqlite_database
{
    KernelDefinition _definition;
    KernelDefinition _restored;
    ReadModelDefinition _stored;

    async Task Because()
    {
        _definition = new KernelDefinition(
            "orders",
            "orders-container",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "orders-projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"}}}"""),
                [2] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"}}}""")
            },
            [new IndexDefinition(new PropertyPath("name"))]);
        var storage = new ReadModelDefinitionsStorage("read-models-store", _database);
        await using (await _database.EventStore("read-models-store"))
        {
        }

        await storage.Save(_definition);
        _restored = await storage.Get(_definition.Identifier);
        _stored = _definition.ToSql();
    }

    [Fact] void should_restore_the_identifier() => _restored.Identifier.ShouldEqual(_definition.Identifier);
    [Fact] void should_restore_the_container_name() => _restored.ContainerName.ShouldEqual(_definition.ContainerName);
    [Fact] void should_restore_the_generations() => _restored.Schemas.Keys.Order().ShouldEqual(_definition.Schemas.Keys.Order());
    [Fact] void should_restore_every_schema() => _definition.Schemas.All(_ => JsonNode.DeepEquals(JsonNode.Parse(_restored.Schemas[_.Key].ToJson()), JsonNode.Parse(_.Value.ToJson()))).ShouldBeTrue();
    [Fact] void should_restore_the_indexes() => _restored.Indexes.ShouldEqual(_definition.Indexes);
    [Fact] void should_keep_one_stored_schema_per_generation() => _stored.Schemas.Keys.Order().ShouldEqual([1u, 2u]);
    [Fact] void should_store_the_latest_schema_as_json() => JsonNode.Parse(_stored.Schemas[2])!["properties"]!.AsObject().Select(_ => _.Key).ShouldEqual(["id", "name"]);
}
