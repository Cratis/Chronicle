// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts between <see cref="AppendedEvent"/> and its storage representation.
/// </summary>
internal static class AppendedEventConverters
{
    /// <summary>
    /// Converts a storage <see cref="Concepts.Events.AppendedEvent"/> to an <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="appendedEvent">The storage appended event to convert.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> the content is serialized with.</param>
    /// <returns>The converted appended event.</returns>
    public static AppendedEvent ToApi(this Concepts.Events.AppendedEvent appendedEvent, JsonSerializerOptions jsonSerializerOptions) => new(
        appendedEvent.Context.SequenceNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        appendedEvent.Context.ToApi(),
        JsonSerializer.Serialize(appendedEvent.Content, jsonSerializerOptions),
        appendedEvent.OriginalContent,
        appendedEvent.Revisions.Select(r => r.ToApi()).ToArray(),
        appendedEvent.GenerationalContent.Select(kvp => new KeyValuePair<int, string>(kvp.Key, kvp.Value)).ToArray());

    /// <summary>
    /// Converts a collection of storage <see cref="Concepts.Events.AppendedEvent"/> to a collection of <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="appendedEvents">The collection of storage appended events to convert.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> the content is serialized with.</param>
    /// <returns>The converted collection of appended events.</returns>
    public static IEnumerable<AppendedEvent> ToApi(this IEnumerable<Concepts.Events.AppendedEvent> appendedEvents, JsonSerializerOptions jsonSerializerOptions) =>
        appendedEvents.Select(e => e.ToApi(jsonSerializerOptions)).ToArray();

    /// <summary>
    /// Converts a storage <see cref="Concepts.Events.EventRevision"/> to an <see cref="EventRevision"/>.
    /// </summary>
    /// <param name="revision">The storage revision to convert.</param>
    /// <returns>The converted revision.</returns>
    public static EventRevision ToApi(this Concepts.Events.EventRevision revision) => new(
        revision.EventTypeGeneration,
        revision.CorrelationId.ToString(),
        revision.CausedBy.ToApi(),
        revision.Occurred,
        revision.Content);

    /// <summary>
    /// Converts an <see cref="EventRevision"/> to a contract <see cref="Contracts.Sequences.EventRevision"/>.
    /// </summary>
    /// <param name="revision">The revision to convert.</param>
    /// <returns>The converted contract revision.</returns>
    public static Contracts.Sequences.EventRevision ToContract(this EventRevision revision) => new()
    {
        Generation = revision.Generation,
        CorrelationId = revision.CorrelationId,
        CausedBy = revision.CausedBy.ToContract(),
        Occurred = revision.Occurred,
        Content = revision.Content
    };
}
