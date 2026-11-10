// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Sequences;
using Cratis.Chronicle.Storage.EventSources;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Checks one import event for everything that does not depend on the order of the log, and turns it into a staged event.
/// </summary>
internal static class ImportEventStaging
{
    /// <summary>
    /// Stages one event.
    /// </summary>
    /// <param name="event">The <see cref="ImportEvent"/>.</param>
    /// <param name="index">The index of the event in the import.</param>
    /// <param name="causedBy">The identity recorded as having caused the event.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the import targets.</param>
    /// <param name="eventTypes">The <see cref="IEventTypesStorage"/> holding the registered schemas.</param>
    /// <param name="eventSources">The <see cref="IEventSourcesStorage"/> holding the registered event sources.</param>
    /// <returns>The staged event, or the rejection.</returns>
    internal static async Task<(StagedImportEvent? Staged, ImportRejection? Rejection)> Stage(
        ImportEvent @event,
        long index,
        Concepts.Identities.Identity causedBy,
        EventSequenceId eventSequenceId,
        IEventTypesStorage eventTypes,
        IEventSourcesStorage eventSources)
    {
        var eventType = @event.EventType.ToChronicle();
        ImportRejection Reject(params string[] reasons) => new(index, eventType, reasons);

        if (!string.IsNullOrEmpty(@event.IdempotencyKey) && string.IsNullOrWhiteSpace(@event.IdempotencyKey))
            return (null, Reject("Idempotency key must not be only whitespace."));
        if (@event.IdempotencyKey?.Length > IdempotencyKey.MaxLength)
            return (null, Reject($"Idempotency key must not exceed {IdempotencyKey.MaxLength} characters."));
        if (string.IsNullOrWhiteSpace(@event.EventSourceId))
            return (null, Reject("The event source identifier is required."));

        JsonObject content;
        try
        {
            if (JsonNode.Parse(@event.Content) is not JsonObject parsed) return (null, Reject("The content is not a JSON object."));
            content = parsed;
        }
        catch (Exception)
        {
            return (null, Reject("The content is not a JSON object."));
        }

        if (!await eventTypes.HasFor(eventType.Id, eventType.Generation))
            return (null, Reject($"The event type '{eventType}' is not registered."));

        var schema = await eventTypes.GetFor(eventType.Id, eventType.Generation);
        if (schema.Visibility == EventTypeVisibility.Public && eventSequenceId == EventSequenceId.Log)
            return (null, Reject("The event type is public and public events are produced from private events, not appended to the event log."));

        var schemaErrors = schema.Schema.Validate(content);
        if (schemaErrors.Count > 0)
            return (null, Reject([.. schemaErrors.Select(error => $"SchemaValidation: {error}")]));

        var route = AppendRoute.Resolve(@event.EventSourceType, @event.EventStreamType, @event.EventStreamId);
        var eventSource = string.IsNullOrEmpty(@event.EventSource) ? null : new EventSourceName(@event.EventSource);
        var resolved = await EventSourceResolution.Resolve(eventSources, eventSource, route.SourceType, route.StreamType, CorrelationId.New());
        if (resolved.TryGetError(out var resolution))
            return (null, Reject([.. resolution.Errors.Select(_ => _.Value), .. resolution.ConstraintViolations.Select(_ => _.Message.Value)]));

        return (new StagedImportEvent(
            index,
            eventType,
            resolved.AsT0,
            @event.EventSourceId,
            route.StreamType,
            route.StreamId,
            @event.Content,
            @event.Tags?.ToArray() ?? [],
            @event.NamedTags?.Select(_ => new StagedImportNamedTag(_.Name, _.Value)).ToArray() ?? [],
            @event.Occurred,
            string.IsNullOrWhiteSpace(@event.Subject) ? null : @event.Subject,
            @event.CorrelationId,
            @event.Causation?.ToChronicle().ToArray(),
            causedBy,
            string.IsNullOrEmpty(@event.IdempotencyKey) ? null : @event.IdempotencyKey,
            eventSource?.Value,
            @event.SourceSequenceNumber), null);
    }
}
