// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents the event payload driving the model-bound read models the benchmark suite resolves instances for.
/// </summary>
/// <param name="Name">The event name.</param>
/// <param name="Value">The benchmark value.</param>
/// <param name="Timestamp">The time the event was created.</param>
/// <remarks>
/// This is deliberately a separate event type from <see cref="BenchmarkEvent"/> so the read models it feeds never
/// take part in the benchmarks measuring the projection, reducer and reactor pipelines.
/// </remarks>
[EventType]
public record BenchmarkInstanceRecorded(string Name, int Value, DateTimeOffset Timestamp);
