// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Observation;
using Cratis.Kernel.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Observation;

/// <summary>
/// Extension methods for aggregation related to <see cref="ObserverState"/>.
/// </summary>
public static class ObserverStateAggregateExtensions
{
    /// <summary>
    /// Join with failed partitions.
    /// </summary>
    /// <param name="aggregation">The aggregation to join.</param>
    /// <returns>New aggregation now based on <see cref="ObserverInformation"/>.</returns>
    public static IAggregateFluent<ObserverInformation> JoinWithFailedPartitions(this IAggregateFluent<ObserverState> aggregation) => aggregation
        .Lookup(
            WellKnownCollectionNames.FailedPartitions,
            new ExpressionFieldDefinition<ObserverState>((ObserverState _) => _.ObserverId),
            new ExpressionFieldDefinition<FailedPartition>((FailedPartition _) => _.ObserverId),
            new ExpressionFieldDefinition<ObserverInformation>((ObserverInformation _) => _.FailedPartitions));
}
