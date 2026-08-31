// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Api.ReadModels;

/// <summary>
/// Represents a read model as it stood immediately after one event was applied to it.
/// </summary>
/// <param name="Instance">The read model, as it stood after the event was applied.</param>
/// <param name="Event">The event that produced this state.</param>
[ReadModel]
public record ReadModelTimelineEntry(JsonObject Instance, Event Event)
{
    /// <summary>
    /// Gets the timeline of a read model instance - its state after each event that shaped it.
    /// </summary>
    /// <param name="readModels">The read models service.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="readModel">The read model identifier.</param>
    /// <param name="readModelKey">The read model key.</param>
    /// <returns>The timeline, one entry per event, oldest first.</returns>
    /// <remarks>
    /// A snapshot groups the events that happened together, which is what someone reading history
    /// wants. Scrubbing needs the other shape: one entry per event, so every step moves by exactly
    /// one thing that happened.
    /// </remarks>
    public static async Task<IEnumerable<ReadModelTimelineEntry>> TimelineForReadModel(
        IReadModels readModels,
        string eventStore,
        string @namespace,
        string readModel,
        string readModelKey)
    {
        var response = await readModels.GetTimelineByKey(new GetTimelineByKeyRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ReadModelIdentifier = readModel,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = readModelKey
        });

        return response.Entries
            .Where(entry => entry.Event is not null)
            .Select(entry => new ReadModelTimelineEntry(
                JsonNode.Parse(entry.ReadModel)!.AsObject(),
                new Event(
                    entry.Event!.Context.SequenceNumber,
                    entry.Event.Context.EventType.Id,
                    entry.Event.Context.Occurred,
                    JsonNode.Parse(entry.Event.Content)!.AsObject())));
    }
}
