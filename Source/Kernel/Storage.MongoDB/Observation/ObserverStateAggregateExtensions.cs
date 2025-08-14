// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Extension methods for aggregation related to <see cref="ObserverState"/>.
/// </summary>
public static class ObserverStateAggregateExtensions
{
    /// <summary>
    /// Join with failed partitions.
    /// </summary>
    /// <param name="aggregation">The aggregation to join.</param>
    /// <returns>New aggregation now based on <see cref="ObserverStateWithFailedPartitions"/>.</returns>
    public static IAggregateFluent<ObserverStateWithFailedPartitions> JoinWithFailedPartitions(this IAggregateFluent<ObserverState> aggregation) => aggregation
        .Lookup(
            WellKnownCollectionNames.FailedPartitions,
            new ExpressionFieldDefinition<ObserverState>((ObserverState _) => _.Id),
            new ExpressionFieldDefinition<FailedPartition>((FailedPartition _) => _.ObserverId),
            new ExpressionFieldDefinition<ObserverStateWithFailedPartitions>((ObserverStateWithFailedPartitions _) => _.FailedPartitions));
}
