// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Queries;
using IReadModelsService = Cratis.Chronicle.Contracts.ReadModels.IReadModels;

namespace Cratis.Chronicle.Api.ReadModels;

/// <summary>
/// Represents a read model instance.
/// </summary>
/// <param name="Instance">The instance identifier as JSON.</param>
[ReadModel]
public record ReadModelInstance(JsonObject Instance)
{
    /// <summary>
    /// Gets instances of a read model.
    /// </summary>
    /// <param name="readModels"><see cref="IReadModelsService"/> for working with read models.</param>
    /// <param name="queryContextManager">The <see cref="IQueryContextManager"/>.</param>
    /// <param name="eventStore">The event store to get instances for.</param>
    /// <param name="namespace">The namespace of the read model to get instances for.</param>
    /// <param name="readModel">The name of the read model to get instances for.</param>
    /// <param name="occurrence">Optional occurrence name to get instances from.</param>
    /// <returns>Paged collection of read model instances.</returns>
    internal static async Task<IEnumerable<ReadModelInstance>> ReadModelInstances(
        IReadModelsService readModels,
        IQueryContextManager queryContextManager,
        string eventStore,
        string @namespace,
        string readModel,
        string? occurrence = null)
    {
        var queryContext = queryContextManager.Current;

        var response = await readModels.GetInstances(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ReadModel = readModel,
            Occurrence = occurrence,
            Page = queryContext.Paging.Page,
            PageSize = queryContext.Paging.Size
        });

        return response.Instances.Select(i => new ReadModelInstance(JsonNode.Parse(i)!.AsObject()));
    }
}
