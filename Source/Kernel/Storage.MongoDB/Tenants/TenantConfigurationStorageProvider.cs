// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.MongoDB;
using MongoDB.Driver;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Tenants;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling tenant configuration data.
/// </summary>
public class TenantConfigurationStorageProvider : IGrainStorage
{
    readonly IDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfigurationStorageProvider"/> class.
    /// </summary>
    /// <param name="database"><see cref="IDatabase"/> that holds the configuration data.</param>
    public TenantConfigurationStorageProvider(IDatabase database)
    {
        _database = database;
    }

    IMongoCollection<TenantConfigurationState> Collection => _database.GetCollection<TenantConfigurationState>(WellKnownCollectionNames.TenantConfiguration);

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<Storage.Configuration.Tenants.TenantConfigurationState>)!;
        var tenantId = (TenantId)grainId.GetGuidKey();
        var cursor = await Collection.FindAsync(_ => _.Id == tenantId).ConfigureAwait(false);
        var state = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        if (state is not null)
        {
            actualGrainState.State = new Storage.Configuration.Tenants.TenantConfigurationState(state.Configuration.ToDictionary(_ => _.Key, _ => _.Value));
        }
        else
        {
            actualGrainState.State = Storage.Configuration.Tenants.TenantConfigurationState.Empty();
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var tenantId = (TenantId)grainId.GetGuidKey();
        var state = (grainState.State as Storage.Configuration.Tenants.TenantConfigurationState)!;
        var mongoDBState = new TenantConfigurationState(tenantId, state.Select(_ => new TenantConfigurationKeyValuePair(_.Key, _.Value)));
        await Collection.ReplaceOneAsync(
            _ => _.Id == tenantId,
            mongoDBState,
            new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
