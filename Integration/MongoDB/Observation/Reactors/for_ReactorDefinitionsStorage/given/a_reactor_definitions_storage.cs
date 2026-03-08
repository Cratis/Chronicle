// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Observation.Reactors;
using MongoDB.Driver;
using NSubstitute;
using ReactorDefinition = Cratis.Chronicle.Concepts.Observation.Reactors.ReactorDefinition;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.Reactors.for_ReactorDefinitionsStorage.given;

public class a_reactor_definitions_storage(ChronicleInProcessFixture fixture) : Integration.given.a_mongo_client(fixture)
{
    protected ReactorDefinitionsStorage _storage = default!;
    protected IEventStoreDatabase _database = default!;
    protected IMongoDatabase _mongoDatabase = default!;
    protected string _databaseName = default!;

    async Task Establish()
    {
        _databaseName = $"chronicle_reactor_defs_specs_{Guid.NewGuid():N}";
        _mongoDatabase = _client.GetDatabase(_databaseName);

        _database = Substitute.For<IEventStoreDatabase>();
        _database
            .GetCollection<Storage.MongoDB.Observation.Reactors.ReactorDefinition>(WellKnownCollectionNames.ReactorDefinitions)
            .Returns(_mongoDatabase.GetCollection<Storage.MongoDB.Observation.Reactors.ReactorDefinition>(WellKnownCollectionNames.ReactorDefinitions));

        _storage = new ReactorDefinitionsStorage(_database);
        await Task.CompletedTask;
    }

    async Task Cleanup() => await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);

    /// <summary>
    /// Creates a reactor definition with the given id.
    /// </summary>
    /// <param name="id">String value for the <see cref="ReactorId"/>.</param>
    /// <returns>A new <see cref="ReactorDefinition"/>.</returns>
    protected static ReactorDefinition CreateReactorDefinition(string id) =>
        new(
            new ReactorId(id),
            ReactorOwner.Kernel,
            EventSequenceId.Log,
            []);
}
