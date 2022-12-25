// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Events.Store.MongoDB.Tenants;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling tenant configuration data.
/// </summary>
public class TenantConfigurationStorageProvider : IGrainStorage
{
    readonly IClusterDatabase _database;
    IMongoCollection<MongoDBTenantConfigurationState> Collection => _database.GetCollection<MongoDBTenantConfigurationState>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfigurationStorageProvider"/> class.
    /// </summary>
    /// <param name="database"><see cref="IClusterDatabase"/> that holds the configuration data.</param>
    public TenantConfigurationStorageProvider(IClusterDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var tenantId = (TenantId)grainReference.GetPrimaryKey();
        var cursor = await Collection.FindAsync(_ => _.Id == tenantId);
        var state = await cursor.FirstOrDefaultAsync();
        if (state is not null)
        {
            grainState.State = new TenantConfigurationState(state.Configuration.ToDictionary(_ => _.Key, _ => _.Value));
        }
        else
        {
            grainState.State = TenantConfigurationState.Empty();
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var tenantId = (TenantId)grainReference.GetPrimaryKey();
        var state = (grainState.State as TenantConfigurationState)!;
        var mongoDBState = new MongoDBTenantConfigurationState(tenantId, state.Select(_ => new MongoDBTenantConfigurationKeyValuePair(_.Key, _.Value)));
        await Collection.ReplaceOneAsync(
            _ => _.Id == tenantId,
            mongoDBState,
            new ReplaceOptions { IsUpsert = true });
    }
}
