// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the API for working with event logs.
/// </summary>
/// <param name="eventSequences"><see cref="IEventSequences"/> for working with event sequences.</param>
[Route("/api/event-store/{eventStore}/sequences")]
public class EventSequences(IEventSequences eventSequences) : ControllerBase
{
    /// <summary>
    /// The namespace to read from when the caller does not name one.
    /// </summary>
    const string DefaultNamespace = "Default";

    /// <summary>
    /// Gets all event sequences.
    /// </summary>
    /// <param name="eventStore">The event store to get event sequences for.</param>
    /// <param name="namespace">The namespace to get event sequences for. Defaults to the default namespace.</param>
    /// <returns>Collection of names of event sequences.</returns>
    [HttpGet]
    public async Task<IEnumerable<string>> AllEventSequences(
        [FromRoute] string eventStore,
        [FromQuery] string @namespace = DefaultNamespace)
    {
        var response = await eventSequences.GetEventSequences(new()
        {
            EventStore = eventStore,
            Namespace = @namespace
        });

        return response.EventSequenceIds;
    }
}
