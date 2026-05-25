// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.EventSequences;
using IEventSequencesService = Cratis.Chronicle.Contracts.EventSequences.IEventSequences;

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the API for querying sequence queries stored in the workbench.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/sequence-queries")]
public class SequenceQueryQueries : ControllerBase
{
    static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    readonly IEventSequencesService _eventSequences;

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceQueryQueries"/> class.
    /// </summary>
    /// <param name="eventSequences"><see cref="IEventSequencesService"/> gRPC service for reading events.</param>
    internal SequenceQueryQueries(IEventSequencesService eventSequences)
    {
        _eventSequences = eventSequences;
    }

    /// <summary>
    /// Gets all sequence queries for the given event store and namespace, reconstructed via server-side reduction.
    /// </summary>
    /// <param name="eventStore">The event store to get queries for.</param>
    /// <param name="namespace">The namespace to get queries for.</param>
    /// <returns>A collection of <see cref="SequenceQueryDefinition"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<SequenceQueryDefinition>> AllSequenceQueries(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace)
    {
        var response = await _eventSequences.GetEventsFromEventSequenceNumber(new GetFromEventSequenceNumberRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = "event-log",
            FromEventSequenceNumber = 0,
            EventTypes =
            [
                new Contracts.Events.EventType { Id = SequenceQueryEventTypeIds.Created, Generation = 1 },
                new Contracts.Events.EventType { Id = SequenceQueryEventTypeIds.Renamed, Generation = 1 },
                new Contracts.Events.EventType { Id = SequenceQueryEventTypeIds.FilterUpdated, Generation = 1 }
            ]
        });

        return Reduce(response.Events);
    }

    Dictionary<string, SequenceQueryDefinition>.ValueCollection Reduce(IEnumerable<Contracts.Events.AppendedEvent> events)
    {
        var queries = new Dictionary<string, SequenceQueryDefinition>();

        foreach (var @event in events)
        {
            var queryId = @event.Context.EventSourceId;

            switch (@event.Context.EventType.Id)
            {
                case SequenceQueryEventTypeIds.Created:
                    if (JsonSerializer.Deserialize<JsonElement>(@event.Content, _jsonOptions) is JsonElement created &&
                        created.TryGetProperty("name", out var nameEl))
                    {
                        queries[queryId] = new SequenceQueryDefinition(queryId, nameEl.GetString() ?? string.Empty, new SequenceQueryFilter());
                    }
                    break;

                case SequenceQueryEventTypeIds.Renamed:
                    if (queries.TryGetValue(queryId, out var toRename) &&
                        JsonSerializer.Deserialize<JsonElement>(@event.Content, _jsonOptions) is JsonElement renamed &&
                        renamed.TryGetProperty("name", out var newNameEl))
                    {
                        queries[queryId] = toRename with { Name = newNameEl.GetString() ?? string.Empty };
                    }
                    break;

                case SequenceQueryEventTypeIds.FilterUpdated:
                    if (queries.TryGetValue(queryId, out var toUpdate))
                    {
                        var filter = JsonSerializer.Deserialize<SequenceQueryFilter>(@event.Content, _jsonOptions);
                        queries[queryId] = toUpdate with { Filter = filter ?? new SequenceQueryFilter() };
                    }
                    break;
            }
        }

        return queries.Values;
    }
}
