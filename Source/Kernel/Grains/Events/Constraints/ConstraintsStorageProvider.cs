// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents the storage provider for constraints.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to use.</param>
public class ConstraintsStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => throw new NotImplementedException();
}
