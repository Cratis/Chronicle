// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents the reducer state used for benchmark event counting.
/// </summary>
/// <param name="EventsProcessed">The number of events processed so far.</param>
public record BenchmarkReadModel(int EventsProcessed);