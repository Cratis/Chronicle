// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Api;

/// <summary>
/// Represents the API for projections.
/// </summary>
[Route("/api/events/store/{microserviceId}/projections")]
public class Projections : Controller
{
    readonly IGrainFactory _grainFactory;
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitionsProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="grainFactory">Orleans <see cref="IGrainFactory"/>.</param>
    /// <param name="projectionDefinitionsProvider">Provider for <see cref="IProjectionDefinitions"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public Projections(
        IGrainFactory grainFactory,
        ProviderFor<IProjectionDefinitions> projectionDefinitionsProvider,
        IExecutionContextManager executionContextManager)
    {
        _grainFactory = grainFactory;
        _projectionDefinitionsProvider = projectionDefinitionsProvider;
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
        foreach (var registration in payload.Projections)
        {
            await projections.Register(registration.Projection, registration.Pipeline);
        }
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
