// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to specify the <see cref="EventStreamType"/> for an event type, command, or observer.
/// </summary>
/// <remarks>
/// When applied to an observer (reactor or reducer), it filters observed events to only
/// those belonging to the given event stream type. When applied to a command, setting
/// <paramref name="concurrency"/> to <see langword="true"/> includes this value in the concurrency
/// scope when appending events.
/// <para>
/// A projection is not one of them. It observes every event of the types its definition declares, and cannot
/// filter on event metadata at all - there is no field for a filter anywhere in a projection's definition, so
/// no client can express one and no kernel could honour it. Narrow a projection by the event types it declares,
/// or pair it with a reactor or reducer that owns the filtered subset. See Documentation/projections/filtering.
/// </para>
/// </remarks>
/// <param name="value">The <see cref="EventStreamType"/> value.</param>
/// <param name="concurrency">
/// Whether to include this metadata in the concurrency scope when appending events.
/// Default is <see langword="false"/>.
/// </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class EventStreamTypeAttribute(string value, bool concurrency = false) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventStreamType"/>.
    /// </summary>
    public EventStreamType EventStreamType { get; } = value;

    /// <summary>
    /// Gets a value indicating whether this metadata should be included in the concurrency scope.
    /// </summary>
    public bool Concurrency { get; } = concurrency;
}
