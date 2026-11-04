// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <inheritdoc/>
    public async Task<AppendResult> Compensate(
        EventSequenceNumber compensationFor,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        IEnumerable<Tag> tags,
        DateTimeOffset? occurred = null)
    {
        var tail = await EventSequenceStorage.GetTailSequenceNumber();
        if (!compensationFor.IsActualValue || !tail.IsActualValue || compensationFor.Value > tail.Value)
        {
            return AppendResult.Failed(correlationId, [AppendError.CompensatedEventNotFound]);
        }

        var original = await EventSequenceStorage.GetEventAt(compensationFor);
        var schema = await EventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        if (schema.Schema.GetCompensationFor() != original.Context.EventType.Id.Value)
        {
            return AppendResult.Failed(correlationId, [AppendError.EventTypeDoesNotCompensateEventType]);
        }

        var originalContext = original.Context;
        var subject = originalContext.SubjectIsEventSourceId ? null : originalContext.Subject;

        return await AppendValidated(
            originalContext.EventSourceType,
            originalContext.EventSourceId,
            originalContext.EventStreamType,
            originalContext.EventStreamId,
            eventType,
            content,
            correlationId,
            causation,
            causedBy,
            tags,
            ConcurrencyScope.None,
            occurred,
            subject,
            [],
            null,
            null,
            compensationFor);
    }
}
