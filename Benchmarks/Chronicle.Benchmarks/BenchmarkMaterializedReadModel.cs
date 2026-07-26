// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents a materialized model-bound read model built by an actively observing projection.
/// </summary>
/// <param name="Id">The read model identifier, resolved from the event source identifier.</param>
/// <param name="Name">The benchmark event name.</param>
/// <param name="Value">The benchmark event value.</param>
/// <param name="Timestamp">The benchmark event timestamp.</param>
[FromEvent<BenchmarkInstanceRecorded>]
public record BenchmarkMaterializedReadModel(Guid Id, string Name, int Value, DateTimeOffset Timestamp);
