// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Grains.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
internal sealed class ReadModels(IGrainFactory grainFactory) : IReadModels
{
    /// <inheritdoc/>
    public async Task Register(RegisterRequest request, CallContext context = default)
    {
        var readModelsManager = grainFactory.GetReadModelsManager(request.EventStore);
        var readModelDefinitions = request.ReadModels.Select(definition => definition.ToChronicle(request.Owner)).ToArray();
        await readModelsManager.Register(readModelDefinitions);
    }
}
