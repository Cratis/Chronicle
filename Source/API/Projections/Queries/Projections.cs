// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Projections.Queries;

/// <summary>
/// Represents the API for projections.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Projections"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
[Route("/api/events/store/{eventStore}/projections")]
public class Projections(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Gets all projections for an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get projections for.</param>
    /// <returns><see cref="Projection"/> containing all projections.</returns>
    [HttpGet]
    public async Task<IEnumerable<Projection>> AllProjections([FromRoute] EventStoreName eventStore)
    {
        var projections = await storage.GetEventStore(eventStore).Projections.GetAll();
        return projections.Select(_ => new Projection(
            _.Identifier,
            _.Name,
            _.IsActive,
            _.Model.Name)).ToArray();
    }
}
