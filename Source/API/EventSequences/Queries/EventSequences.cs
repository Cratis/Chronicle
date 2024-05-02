// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.EventSequences.Queries;

/// <summary>
/// Represents the API for working with event logs.
/// </summary>
[Route("/api/events/store/sequences")]
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
