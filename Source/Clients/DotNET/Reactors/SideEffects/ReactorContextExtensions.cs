// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Extension methods for <see cref="ReactorContext"/> that resolve append-metadata from the
/// reactor instance, its type attributes, and the incoming <see cref="EventContext"/>.
/// </summary>
public static class ReactorContextExtensions
{
    /// <summary>
    /// Resolves the <see cref="EventSourceId"/> to use when appending side-effect events.
    /// </summary>
    /// <remarks>
    /// Resolution order:
    /// <list type="number">
    ///   <item>Reactor implements <see cref="ICanProvideEventSourceId"/> — calls <see cref="ICanProvideEventSourceId.GetEventSourceId"/>.</item>
    ///   <item>Falls back to <see cref="EventContext.EventSourceId"/> of the triggering event.</item>
    /// </list>
    /// </remarks>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The resolved <see cref="EventSourceId"/>.</returns>
    public static EventSourceId GetEventSourceId(this ReactorContext reactorContext)
    {
        if (reactorContext.Reactor is ICanProvideEventSourceId provider)
        {
            return provider.GetEventSourceId();
        }

        return reactorContext.EventContext.EventSourceId;
    }

    /// <summary>
    /// Resolves the <see cref="EventStreamType"/> to use when appending side-effect events.
    /// </summary>
    /// <remarks>
    /// Reads the <see cref="EventStreamTypeAttribute"/> from the reactor type.
    /// Returns <see langword="null"/> when the attribute is absent.
    /// </remarks>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The <see cref="EventStreamType"/> from the reactor's attribute, or <see langword="null"/>.</returns>
    public static EventStreamType? GetEventStreamType(this ReactorContext reactorContext)
    {
        var attribute = Attribute.GetCustomAttribute(reactorContext.Reactor.GetType(), typeof(EventStreamTypeAttribute)) as EventStreamTypeAttribute;
        return attribute?.EventStreamType;
    }

    /// <summary>
    /// Resolves the <see cref="EventStreamId"/> to use when appending side-effect events.
    /// </summary>
    /// <remarks>
    /// Resolution order:
    /// <list type="number">
    ///   <item>Reactor implements <see cref="ICanProvideEventStreamId"/> — calls <see cref="ICanProvideEventStreamId.GetEventStreamId"/>. Takes priority over the attribute.</item>
    ///   <item>Reactor type has <see cref="EventStreamIdAttribute"/> with a value other than <see cref="EventStreamId.NotSet"/> (the sentinel for "not configured").</item>
    ///   <item>Returns <see langword="null"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The resolved <see cref="EventStreamId"/>, or <see langword="null"/>.</returns>
    public static EventStreamId? GetEventStreamId(this ReactorContext reactorContext)
    {
        if (reactorContext.Reactor is ICanProvideEventStreamId provider)
        {
            return provider.GetEventStreamId();
        }

        var attribute = Attribute.GetCustomAttribute(reactorContext.Reactor.GetType(), typeof(EventStreamIdAttribute)) as EventStreamIdAttribute;
        if (attribute is not null && attribute.Value != EventStreamId.NotSet)
        {
            return attribute.Value;
        }

        return null;
    }

    /// <summary>
    /// Resolves the <see cref="EventSourceType"/> to use when appending side-effect events.
    /// </summary>
    /// <remarks>
    /// Reads the <see cref="EventSourceTypeAttribute"/> from the reactor type.
    /// Returns <see langword="null"/> when the attribute is absent.
    /// </remarks>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The <see cref="EventSourceType"/> from the reactor's attribute, or <see langword="null"/>.</returns>
    public static EventSourceType? GetEventSourceType(this ReactorContext reactorContext)
    {
        var attribute = Attribute.GetCustomAttribute(reactorContext.Reactor.GetType(), typeof(EventSourceTypeAttribute)) as EventSourceTypeAttribute;
        return attribute?.EventSourceType;
    }

    /// <summary>
    /// Resolves the <see cref="Subject"/> to use when appending side-effect events.
    /// </summary>
    /// <remarks>
    /// Resolution order:
    /// <list type="number">
    ///   <item>Reactor implements <see cref="ICanProvideSubject"/> — calls <see cref="ICanProvideSubject.GetSubject"/>.</item>
    ///   <item>Returns <see langword="null"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The <see cref="Subject"/> from the reactor, or <see langword="null"/>.</returns>
    public static Subject? GetSubject(this ReactorContext reactorContext)
    {
        if (reactorContext.Reactor is ICanProvideSubject provider)
        {
            return provider.GetSubject();
        }

        return null;
    }
}
