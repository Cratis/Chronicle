// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Extension methods for working with event types as the target of projections and reducers.
/// </summary>
public static class EventTargetExtensions
{
    /// <summary>
    /// Gets whether a projection or reducer target type is an event type rather than a read model.
    /// </summary>
    /// <param name="type">The target type.</param>
    /// <returns>True when the type is an event type.</returns>
    public static bool IsEventTypeTarget(this Type type) =>
        Attribute.IsDefined(type, typeof(EventTypeAttribute)) || Attribute.IsDefined(type, typeof(EventTypeGenerationForAttribute));

    /// <summary>
    /// Gets whether an event type is public for the event store the client is connected to.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="eventStore">The event store the client is connected to.</param>
    /// <returns>True when declared with <see cref="PublicAttribute"/> or owned by the event store.</returns>
    public static bool IsPublicEventType(this Type type, EventStoreName eventStore) =>
        Attribute.IsDefined(type, typeof(PublicAttribute)) || type.GetEventStoreName() == eventStore.Value;

    /// <summary>
    /// Gets the sequence a target event type is published to; the outbox unless <see cref="PublishToAttribute"/> says otherwise.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <returns>The destination sequence.</returns>
    public static EventSequenceId GetPublishingSequence(this Type type) =>
        type.GetCustomAttribute<PublishToAttribute>()?.Sequence ?? EventSequenceId.Outbox;

    /// <summary>
    /// Validates an event type that is the target of a projection or reducer.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="eventStore">The event store the client is connected to.</param>
    /// <exception cref="EventLogIsNotAPublicationTarget">Thrown when the destination is the event log.</exception>
    /// <exception cref="PrivateEventTypeCannotBePublishedToOutbox">Thrown when the outbox is the destination and the event type is not public.</exception>
    public static void ValidateAsEventTarget(this Type type, EventStoreName eventStore)
    {
        var destination = type.GetPublishingSequence();
        if (destination == EventSequenceId.Log)
        {
            throw new EventLogIsNotAPublicationTarget(type);
        }

        if (destination == EventSequenceId.Outbox && !type.IsPublicEventType(eventStore))
        {
            throw new PrivateEventTypeCannotBePublishedToOutbox(type, eventStore);
        }
    }
}
