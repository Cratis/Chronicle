// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Projections;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Projections;

/// <summary>
/// Represents the API for projections.
/// </summary>
[Route("/api/events/store/{microserviceId}/projections")]
public class Projections : Controller
{
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitionsProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="projectionDefinitionsProvider">Provider for <see cref="IProjectionDefinitions"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public Projections(
        ProviderFor<IProjectionDefinitions> projectionDefinitionsProvider,
        IExecutionContextManager executionContextManager)
    {
        _projectionDefinitionsProvider = projectionDefinitionsProvider;
        _executionContextManager = executionContextManager;
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
            _.IsActive,
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
