// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents the API for commands related to seed data.
/// </summary>
[Route("/api/event-stores/{eventStore}/seed-data")]
public class SeedDataCommands : ControllerBase
{
    readonly IEventSeeding _eventSeeding;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedDataCommands"/> class.
    /// </summary>
    /// <param name="eventSeeding">The event seeding contract.</param>
    internal SeedDataCommands(IEventSeeding eventSeeding)
    {
        _eventSeeding = eventSeeding;
    }

    /// <summary>
    /// Add seed data.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="command">The add seed data command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("add")]
    public async Task Add(string eventStore, [FromBody] AddSeedData command)
    {
        var entry = new SeedingEntry
        {
            EventSourceId = command.EventSourceId,
            EventTypeId = command.EventTypeId,
            Content = command.Content,
            IsGlobal = command.IsGlobal,
            TargetNamespace = command.IsGlobal ? string.Empty : command.Namespace
        };

        await _eventSeeding.Seed(new SeedRequest
        {
            EventStore = eventStore,
            Namespace = command.Namespace,
            Entries = [entry]
        });
    }
}
