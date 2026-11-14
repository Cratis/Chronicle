// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Projects orders keyed by the event source id with a root join on the customer, which marks the projection for
/// applied event markers on MongoDB.
/// </summary>
public class BenchmarkJoinProjection : IProjectionFor<BenchmarkJoinOrder>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<BenchmarkJoinOrder> builder) => builder
        .From<BenchmarkJoinOrderPlaced>()
        .Join<BenchmarkJoinCustomerRenamed>(j => j.On(m => m.CustomerId));
}
