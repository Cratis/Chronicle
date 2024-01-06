// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Projections;

/// <summary>
/// Represents the API for projections.
/// </summary>
[Route("/api/events/store/{microserviceId}/projections")]
public class Projections : ControllerBase
{
    readonly IClusterStorage _clusterStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing underlying storage.</param>
    public Projections(IClusterStorage clusterStorage)
    {
        _clusterStorage = clusterStorage;
    }

    /// <summary>
    /// Gets all projections.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> to get projections for.</param>
    /// <returns><see cref="Projection"/> containing all projections.</returns>
    [HttpGet]
    public async Task<IEnumerable<Projection>> AllProjections([FromRoute] MicroserviceId microserviceId)
    {
        var projections = await _clusterStorage.GetEventStore((string)microserviceId).Projections.GetAll();
        return projections.Select(_ => new Projection(
            _.Identifier,
            _.Name,
            _.IsActive,
            _.Model.Name)).ToArray();
    }
}
