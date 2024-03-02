// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents a system that is capable of creating instances of <see cref="IAggregateRootEventHandlersFactory"/>.
/// </summary>
public class AggregateRootEventHandlersFactory : IAggregateRootEventHandlersFactory
{
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of <see cref="AggregateRootEventHandlersFactory"/>.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    public AggregateRootEventHandlersFactory(IEventTypes eventTypes)
    {
        _eventTypes = eventTypes;
    }

    /// <inheritdoc/>
    public IAggregateRootEventHandlers CreateFor(IAggregateRoot aggregateRoot)
    {
        if (aggregateRoot.IsStateful)
        {
            return NullAggregateRootEventHandlers.Instance;
        }
        return new AggregateRootEventHandlers(_eventTypes, aggregateRoot.GetType());
    }
}
