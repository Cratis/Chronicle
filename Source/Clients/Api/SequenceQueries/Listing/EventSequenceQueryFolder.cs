// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Api.SequenceQueries.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences;
using IEventSequencesService = Cratis.Chronicle.Contracts.EventSequences.IEventSequences;

namespace Cratis.Chronicle.Api.SequenceQueries.Listing;

/// <summary>
/// Represents a folder of saved event sequence queries. The folder's <see cref="Owner"/> is
/// <c>"System"</c> for shared folders and the user identifier of the caller for per-user folders.
/// </summary>
/// <param name="FolderId">The unique identifier of the folder.</param>
/// <param name="Name">The display name of the folder.</param>
/// <param name="Owner">The owner identifier; <c>"System"</c> for shared folders or a user identifier for per-user folders.</param>
/// <param name="Queries">The queries contained in this folder.</param>
[ReadModel]
public record EventSequenceQueryFolder(
    EventSequenceQueryFolderId FolderId,
    string Name,
    string Owner,
    IEnumerable<EventSequenceQuery> Queries)
{
    /// <summary>
    /// The literal owner value used for shared folders.
    /// </summary>
    public const string SystemOwner = "System";

    static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Gets all event sequence query folders by reducing the folder and query events directly
    /// from the event log. This is the C# equivalent of the PDL projection documented on
    /// <see cref="EventSequenceQueryFolderProjection"/>; it lives here for now because the
    /// workbench's internal read models are not auto-registered against user event stores.
    /// </summary>
    /// <param name="eventStore">The event store context.</param>
    /// <param name="namespace">The namespace context.</param>
    /// <param name="eventSequences">The <see cref="IEventSequencesService"/> gRPC service.</param>
    /// <returns>The folders, each with their nested queries.</returns>
    public static async Task<IEnumerable<EventSequenceQueryFolder>> AllEventSequenceQueryFolders(
        string eventStore,
        string @namespace,
        IEventSequencesService eventSequences)
    {
        var tail = await eventSequences.GetTailSequenceNumber(new GetTailSequenceNumberRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = "event-log"
        });

        if (tail.SequenceNumber == 0)
        {
            return [];
        }

        var response = await eventSequences.GetEventsFromEventSequenceNumber(new GetFromEventSequenceNumberRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = "event-log",
            FromEventSequenceNumber = 0,
            ToEventSequenceNumber = tail.SequenceNumber
        });

        return Reduce(response.Events.Where(@event =>
            @event.Context.EventType.Id == nameof(EventSequenceQueryFolderAdded) ||
            @event.Context.EventType.Id == nameof(EventSequenceQueryFolderForUserAdded) ||
            @event.Context.EventType.Id == nameof(EventSequenceQueryAdded)));
    }

    static List<EventSequenceQueryFolder> Reduce(IEnumerable<AppendedEvent> events)
    {
        var folders = new Dictionary<string, EventSequenceQueryFolder>(StringComparer.Ordinal);
        var queriesByFolder = new Dictionary<string, List<EventSequenceQuery>>(StringComparer.Ordinal);

        foreach (var @event in events)
        {
            var eventSourceId = @event.Context.EventSourceId;
            switch (@event.Context.EventType.Id)
            {
                case nameof(EventSequenceQueryFolderAdded):
                    {
                        var payload = JsonSerializer.Deserialize<EventSequenceQueryFolderAdded>(@event.Content, _jsonOptions);
                        if (payload is not null)
                        {
                            folders[eventSourceId] = new EventSequenceQueryFolder(
                                Guid.Parse(eventSourceId),
                                payload.Name,
                                SystemOwner,
                                []);
                        }
                        break;
                    }

                case nameof(EventSequenceQueryFolderForUserAdded):
                    {
                        var payload = JsonSerializer.Deserialize<EventSequenceQueryFolderForUserAdded>(@event.Content, _jsonOptions);
                        if (payload is not null)
                        {
                            var owner = @event.Context.CausedBy.Subject;
                            folders[eventSourceId] = new EventSequenceQueryFolder(
                                Guid.Parse(eventSourceId),
                                payload.Name,
                                string.IsNullOrEmpty(owner) ? SystemOwner : owner,
                                []);
                        }
                        break;
                    }

                case nameof(EventSequenceQueryAdded):
                    {
                        var payload = JsonSerializer.Deserialize<EventSequenceQueryAdded>(@event.Content, _jsonOptions);
                        if (payload is not null)
                        {
                            var folderKey = payload.FolderId.Value.ToString();
                            if (!queriesByFolder.TryGetValue(folderKey, out var list))
                            {
                                list = [];
                                queriesByFolder[folderKey] = list;
                            }

                            list.Add(new EventSequenceQuery(
                                Guid.Parse(eventSourceId),
                                payload.Name,
                                payload.EventSequenceId,
                                payload.Filter));
                        }
                        break;
                    }
            }
        }

        return folders.Values.Select(folder =>
            queriesByFolder.TryGetValue(folder.FolderId.Value.ToString(), out var nested)
                ? folder with { Queries = nested }
                : folder).ToList();
    }
}
