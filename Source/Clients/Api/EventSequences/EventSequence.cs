// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents an event sequence with its identifier and display name.
/// </summary>
/// <param name="Id">The unique identifier of the event sequence.</param>
/// <param name="Name">The human-readable display name.</param>
[ReadModel]
public record EventSequence(string Id, string Name)
{
    /// <summary>
    /// Gets all event sequences for a given event store and namespace.
    /// </summary>
    /// <param name="eventSequences"><see cref="IEventSequences"/> for querying sequences.</param>
    /// <param name="eventStore">The event store to get sequences for.</param>
    /// <param name="namespace">The namespace to get sequences for.</param>
    /// <returns>Collection of <see cref="EventSequence"/>.</returns>
    public static async Task<IEnumerable<EventSequence>> AllEventSequences(
        IEventSequences eventSequences,
        string eventStore,
        string @namespace)
    {
        var response = await eventSequences.GetAllEventSequences(new GetAllEventSequencesRequest
        {
            EventStore = eventStore,
            Namespace = @namespace
        });

        return response.EventSequences.Select(s => new EventSequence(s.Id, s.Name));
    }
}
