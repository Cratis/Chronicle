// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.ReadModelExplorer;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.given;

/// <summary>
/// Builds the snapshot payload the generated ReadModelExplorer query answers with, so the specs that assert on
/// what the client makes of it do not each restate the contract shape.
/// </summary>
public static class a_snapshot_response
{
    /// <summary>
    /// Builds a snapshot carrying one event.
    /// </summary>
    /// <param name="instance">The JSON the read model stood at.</param>
    /// <param name="eventType">The type of the event behind the snapshot.</param>
    /// <param name="content">The event's content.</param>
    /// <param name="jsonSerializerOptions">The options the content is serialized with.</param>
    /// <returns>The snapshot.</returns>
    public static ReadModelSnapshotResponse With(
        string instance,
        EventType eventType,
        object content,
        JsonSerializerOptions jsonSerializerOptions) =>
        new()
        {
            Instance = instance,
            Occurred = DateTimeOffset.UtcNow,
            CorrelationId = Guid.NewGuid(),
            Events =
            [
                new Contracts.ReadModelExplorer.Event
                {
                    Context = new Contracts.Sequences.EventContext
                    {
                        EventType = new Contracts.Sequences.EventType { Id = eventType.Id, Generation = eventType.Generation },
                        EventSourceType = string.Empty,
                        EventSourceId = Guid.NewGuid().ToString(),
                        SequenceNumber = 42,
                        EventStreamType = string.Empty,
                        EventStreamId = string.Empty,
                        Occurred = DateTimeOffset.UtcNow,
                        CorrelationId = Guid.NewGuid(),
                        CausedBy = new Contracts.Sequences.Identity { Subject = string.Empty, Name = string.Empty, UserName = string.Empty },
                        Hash = string.Empty
                    },
                    Content = JsonSerializer.Serialize(content, jsonSerializerOptions)
                }
            ]
        };

    /// <summary>
    /// Wraps snapshots in the query result the service answers with.
    /// </summary>
    /// <param name="snapshots">The snapshots to answer with.</param>
    /// <returns>The query result.</returns>
    public static QueryResult<IEnumerable<ReadModelSnapshotResponse>> AsResult(params ReadModelSnapshotResponse[] snapshots) =>
        new() { Data = snapshots };
}
