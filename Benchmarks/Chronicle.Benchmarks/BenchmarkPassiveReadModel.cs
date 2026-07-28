// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents a passive model-bound read model that is never materialized into a sink.
/// </summary>
/// <param name="Id">The read model identifier, resolved from the event source identifier.</param>
/// <param name="Name">The benchmark event name.</param>
/// <param name="Value">The benchmark event value.</param>
/// <param name="Timestamp">The benchmark event timestamp.</param>
/// <remarks>
/// A passive projection never subscribes an observer, so resolving an instance by key projects the events of that
/// key on demand instead of reading a materialized document.
/// </remarks>
[Passive]
[FromEvent<BenchmarkInstanceRecorded>]
public record BenchmarkPassiveReadModel(Guid Id, string Name, int Value, DateTimeOffset Timestamp);
