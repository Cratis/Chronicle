// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.EventSequences;

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
            new EventSequenceInformation(EventSequenceId.Log.ToString(), "Log"),
            new EventSequenceInformation(EventSequenceId.Inbox.ToString(), "Inbox"),
            new EventSequenceInformation(EventSequenceId.Outbox.ToString(), "Outbox")
        }.AsEnumerable());
    }
}
