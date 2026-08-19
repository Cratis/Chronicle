// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the read model for the event sequences an event store holds.
/// </summary>
/// <param name="Name">The name of the event sequence.</param>
[ReadModel]
public record EventSequenceNames(string Name)
{
    const string DefaultNamespace = "Default";

    /// <summary>
    /// Gets the names of every event sequence in an event store.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequences"/> to read from.</param>
    /// <param name="eventStore">The event store to get sequences for.</param>
    /// <param name="namespace">The namespace within the event store.</param>
    /// <returns>The names of the event sequences.</returns>
    internal static async Task<IEnumerable<string>> AllEventSequences(
        IEventSequences eventSequences,
        string eventStore,
        string @namespace = DefaultNamespace)
    {
        var response = await eventSequences.GetEventSequences(new()
        {
            EventStore = eventStore,
            Namespace = @namespace
        });

        return response.EventSequenceIds;
    }
}
