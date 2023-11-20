// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents a null implementation of <see cref="IAggregateRootEventHandlers"/>.
/// </summary>
public class NullAggregateRootEventHandlers : IAggregateRootEventHandlers
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullAggregateRootEventHandlers"/>.
    /// </summary>
    public static readonly NullAggregateRootEventHandlers Instance = new();

    /// <inheritdoc/>
    public bool HasHandleMethods => false;

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes => ImmutableList<EventType>.Empty;

    /// <inheritdoc/>
    public Task Handle(IAggregateRoot target, IEnumerable<EventAndContext> events) => Task.CompletedTask;
}
