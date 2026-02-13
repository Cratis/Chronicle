// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling Data Protection keys state.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
public class DataProtectionKeysStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<DataProtectionKeysState>)!;
        var keys = await storage.System.DataProtectionKeys.GetAll();

        actualGrainState.State = new DataProtectionKeysState
        {
            Keys = keys.ToList()
        };
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<DataProtectionKeysState>)!;

        foreach (var key in actualGrainState.State.NewKeys)
        {
            await storage.System.DataProtectionKeys.Store(key);
            actualGrainState.State.Keys.Add(key);
        }

        actualGrainState.State.NewKeys.Clear();
    }
}
