// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Events.Store.Api;

/// <summary>
/// Represents the API for working with event logs.
/// </summary>
[Route("/api/events/store/sequences")]
public class EventSequences : Controller
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
