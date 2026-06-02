// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.EventSequences.Concurrency;
using IEventSequencesService = Cratis.Chronicle.Contracts.EventSequences.IEventSequences;

namespace Cratis.Chronicle.Api.SequenceQueries.Renaming;

/// <summary>
/// Represents the command for renaming an existing sequence query.
/// </summary>
/// <param name="EventStore">The event store context for the query.</param>
/// <param name="Namespace">The namespace context for the query.</param>
/// <param name="QueryId">The unique identifier of the query to rename.</param>
/// <param name="Name">The new display name for the query.</param>
[Command]
public record RenameSequenceQuery(string EventStore, string Namespace, string QueryId, string Name)
{
    /// <summary>
    /// Handles the rename sequence query command by appending a rename event to Chronicle.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequencesService"/> gRPC service.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for tracking causation.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(
        IEventSequencesService eventSequences,
        ICausationManager causationManager)
    {
        var content = JsonSerializer.Serialize(new { name = Name });

        await eventSequences.Append(new AppendRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            EventSequenceId = "event-log",
            CorrelationId = Guid.NewGuid(),
            EventSourceId = QueryId,
            EventSourceType = string.Empty,
            EventStreamType = string.Empty,
            EventStreamId = string.Empty,
            EventType = new Contracts.Events.EventType { Id = SequenceQueryEventTypeIds.Renamed, Generation = 1 },
            Content = content,
            ConcurrencyScope = new ConcurrencyScope { SequenceNumber = ulong.MaxValue, EventSourceId = false },
            Causation = causationManager.GetCurrentChain().ToContract(),
            CausedBy = new Contracts.Identities.Identity { Subject = "workbench", Name = "Workbench", UserName = "workbench" },
            Tags = []
        });
    }
}
