// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Sys;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling system grain state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class SystemStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainState is IGrainState<SystemInformationState> actualGrainState)
        {
            var systemInfo = await storage.System.GetSystemInformation();
            actualGrainState.State.Version = systemInfo?.Version;
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainState is IGrainState<SystemInformationState> actualGrainState &&
            actualGrainState.State.Version is not null)
        {
            await storage.System.SetSystemInformation(new SystemInformation(actualGrainState.State.Version));
        }
    }
}
