// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// An immutable snapshot of an event's serialized content, bound to the sequence that prepared it.
/// </summary>
/// <remarks>
/// Create through <see cref="IEventSequence.Prepare"/> and reuse for append and verification.
/// Additional-information providers run only during preparation. The snapshot contains plaintext;
/// retain it only as long as needed to resolve the append outcome.
/// </remarks>
public sealed class PreparedEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreparedEvent"/> class.
    /// </summary>
    /// <param name="owner">The owning sequence.</param>
    /// <param name="event">The original event for append notifications.</param>
    /// <param name="eventType">The captured type and generation.</param>
    /// <param name="content">The serialized snapshot.</param>
    /// <param name="subject">The captured subject.</param>
    /// <param name="tags">The static event type tags.</param>
    internal PreparedEvent(IEventSequence owner, object @event, EventType eventType, string content, Subject? subject, ImmutableList<string> tags)
    {
        Owner = owner;
        Event = @event;
        EventType = eventType;
        Content = content;
        Subject = subject;
        Tags = tags;
    }

    /// <summary>
    /// Gets the owning sequence.
    /// </summary>
    internal IEventSequence Owner { get; }

    /// <summary>
    /// Gets the original event used for append notifications, never for serialization.
    /// </summary>
    internal object Event { get; }

    /// <summary>
    /// Gets the captured event type and generation.
    /// </summary>
    internal EventType EventType { get; }

    /// <summary>
    /// Gets the immutable serialized content.
    /// </summary>
    internal string Content { get; }

    /// <summary>
    /// Gets the captured subject.
    /// </summary>
    internal Subject? Subject { get; }

    /// <summary>
    /// Gets the static event type tags.
    /// </summary>
    internal ImmutableList<string> Tags { get; }
}
