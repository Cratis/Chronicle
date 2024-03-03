// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Defines a system that is capable of creating instances of <see cref="IAggregateRootEventHandlers"/>.
/// </summary>
public interface IAggregateRootEventHandlersFactory
{
    /// <summary>
    /// Create an instance of <see cref="IAggregateRootEventHandlers"/> for a specific aggregate root type.
    /// </summary>
    /// <param name="aggregateRoot">The <see cref="IAggregateRoot"/> to create for.</param>
    /// <returns>A new <see cref="IAggregateRootEventHandlers"/> instance.</returns>
    IAggregateRootEventHandlers CreateFor(IAggregateRoot aggregateRoot);
}
