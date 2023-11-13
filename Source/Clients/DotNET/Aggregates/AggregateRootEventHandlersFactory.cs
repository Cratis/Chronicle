// Copyright (c) Aksio Insurtech. All rights reserved.
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
    /// Initializes a new instance of the <see cref="AggregateRootEventHandlersFactory"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for working with event types.</param>
    public AggregateRootEventHandlersFactory(IEventTypes eventTypes) => _eventTypes = eventTypes;

    /// <inheritdoc/>
    public IAggregateRootEventHandlers Create(Type type) => new AggregateRootEventHandlers(type, _eventTypes);
}
