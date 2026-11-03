// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents a customer being renamed, the joined event of <see cref="BenchmarkJoinProjection"/>.
/// </summary>
/// <param name="Name">The new name of the customer.</param>
[EventType("5524a1c0-6f1d-4d0b-9a57-0c6a5524a002")]
public record BenchmarkJoinCustomerRenamed(string Name);
