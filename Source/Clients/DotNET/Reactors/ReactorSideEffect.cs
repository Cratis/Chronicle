// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents a side effect produced by a reactor handler — an event to be appended to an event sequence.
/// </summary>
/// <remarks>
/// <para>
/// Return a <see cref="ReactorSideEffect"/> (or an <see cref="IEnumerable{T}"/> of them) from a reactor
/// handler method to append events without taking a direct dependency on <see cref="IEventLog"/> or
/// any specific event sequence.
/// </para>
/// <para>
/// When only the event is specified (via <see cref="For"/>), the <see cref="EventSourceId"/> from the
/// incoming <see cref="EventContext"/> is used and the event is appended to the <see cref="IEventLog"/>.
/// </para>
/// <para>
/// You can also return the event object directly as <c>Task&lt;TEvent&gt;</c> or a collection of events
/// as <c>Task&lt;IEnumerable&lt;object&gt;&gt;</c> — in that case the same defaults apply.
/// </para>
/// </remarks>
public record ReactorSideEffect
{
    /// <summary>
    /// Gets the event to append.
    /// </summary>
    public required object Event { get; init; }

    /// <summary>
    /// Gets the <see cref="EventSourceId"/> to append for.
    /// When <see langword="null"/>, the <see cref="EventSourceId"/> from the triggering
    /// <see cref="EventContext"/> is used.
    /// </summary>
    public EventSourceId? EventSourceId { get; init; }

    /// <summary>
    /// Gets the identifier of the target event sequence.
    /// When <see langword="null"/>, the <see cref="IEventLog"/> is used.
    /// </summary>
    public EventSequenceId? EventSequenceId { get; init; }

    /// <summary>
    /// Gets the <see cref="EventStreamType"/> to append to.
    /// When <see langword="null"/>, the default stream type is used.
    /// </summary>
    public EventStreamType? EventStreamType { get; init; }

    /// <summary>
    /// Gets the <see cref="EventStreamId"/> to append to.
    /// When <see langword="null"/>, the default stream is used.
    /// </summary>
    public EventStreamId? EventStreamId { get; init; }

    /// <summary>
    /// Gets the <see cref="EventSourceType"/> to append for.
    /// When <see langword="null"/>, the default source type is used.
    /// </summary>
    public EventSourceType? EventSourceType { get; init; }

    /// <summary>
    /// Gets the <see cref="Subject"/> identifying the target the event is about.
    /// When <see langword="null"/>, the effective <see cref="EventSourceId"/> is used as the subject.
    /// </summary>
    public Subject? Subject { get; init; }

    /// <summary>
    /// Creates a new <see cref="ReactorSideEffect"/> for the given event with default metadata.
    /// </summary>
    /// <param name="event">The event to append.</param>
    /// <returns>A new <see cref="ReactorSideEffect"/>.</returns>
    public static ReactorSideEffect For(object @event) => new() { Event = @event };
}
