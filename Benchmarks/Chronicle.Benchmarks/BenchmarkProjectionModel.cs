// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents the projected benchmark event state.
/// </summary>
/// <param name="Id">The projection identifier.</param>
/// <param name="Name">The benchmark event name.</param>
/// <param name="Value">The benchmark event value.</param>
/// <param name="Timestamp">The benchmark event timestamp.</param>
public record BenchmarkProjectionModel(string Id, string Name, int Value, DateTimeOffset Timestamp);