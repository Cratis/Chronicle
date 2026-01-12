// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Api.ReadModels;

/// <summary>
/// Represents a snapshot of a read model.
/// </summary>
/// <param name="Occurred">When the snapshot was taken.</param>
/// <param name="Instance">The instance of the read model.</param>
/// <param name="Events">The events that led to the snapshot.</param>
[ReadModel]
public record ReadModelSnapshot(DateTimeOffset Occurred, JsonObject Instance, IEnumerable<Event> Events)
{
    /// <summary>
    /// Gets all snapshots for a specific read model.
    /// </summary>
    /// <param name="readModels">The read models service.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="readModel">The read model identifier.</param>
    /// <param name="readModelKey">The read model key.</param>
    /// <returns>Collection of snapshots.</returns>
    internal static async Task<IEnumerable<ReadModelSnapshot>> AllSnapshotsForReadModel(
        IReadModels readModels,
        string eventStore,
        string @namespace,
        string readModel,
        string readModelKey)
    {
        var response = await readModels.GetSnapshotsByKey(new GetSnapshotsByKeyRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ReadModelIdentifier = readModel,
            ReadModelKey = readModelKey
        });

        return response.Snapshots.Select(s => new ReadModelSnapshot(s.Occurred, JsonNode.Parse(s.ReadModel)!.AsObject(), s.Events.Select(e => new Event(
            e.Context.SequenceNumber,
            e.Context.EventType.Id,
            e.Context.Occurred,
            JsonNode.Parse(e.Content)!.AsObject()))));
    }
}
