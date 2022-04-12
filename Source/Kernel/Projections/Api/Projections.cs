// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Events.Projections.Api;

/// <summary>
/// Represents the API for projections.
/// </summary>
[Route("/api/events/projections")]
public class Projections : Controller
{
    /// <summary>
    /// Gets all projections.
    /// </summary>
    /// <returns><see cref="ClientObservable{T}"/> containing all projections.</returns>
    [HttpGet]
    public ClientObservable<IEnumerable<Projection>> AllProjections()
    {
        return new ClientObservable<IEnumerable<Projection>>
        {
            ClientDisconnected = () =>
            {
            }
        };
    }

    /// <summary>
    /// Rewind a specific projection.
    /// </summary>
    /// <param name="projectionId">Id of projection to rewind.</param>
    [HttpPost("{projectionId}/rewind")]
    public void Rewind([FromRoute] Guid projectionId)
    {
        Console.WriteLine(projectionId);
    }

    /// <summary>
    /// Get all collections for projection.
    /// </summary>
    /// <param name="projectionId">Id of projection to get for.</param>
    /// <returns>Collection of all the projection collections.</returns>
    [HttpGet("{projectionId}/collections")]
#pragma warning disable IDE0060
    public IEnumerable<ProjectionCollection> Collections([FromRoute] Guid projectionId)
    {
        return new ProjectionCollection[]
        {
                new("Something", 42)
        };
    }
}
