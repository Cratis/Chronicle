// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents an order placed by a customer, the own event of <see cref="BenchmarkJoinProjection"/>.
/// </summary>
/// <param name="CustomerId">The customer the order is for, which the projection joins on.</param>
/// <param name="Quantity">The quantity ordered.</param>
[EventType("5524a1c0-6f1d-4d0b-9a57-0c6a5524a001")]
public record BenchmarkJoinOrderPlaced(string CustomerId, int Quantity);
