// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents utilities for creating <see cref="ValueProvider{T}"/> instances for providing values from <see cref="AppendedEvent">events</see>.
/// </summary>
public static class EventValueProviders
{
    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides the event source id from an event.
    /// </summary>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static readonly ValueProvider<AppendedEvent> EventSourceId = @event => @event.Context.EventSourceId.Value;

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides a value from the event content.
    /// </summary>
    /// <param name="sourceProperty">Source property.</param>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> EventContent(PropertyPath sourceProperty)
    {
        return @event => sourceProperty.GetValue(@event.Content, ArrayIndexers.NoIndexers)!;
    }

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides a value from the <see cref="EventContext"/>.
    /// </summary>
    /// <param name="sourceProperty">Property on the context.</param>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> EventContext(PropertyPath sourceProperty)
    {
        return @event => sourceProperty.GetValue(@event.Context, ArrayIndexers.NoIndexers)!;
    }

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that provides a constant value.
    /// </summary>
    /// <param name="value">Constant to provide.</param>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> Value(string value) => _ => value;

    /// <summary>
    /// Create a <see cref="ValueProvider{T}"/> that generates a new unique identifier from the event metadata.
    /// </summary>
    /// <returns>A new <see cref="ValueProvider{T}"/>.</returns>
    public static ValueProvider<AppendedEvent> UniqueIdentifier() => @event => $"{@event.Context.SequenceNumber}-{@event.Context.Occurred.ToUnixTimeMilliseconds()}";
}
