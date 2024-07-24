// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Projections;
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
        var projectionsManager = grainFactory.GetGrain<Grains.Projections.IProjectionsManager>(request.EventStoreName);
        var projections = request.Projections.Select(_ => _.ToChronicle()).ToArray();

        _ = Task.Run(() => projectionsManager.Register(projections));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceById(GetInstanceByIdRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStoreName,
            request.Namespace,
            request.EventSequenceId,
            request.ModelKey);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSession(GetInstanceByIdForSessionRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStoreName,
            request.Namespace,
            request.EventSequenceId,
            request.ModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdFOrSessionWithEventsApplied(GetInstanceByIdForSessionWithEventsAppliedRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStoreName,
            request.Namespace,
            request.EventSequenceId,
            request.ModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var eventsToApply = request.Events.Select(_ => _.ToChronicle()).ToArray();
        var result = await projection.GetCurrentModelInstanceWithAdditionalEventsApplied(eventsToApply);
        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStoreName,
            request.Namespace,
            request.EventSequenceId,
            request.ModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);
        await projection.Dehydrate();
    }
}
