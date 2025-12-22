// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Api.ReadModels;

/// <summary>
/// Represents the API for working with read model queries.
/// </summary>
[Route("/api/event-store/{eventStore}/read-models")]
public class ReadModelQueries : ControllerBase
{
    readonly IReadModels _readModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelQueries"/> class.
    /// </summary>
    /// <param name="readModels"><see cref="IReadModels"/> for working with read models.</param>
    internal ReadModelQueries(IReadModels readModels)
    {
        _readModels = readModels;
    }

    /// <summary>
    /// Get all read model definitions.
    /// </summary>
    /// <param name="eventStore">The event store to get read models for.</param>
    /// <returns>Collection of read model definitions.</returns>
    [HttpGet]
    public async Task<IEnumerable<ReadModelDefinition>> AllReadModels([FromRoute] string eventStore)
    {
        var response = await _readModels.GetDefinitions(new()
        {
            EventStore = eventStore
        });

        return response.ReadModels.Select(rm => new ReadModelDefinition(
            rm.Identifier,
            rm.Name,
            rm.Generation,
            rm.Schema,
            rm.Indexes.Select(i => i.PropertyPath)));
    }
}
