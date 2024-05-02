// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Aggregates;

/// <summary>
/// Represents a system that is capable of creating instances of <see cref="IAggregateRootEventHandlersFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="AggregateRootEventHandlersFactory"/>.
/// </remarks>
/// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
public class AggregateRootEventHandlersFactory(IEventTypes eventTypes) : IAggregateRootEventHandlersFactory
{
    /// <inheritdoc/>
    public IAggregateRootEventHandlers CreateFor(IAggregateRoot aggregateRoot)
    {
        if (aggregateRoot.IsStateful)
        {
            return NullAggregateRootEventHandlers.Instance;
        }
        return new AggregateRootEventHandlers(eventTypes, aggregateRoot.GetType());
    }
}
