// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents the API for projections.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionQueries"/> class.
/// </remarks>
[Route("/api/event-store/{eventStore}/projections")]
public class ProjectionQueries() : ControllerBase
{
    /// <summary>
    /// Gets all projections for an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get projections for.</param>
    /// <returns><see cref="Projection"/> containing all projections.</returns>
    /// <exception cref="NotImplementedException">Not implemented.</exception>
    [HttpGet]
    public async Task<IEnumerable<Projection>> AllProjections([FromRoute] string eventStore)
    {
        throw new NotImplementedException();
    }
}
