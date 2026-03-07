// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling patch manager state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class PatchManagerStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainState is IGrainState<PatchManagerState> actualGrainState)
        {
            var appliedPatches = await storage.System.Patches.GetAll();
            actualGrainState.State = new PatchManagerState(appliedPatches);
        }
    }

    /// <inheritdoc/>
    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;
}
