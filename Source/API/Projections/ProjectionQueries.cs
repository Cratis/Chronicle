// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Projections;

/// <summary>
/// Represents the API for projections.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionQueries"/> class.
/// </remarks>
[Route("/api/events/store/{eventStore}/projections")]
public class ProjectionQueries() : ControllerBase
{
    /// <summary>
    /// Gets all projections for an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get projections for.</param>
    /// <returns><see cref="Projection"/> containing all projections.</returns>
    [HttpGet]
    public async Task<IEnumerable<Projection>> AllProjections([FromRoute] EventStoreName eventStore)
    {
        throw new NotImplementedException();
    }
}
