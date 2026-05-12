// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to specify the <see cref="EventSourceType"/> for an event type, command, or observer.
/// </summary>
/// <remarks>
/// When applied to an observer (reactor, reducer, or projection), it filters observed events to only
/// those originating from the given event source type. When applied to a command, setting
/// <paramref name="concurrency"/> to <see langword="true"/> includes this value in the concurrency
/// scope when appending events.
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
