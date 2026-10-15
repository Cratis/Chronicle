// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Resolves and validates the inbox an events capture reads from, based on the visibility and origin the
/// event types it captures were registered with.
/// </summary>
/// <remarks>
/// A captured type must be <see cref="EventTypeVisibility.Public"/> and carry the origin of the event store whose
/// inbox it arrives through. <see cref="EventTypeVisibility.Unspecified"/> is not accepted: an event type that was
/// never declared public cannot be known to be part of another store's public contract, and an inbox only ever
/// delivers public events. There is no leniency for clients that predate visibility.
/// </remarks>
public static class EventsCaptureSequence
{
    /// <summary>
    /// Resolves an events source using its registered event types.
    /// </summary>
    /// <param name="eventTypes">The event type storage of the consuming event store.</param>
    /// <param name="source">The source to resolve.</param>
    /// <returns>The inbox, schemas and problems preventing resolution.</returns>
    public static async Task<(string Sequence, IReadOnlyList<EventTypeSchema> Schemas, IReadOnlyList<string> Errors)> ResolveFor(IEventTypesStorage eventTypes, SourceDefinition source)
    {
        var schemas = new List<EventTypeSchema>();
        var errors = new List<string>();
        foreach (var name in (source.Events ?? []).Distinct())
        {
            var id = new EventTypeId(name);
            if (await eventTypes.HasFor(id))
            {
                schemas.Add(await eventTypes.GetFor(id));
            }
            else
            {
                errors.Add($"There is no event type named '{name}' to capture from");
            }
        }

        errors.AddRange(Resolve(source.Sequence, schemas, out var sequence));
        return (sequence, schemas, errors);
    }

    /// <summary>
    /// Resolve the inbox sequence of an events capture.
    /// </summary>
    /// <param name="declared">The sequence named in the declaration, if any.</param>
    /// <param name="sourceTypes">The schemas of the event types the capture reads from.</param>
    /// <param name="sequence">The resolved inbox sequence - only meaningful when there are no errors.</param>
    /// <returns>The problems preventing the capture from being resolved - empty when it resolved.</returns>
    public static IReadOnlyList<string> Resolve(string? declared, IEnumerable<EventTypeSchema> sourceTypes, out string sequence)
    {
        var errors = new List<string>();
        var schemas = sourceTypes.ToArray();
        sequence = declared ?? string.Empty;

        foreach (var schema in schemas)
        {
            if (schema.Visibility != EventTypeVisibility.Public)
            {
                errors.Add($"'{schema.Type.Id}' is {schema.Visibility.ToString().ToLowerInvariant()}, not public - an events source can only capture public events");
            }
        }

        if (string.IsNullOrWhiteSpace(declared))
        {
            DeriveFromOrigin(schemas, errors, ref sequence);
            return errors;
        }

        if (!declared.StartsWith(EventSequenceId.InboxPrefix, StringComparison.Ordinal) || declared.Length == EventSequenceId.InboxPrefix.Length)
        {
            errors.Add($"'{declared}' is not an inbox - an events source captures public events from another event store's inbox, which is named '{EventSequenceId.InboxPrefix}<event store>'. Private event sequences such as the event log, the outbox and the system sequence cannot be captured from");
            return errors;
        }

        var store = declared[EventSequenceId.InboxPrefix.Length..];
        foreach (var schema in schemas.Where(schema => schema.Visibility == EventTypeVisibility.Public && schema.Origin != store))
        {
            errors.Add(string.IsNullOrEmpty(schema.Origin)
                ? $"'{schema.Type.Id}' does not originate from another event store, so it never arrives in '{declared}'"
                : $"'{schema.Type.Id}' originates from '{schema.Origin}', so it never arrives in '{declared}'");
        }

        return errors;
    }

    static void DeriveFromOrigin(EventTypeSchema[] schemas, List<string> errors, ref string sequence)
    {
        var origins = schemas.Select(schema => schema.Origin).Where(origin => origin.Length > 0).Distinct().ToArray();
        if (origins.Length == 1 && schemas.All(schema => schema.Origin.Length > 0))
        {
            sequence = $"{EventSequenceId.InboxPrefix}{origins[0]}";
            return;
        }

        if (schemas.Length == 0)
        {
            return;
        }

        errors.AddRange(schemas.Where(schema => string.IsNullOrEmpty(schema.Origin))
            .Select(schema => $"'{schema.Type.Id}' has no origin; register it with the [EventStore] of its owner"));
        if (origins.Length > 1)
        {
            errors.Add($"The 'from' events originate from several event stores ({string.Join(", ", origins.Order())}); they must all originate from one event store - split into one capture per origin");
        }
    }
}
