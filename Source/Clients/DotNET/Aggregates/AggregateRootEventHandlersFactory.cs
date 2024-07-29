// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents a system that is capable of creating instances of <see cref="IAggregateRootEventHandlersFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="AggregateRootEventHandlersFactory"/>.
/// </remarks>
/// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
public class AggregateRootEventHandlersFactory(IEventTypes eventTypes) : IAggregateRootEventHandlersFactory
{
    readonly Dictionary<Type, AggregateRootEventHandlers> _handlers = [];

    /// <inheritdoc/>
    public IAggregateRootEventHandlers GetFor(IAggregateRoot aggregateRoot)
    {
        var aggregateRootType = aggregateRoot.GetType();
        if (_handlers.TryGetValue(aggregateRootType, out var handlers)) return handlers;
        return _handlers[aggregateRootType] = new AggregateRootEventHandlers(eventTypes, aggregateRoot.GetType());
    }
}
