// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Projections.Definitions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
public class Projections(IGrainFactory grainFactory) : IProjections
{
    /// <inheritdoc/>
    public Task Register(RegisterRequest request, CallContext context = default)
    {
        var projections = grainFactory.GetGrain<Grains.Projections.IProjections>(0);
        var projectionAndPipelines = request.ProjectionsAndPipelines.Select(_ => _.ToChronicle()).ToArray();

        _ = Task.Run(() => projections.Register(request.EventStoreName, projectionAndPipelines));
        return Task.CompletedTask;
    }
}
