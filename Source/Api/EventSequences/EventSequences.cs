// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <returns>Collection of names of event sequences.</returns>
    [HttpGet]
    public Task<IEnumerable<string>> AllEventSequences() => Task.FromResult<IEnumerable<string>>(["Log"]);
}
