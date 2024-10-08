// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Api.EventSequences;

/// <summary>
/// Represents the API for working with event logs.
/// </summary>
[Route("/api/event-store/sequences")]
public class EventSequences : ControllerBase
{
    /// <summary>
    /// Gets all event sequences.
    /// </summary>
    /// <returns>Collection of <see cref="EventSequenceInformation"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<EventSequenceInformation>> AllEventSequences()
    {
        return Task.FromResult(new[]
        {
            new EventSequenceInformation(EventSequenceId.Log.ToString(), "Log")
        }.AsEnumerable());
    }
}
