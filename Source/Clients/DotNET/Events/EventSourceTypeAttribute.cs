// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to specify the <see cref="EventSourceType"/> for a command or observer.
/// </summary>
/// <remarks>
/// When applied to an observer (reactor or reducer), it filters observed events to only
/// those originating from the given event source type. When applied to a command, setting
/// <paramref name="concurrency"/> to <see langword="true"/> includes this value in the concurrency
/// scope when appending events.
/// <para>
/// A projection is not one of them. It observes every event of the types its definition declares, and cannot
/// filter on event metadata at all - there is no field for a filter anywhere in a projection's definition, so
/// no client can express one and no kernel could honour it. Narrow a projection by the event types it declares,
/// or pair it with a reactor or reducer that owns the filtered subset. See Documentation/projections/filtering.
/// </para>
/// <para>
/// Neither is an event type. An append resolves its <see cref="EventSourceType"/> from the arguments it is given,
/// never from the CLR type of the event being appended, so the attribute placed on an event type is read by
/// nothing and reaches no appended event's context. Declare it on the command that appends the event, or on the
/// reactor or reducer that observes it.
/// </para>
/// </remarks>
/// <param name="value">The <see cref="EventSourceType"/> value.</param>
/// <param name="concurrency">
/// Whether to include this metadata in the concurrency scope when appending events.
/// Default is <see langword="false"/>.
/// </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class EventSourceTypeAttribute(string value, bool concurrency = false) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventSourceType"/>.
    /// </summary>
    public EventSourceType EventSourceType { get; } = value;

    /// <summary>
    /// Gets a value indicating whether this metadata should be included in the concurrency scope.
    /// </summary>
    public bool Concurrency { get; } = concurrency;
}
