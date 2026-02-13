// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the API for working with event logs.
/// </summary>
[Route("/api/event-store/{eventStore}/sequences")]
public class EventSequences : ControllerBase
{
    /// <summary>
    /// Gets all event sequences.
    /// </summary>
    /// <param name="eventStore">The event store to get event sequences for.</param>
    /// <returns>Collection of names of event sequences.</returns>
    [HttpGet]
#pragma warning disable IDE0060 // Remove unused parameter
    public Task<IEnumerable<string>> AllEventSequences([FromRoute] string eventStore) => Task.FromResult<IEnumerable<string>>(["event-log"]);
#pragma warning restore IDE0060 // Remove unused parameter
}
