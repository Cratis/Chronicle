// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Api.SequenceQueries.Listing;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.EventSequences.Concurrency;
using IEventSequencesService = Cratis.Chronicle.Contracts.EventSequences.IEventSequences;

namespace Cratis.Chronicle.Api.SequenceQueries.Creating;

/// <summary>
/// Represents the command for creating a new sequence query in the workbench.
/// </summary>
/// <param name="EventStore">The event store context for storing the query event.</param>
/// <param name="Namespace">The namespace context for storing the query event.</param>
/// <param name="Name">The display name of the query to create.</param>
[Command]
public record CreateSequenceQuery(string EventStore, string Namespace, string Name)
{
    /// <summary>
    /// Handles the create sequence query command by appending a creation event to Chronicle.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequencesService"/> gRPC service.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for tracking causation.</param>
    /// <returns>The created <see cref="SequenceQueryDefinition"/>.</returns>
    internal async Task<SequenceQueryDefinition> Handle(
        IEventSequencesService eventSequences,
        ICausationManager causationManager)
    {
        var queryId = SequenceQueryId.New();
        var content = JsonSerializer.Serialize(new { name = Name });

        await eventSequences.Append(new AppendRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            EventSequenceId = "event-log",
            CorrelationId = Guid.NewGuid(),
            EventSourceId = queryId.Value.ToString(),
            EventSourceType = string.Empty,
            EventStreamType = string.Empty,
            EventStreamId = string.Empty,
            EventType = new Contracts.Events.EventType { Id = SequenceQueryEventTypeIds.Created, Generation = 1 },
            Content = content,
            ConcurrencyScope = new ConcurrencyScope { SequenceNumber = ulong.MaxValue, EventSourceId = false },
            Causation = causationManager.GetCurrentChain().ToContract(),
            CausedBy = new Contracts.Identities.Identity { Subject = "workbench", Name = "Workbench", UserName = "workbench" },
            Tags = []
        });

        return new SequenceQueryDefinition(queryId.Value.ToString(), Name, new SequenceQueryFilter());
    }
}
