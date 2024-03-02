// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Defines a system that can manage <see cref="IAggregateRootStateProvider"/> for <see cref="AggregateRoot"/>.
/// </summary>
public interface IAggregateRootStateProviders
{
    /// <summary>
    /// Create a <see cref="IAggregateRootStateProvider"/> for a specific <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="aggregateRoot"><see cref="AggregateRoot"/> to create for.</param>
    /// <returns>A new <see cref="IAggregateRootStateProvider"/> instance.</returns>
    /// <exception cref="MissingAggregateRootStateProvider">Thrown if it can't resolve a state provider for the aggregate root.</exception>
    /// <exception cref="AmbiguousAggregateRootStateProvider">Thrown if there are multiple state providers for the same aggregate root, this is not allowed.</exception>
    Task<IAggregateRootStateProvider> CreateFor(AggregateRoot aggregateRoot);
}
