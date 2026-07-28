// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Projections;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_read_model_definitions;

[Collection(MongoDBCollection.Name)]
public class when_round_tripping_indexes(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    IReadOnlyList<string> _indexes;

    async Task Because()
    {
        var eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        eventStoreDatabase.GetCollection<ReadModel>(WellKnownCollectionNames.ReadModelDefinitions)
            .Returns(_rawDatabase.GetCollection<ReadModel>(WellKnownCollectionNames.ReadModelDefinitions));
        var storage = new ReadModelDefinitionsStorage(eventStoreDatabase);

        var definition = new ReadModelDefinition(
            "test-read-model",
            "TestReadModel",
            "Test Read Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            [new IndexDefinition("Name"), new IndexDefinition("Address.City")]);

        await storage.Save(definition);

        var loaded = (await storage.GetAll()).Single();
        _indexes = loaded.Indexes.Select(index => index.PropertyPath.Path).ToList();
    }

    [Fact] void should_preserve_both_index_paths() => _indexes.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_index_property_paths() => _indexes.ShouldContainOnly(["Name", "Address.City"]);
}
