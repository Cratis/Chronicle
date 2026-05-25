// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.EventSequences.Concurrency;
using IEventSequencesService = Cratis.Chronicle.Contracts.EventSequences.IEventSequences;

namespace Cratis.Chronicle.Api.SequenceQueries.UpdatingFilter;

/// <summary>
/// Represents the command for updating the filter of an existing sequence query.
/// </summary>
/// <param name="EventStore">The event store context for the query.</param>
/// <param name="Namespace">The namespace context for the query.</param>
/// <param name="QueryId">The unique identifier of the query to update.</param>
/// <param name="Filter">The new filter definition to apply to the query.</param>
[Command]
public record UpdateSequenceQueryFilter(
    string EventStore,
    string Namespace,
    string QueryId,
    SequenceQueryFilter Filter)
{
    /// <summary>
    /// Handles the update sequence query filter command by appending a filter update event to Chronicle.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequencesService"/> gRPC service.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for tracking causation.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(
        IEventSequencesService eventSequences,
        ICausationManager causationManager)
    {
        var content = JsonSerializer.Serialize(Filter);

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
            EventType = new Contracts.Events.EventType { Id = SequenceQueryEventTypeIds.FilterUpdated, Generation = 1 },
            Content = content,
            ConcurrencyScope = new ConcurrencyScope { SequenceNumber = ulong.MaxValue, EventSourceId = false },
            Causation = causationManager.GetCurrentChain().ToContract(),
            CausedBy = new Contracts.Identities.Identity { Subject = "workbench", Name = "Workbench", UserName = "workbench" },
            Tags = []
        });
    }
}
