// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task<AppendResult> AppendValidated(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        IEnumerable<Tag> tags,
        ConcurrencyScope concurrencyScope,
        DateTimeOffset? occurred,
        Subject? subject,
        IReadOnlyCollection<NamedTag> namedTags,
        EventSourceName? eventSource,
        EventPublication? publication = null)
    {
        try
        {
            if (publication is not null)
            {
                var publicationStorage = EventSequenceStorage as IEventPublicationStorage ?? throw new EventPublicationStorageNotSupported();
                if ((await publicationStorage.TryGetPublication(publication)).TryGetValue(out var receipt))
                {
                    await ReconcileExistingPublication(receipt, eventType, eventSourceId, eventSourceType, eventStreamType, eventStreamId, content);
                    return AppendResult.Success(correlationId, receipt.SequenceNumber);
                }
            }

            await RefreshConstraintsIfChanged();
            var resolvedEventSourceType = await EventSourceResolution.Resolve(EventSourcesStorage, eventSource, eventSourceType, eventStreamType, correlationId);
            if (resolvedEventSourceType.TryGetError(out var eventSourceError))
            {
                return eventSourceError;
            }

            eventSourceType = resolvedEventSourceType.AsT0;
            var getValidAndCompliantEvent = await GetValidAndCompliantEvent(eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, content, correlationId, subject);
            if (getValidAndCompliantEvent.TryGetError(out var error))
            {
                return error;
            }

            var (compliantEvent, compliantContent, constraintContext) = getValidAndCompliantEvent.AsT0;
            var concurrencyCheckPerformed = concurrencyScope.ShouldBeValidated;
            var maybeConcurrencyViolation = await ConcurrencyValidator.Validate(eventSourceId, concurrencyScope);
            if (maybeConcurrencyViolation.TryGetValue(out var concurrencyViolation))
            {
                return AppendResult.Failed(correlationId, concurrencyViolation).ReportingConcurrencyCheck(concurrencyCheckPerformed);
            }

            var appendResult = await AppendValidAndCompliantEvent(
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
                eventType,
                correlationId,
                causation,
                causedBy,
                tags,
                compliantEvent,
                content,
                constraintContext,
                occurred,
                subject,
                namedTags,
                eventSource,
                publication);

            return appendResult.ReportingConcurrencyCheck(concurrencyCheckPerformed);
        }
        catch (Exception ex)
        {
            return HandleAppendEventException(ex, eventSourceType, eventSourceId, eventType, eventStreamId, correlationId);
        }
    }

    /// <summary>
    /// Brings the grain state and the post-append side effects in line with a publication that is already durable.
    /// </summary>
    /// <param name="receipt">The <see cref="EventPublicationReceipt"/> of the durable publication.</param>
    /// <param name="eventType">The <see cref="EventType"/> that was published.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the publication.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> of the publication.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> of the publication.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> of the publication.</param>
    /// <param name="content">The content of the publication.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The publication can be durable while the activation that wrote it crashed before it advanced the sequence
    /// number, the per event type tail, handed the event to the observers or updated the constraint indexes. A retry
    /// finding the receipt must therefore finish that work (all of it is idempotent) instead of just reporting
    /// success. Constraints are deliberately not validated: the event itself already occupies its unique values.
    /// </remarks>
    async Task ReconcileExistingPublication(
        EventPublicationReceipt receipt,
        EventType eventType,
        EventSourceId eventSourceId,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        JsonObject content)
    {
        var appendedEvent = await EventSequenceStorage.GetEventAt(receipt.SequenceNumber);
        if (State.SequenceNumber <= receipt.SequenceNumber)
        {
            State.SequenceNumber = receipt.SequenceNumber.Next();
        }

        if (!State.TailSequenceNumberPerEventType.TryGetValue(eventType.Id, out var tail) || tail < receipt.SequenceNumber)
        {
            State.TailSequenceNumberPerEventType[eventType.Id] = receipt.SequenceNumber;
        }

        await RefreshConstraintsIfChanged();
        var eventSchema = await EventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        var constraintContext = _constraints!.Establish(
            eventSourceId,
            eventType.Id,
            expandoObjectConverter.ToExpandoObject(content, eventSchema.Schema),
            eventSourceType,
            eventStreamType,
            eventStreamId);
        await CompleteDurableAppend([appendedEvent], [(constraintContext, receipt.SequenceNumber)]);
    }

    /// <summary>
    /// Enforces that only public event types go to the outbox and no public event type goes to the event log.
    /// </summary>
    /// <param name="eventType">The <see cref="EventType"/> being appended.</param>
    /// <param name="eventSchema">The registered <see cref="EventTypeSchema"/> carrying the visibility.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the append.</param>
    /// <returns>A failed <see cref="AppendResult"/> when refused, otherwise nothing.</returns>
    /// <remarks>
    /// An unspecified visibility comes from a client that predates public event types and is allowed with a
    /// warning, once per event type per activation, so existing systems keep working.
    /// </remarks>
    AppendResult? EnforceEventTypeVisibility(EventType eventType, EventTypeSchema eventSchema, CorrelationId correlationId)
    {
        var isOutbox = _eventSequenceId == EventSequenceId.Outbox;
        var isEventLog = _eventSequenceId == EventSequenceId.Log;
        if (!isOutbox && !isEventLog)
        {
            return null;
        }

        switch (eventSchema.Visibility)
        {
            case EventTypeVisibility.Private when isOutbox:
                logger.EventTypeVisibilityRefused(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, eventType, "the event type is private and only public event types can be appended to the outbox");
                return AppendResult.Failed(correlationId, [AppendError.PrivateEventTypeCannotBeAppendedToOutbox]);

            case EventTypeVisibility.Public when isEventLog:
                logger.EventTypeVisibilityRefused(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, eventType, "the event type is public and public events are produced from private events, not appended to the event log");
                return AppendResult.Failed(correlationId, [AppendError.PublicEventTypeCannotBeAppendedToEventLog]);

            case EventTypeVisibility.Unspecified when _unspecifiedVisibilityWarned.Add(eventType.Id):
                logger.EventTypeVisibilityNotSpecified(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, eventType);
                return null;

            default:
                return null;
        }
    }
}
