// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The sizes a workload is measured at.</summary>
/// <param name="EventCounts">The number of seeded events for the catch-up and activation workloads.</param>
/// <param name="Concurrency">The writer counts for the concurrent append workloads.</param>
/// <param name="BatchSizes">The batch sizes for `AppendMany`.</param>
/// <param name="ConstraintBatchSizes">The governed batch sizes for the uniqueness read workload.</param>
public sealed record WorkloadShape(IReadOnlyList<int> EventCounts, IReadOnlyList<int> Concurrency, IReadOnlyList<int> BatchSizes, IReadOnlyList<int> ConstraintBatchSizes);
