// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts;

/// <summary>
/// Supplies real standalone MongoDB storage in an isolated database.
/// </summary>
public class MongoAlertIncidentsHarness : IAlertIncidentsStorageHarness
{
    MongoClient _client;
    string _databaseName;

    /// <summary>
    /// Gets or sets the shared MongoDB fixture.
    /// </summary>
    public MongoDBFixture Fixture { get; set; } = null!;

    /// <inheritdoc/>
    public Task<IAlertIncidentsStorage> Create()
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(Fixture.ConnectionString);
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetCollection<AlertIncidentDocument>(WellKnownCollectionNames.AlertIncidents)
            .Returns(_client.GetDatabase(_databaseName).GetCollection<AlertIncidentDocument>(WellKnownCollectionNames.AlertIncidents));

        return Task.FromResult<IAlertIncidentsStorage>(new AlertIncidentsStorage(database));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _client.DropDatabaseAsync(_databaseName);
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
