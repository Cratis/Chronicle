// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Kernel.Grains.Projections;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Microsoft.AspNetCore.Mvc;
using ImmediateProjectionResult = Aksio.Cratis.Kernel.Grains.Projections.ImmediateProjectionResult;
using IProjections = Aksio.Cratis.Kernel.Grains.Projections.IProjections;

namespace Aksio.Cratis.Kernel.Domain.Projections;

/// <summary>
/// Represents the API for projections.
/// </summary>
[Route("/api/events/store/{microserviceId}/projections")]
public class Projections : ControllerBase
{
    readonly IGrainFactory _grainFactory;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for deserializing projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public Projections(
        IGrainFactory grainFactory,
        IJsonProjectionSerializer projectionSerializer,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _grainFactory = grainFactory;
        _projectionSerializer = projectionSerializer;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>
    /// Register projections with pipelines.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to register for.</param>
    /// <param name="payload">The registrations.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task RegisterProjections(
        [FromRoute] MicroserviceId microserviceId,
        [FromBody] RegisterProjections payload)
    {
        var projections = _grainFactory.GetGrain<IProjections>(0);
        var projectionsAndPipelines = payload.Projections.Select(_ =>
            new ProjectionAndPipeline(
                _projectionSerializer.Deserialize(_.Projection),
                _.Pipeline.Deserialize<ProjectionPipelineDefinition>(_jsonSerializerOptions)!)).ToArray();

        await projections.Register(microserviceId, projectionsAndPipelines);
    }

    /// <summary>
    /// Perform an immediate projection.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to perform for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to perform for.</param>
    /// <param name="immediateProjection">The details about the <see cref="ImmediateProjection"/>.</param>
    /// <returns><see cref="ImmediateProjectionResult"/>.</returns>
    [HttpPost("immediate/{tenantId}")]
    public async Task<ImmediateProjectionResult> Immediate(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromBody] ImmediateProjection immediateProjection)
    {
        var key = new ImmediateProjectionKey(
            microserviceId,
            tenantId,
            immediateProjection.EventSequenceId,
            immediateProjection.ModelKey);

        var projection = _grainFactory.GetGrain<IImmediateProjection>(immediateProjection.ProjectionId, key);
        return await projection.GetModelInstance();
    }

    /// <summary>
    /// Perform an immediate projection for a specific session based on the current correlation id.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to perform for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to perform for.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> identifying the session.</param>
    /// <param name="immediateProjection">The details about the <see cref="ImmediateProjection"/>.</param>
    /// <returns><see cref="ImmediateProjectionResult"/>.</returns>
    [HttpPost("immediate/{tenantId}/session/{correlationId}")]
    public async Task<ImmediateProjectionResult> ImmediateForSession(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] CorrelationId correlationId,
        [FromBody] ImmediateProjection immediateProjection)
    {
        var key = new ImmediateProjectionKey(
            microserviceId,
            tenantId,
            immediateProjection.EventSequenceId,
            immediateProjection.ModelKey,
            correlationId);

        var projection = _grainFactory.GetGrain<IImmediateProjection>(immediateProjection.ProjectionId, key);
        return await projection.GetModelInstance();
    }

    /// <summary>
    /// Apply additional events to an immediate projection in session, assuming it already exists with state.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to perform for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to perform for.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> identifying the session.</param>
    /// <param name="immediateProjection">The details about the <see cref="ImmediateProjectionWithEventsToApply"/>.</param>
    /// <returns><see cref="ImmediateProjectionResult"/>.</returns>
    [HttpPost("immediate/{tenantId}/session/{correlationId}/with-events")]
    public async Task<ImmediateProjectionResult> ImmediateForSessionWithEvents(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] CorrelationId correlationId,
        [FromBody] ImmediateProjectionWithEventsToApply immediateProjection)
    {
        var key = new ImmediateProjectionKey(
            microserviceId,
            tenantId,
            immediateProjection.EventSequenceId,
            immediateProjection.ModelKey,
            correlationId);

        var projection = _grainFactory.GetGrain<IImmediateProjection>(immediateProjection.ProjectionId, key);
        return await projection.GetCurrentModelInstanceWithAdditionalEventsApplied(immediateProjection.Events);
    }

    /// <summary>
    /// Apply additional events to an immediate projection in session, assuming it already exists with state.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to perform for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to perform for.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> identifying the session.</param>
    /// <param name="immediateProjection">The details about the <see cref="ImmediateProjectionWithEventsToApply"/>.</param>
    /// <returns><see cref="ImmediateProjectionResult"/>.</returns>
    [HttpPost("immediate/{tenantId}/session/{correlationId}/dehydrate")]
    public async Task Dehydrate(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] CorrelationId correlationId,
        [FromBody] ImmediateProjection immediateProjection)
    {
        var key = new ImmediateProjectionKey(
            microserviceId,
            tenantId,
            immediateProjection.EventSequenceId,
            immediateProjection.ModelKey,
            correlationId);

        var projection = _grainFactory.GetGrain<IImmediateProjection>(immediateProjection.ProjectionId, key);
        await projection.Dehydrate();
    }
}
