// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Kernel.Grains.Projections;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using ImmediateProjectionResult = Aksio.Cratis.Kernel.Grains.Projections.ImmediateProjectionResult;

namespace Aksio.Cratis.Kernel.Domain.Projections;

/// <summary>
/// Represents the API for projections.
/// </summary>
[Route("/api/events/store/{microserviceId}/projections")]
public class Projections : Controller
{
    readonly IGrainFactory _grainFactory;
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitionsProvider;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
    /// <param name="projectionDefinitionsProvider">Provider for <see cref="IProjectionDefinitions"/>.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for deserializing projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public Projections(
        IGrainFactory grainFactory,
        ProviderFor<IProjectionDefinitions> projectionDefinitionsProvider,
        IJsonProjectionSerializer projectionSerializer,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager)
    {
        _grainFactory = grainFactory;
        _projectionDefinitionsProvider = projectionDefinitionsProvider;
        _projectionSerializer = projectionSerializer;
        _jsonSerializerOptions = jsonSerializerOptions;
        _executionContextManager = executionContextManager;
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
        _executionContextManager.Establish(microserviceId);

        var projections = _grainFactory.GetGrain<IProjections>(microserviceId.Value);
        var projectionsAndPipelines = payload.Projections.Select(_ =>
            new ProjectionAndPipeline(
                _projectionSerializer.Deserialize(_.Projection),
                _.Pipeline.Deserialize<ProjectionPipelineDefinition>(_jsonSerializerOptions)!
            )).ToArray();

        await projections.Register(projectionsAndPipelines);
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

        var projectionDefinition = _projectionSerializer.Deserialize(immediateProjection.Projection);
        var projection = _grainFactory.GetGrain<IImmediateProjection>(immediateProjection.ProjectionId, key);
        return await projection.GetModelInstance(projectionDefinition);
    }

    /// <summary>
    /// Gets all projections.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> to get projections for.</param>
    /// <returns><see cref="Projection"/> containing all projections.</returns>
    [HttpGet]
    public async Task<IEnumerable<Projection>> AllProjections([FromRoute] MicroserviceId microserviceId)
    {
        _executionContextManager.Establish(microserviceId);

        var projections = await _projectionDefinitionsProvider().GetAll();
        return projections.Select(_ => new Projection(
            _.Identifier,
            _.Name,
            _.Model.Name)).ToArray();
    }

    /// <summary>
    /// Get all collections for projection.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> to get projection collections for.</param>
    /// <param name="projectionId">Id of projection to get for.</param>
    /// <returns>Collection of all the projection collections.</returns>
    [HttpGet("{projectionId}/collections")]
#pragma warning disable IDE0060
    public IEnumerable<ProjectionCollection> Collections(
        [FromQuery] MicroserviceId microserviceId,
        [FromRoute] ProjectionId projectionId)
    {
        return new ProjectionCollection[]
        {
                new("Something", 42)
        };
    }
}
