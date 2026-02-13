// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Command to add seed data.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace (or empty for global).</param>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content of the event.</param>
/// <param name="IsGlobal">Whether this seed data is global (applies to all namespaces).</param>
[Command]
public record AddSeedEntry(
    string EventStore,
    string Namespace,
    string EventSourceId,
    string EventTypeId,
    string Content,
    bool IsGlobal)
{
    /// <summary>
    /// Handles the command by invoking the <see cref="IEventSeeding"/> contract.
    /// </summary>
    /// <param name="eventSeeding">The event seeding contract.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(IEventSeeding eventSeeding)
    {
        var entry = new SeedingEntry
        {
            EventSourceId = EventSourceId,
            EventTypeId = EventTypeId,
            Content = Content,
            IsGlobal = IsGlobal,
            TargetNamespace = IsGlobal ? string.Empty : Namespace
        };

        await eventSeeding.Seed(new SeedRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            Entries = [entry]
        });
    }
}
