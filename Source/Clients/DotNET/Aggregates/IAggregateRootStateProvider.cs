// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

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
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to provide events from.</param>
    /// <returns>Collection of <see cref="AppendedEvent"/> potentially provided.</returns>
    Task<IImmutableList<AppendedEvent>> Provide(AggregateRoot aggregateRoot, IEventSequence eventSequence);
}
