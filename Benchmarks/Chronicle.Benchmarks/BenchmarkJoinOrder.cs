// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents the read model of <see cref="BenchmarkJoinProjection"/>, keyed by the order's event source id.
/// </summary>
/// <param name="Id">The order id.</param>
/// <param name="CustomerId">The customer the order is for.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="Name">The name of the customer, joined from the customer's events.</param>
public record BenchmarkJoinOrder(string Id, string CustomerId, int Quantity, string Name);
