// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
internal sealed class ReadModels(IGrainFactory grainFactory) : IReadModels
{
    /// <inheritdoc/>
    public Task Register(RegisterRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetGrain<Grains.ReadModels.IReadModelsManager>(request.EventStore);
        var readModelDefinitions = request.ReadModels.Select(definition => definition.ToChronicle()).ToArray();
        _ = Task.Run(() => readModelsManager.Register(readModelDefinitions));
        return Task.CompletedTask;
    }
}
