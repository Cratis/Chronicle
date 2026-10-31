// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Configures the scope closed by one event type.
/// </summary>
/// <typeparam name="TEvent">The closing event type.</typeparam>
public interface IClosesStreamBuilder<TEvent>
{
    /// <summary>
    /// Include the event source identifier.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IClosesStreamBuilder<TEvent> PerEventSourceId();

    /// <summary>
    /// Include the event source type.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IClosesStreamBuilder<TEvent> PerEventSourceType();

    /// <summary>
    /// Include the stream type.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IClosesStreamBuilder<TEvent> PerEventStreamType();

    /// <summary>
    /// Include the stream identifier.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IClosesStreamBuilder<TEvent> PerEventStreamId();

    /// <summary>
    /// Resolve the stream identifier from the payload instead of the append's stream metadata.
    /// </summary>
    /// <param name="property">The property carrying the stream identifier.</param>
    /// <returns>The builder for continuation.</returns>
    IClosesStreamBuilder<TEvent> EventStreamIdFrom(Expression<Func<TEvent, object?>> property);

    /// <summary>
    /// Declare an event type that reopens this constraint's exact scope.
    /// </summary>
    /// <typeparam name="TReopen">The reopening event type.</typeparam>
    /// <returns>The builder for continuation.</returns>
    IClosesStreamBuilder<TEvent> ReopenedBy<TReopen>();
}
