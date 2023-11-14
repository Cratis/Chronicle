// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Defines a system that can manage state for an <see cref="AggregateRoot"/>.
/// </summary>
public interface IAggregateRootStateProvider
{
    /// <summary>
    /// Handle state for an <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="aggregateRoot">The <see cref="AggregateRoot"/> to handle state for.</param>
    /// <returns>Awaitable task.</returns>
    Task Provide(AggregateRoot aggregateRoot);
}
