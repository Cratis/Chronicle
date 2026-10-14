// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Represents a passive read model built by <see cref="BenchmarkPassiveReducer"/>.
/// </summary>
/// <param name="EventsReduced">The number of events folded into the instance.</param>
/// <param name="LastValue">The value carried by the last folded event.</param>
/// <remarks>
/// A passive reducer never subscribes an observer, so resolving an instance by key fetches every event of that key
/// and reduces them in the client on every read.
/// </remarks>
[Passive]
public record BenchmarkPassiveReducedReadModel(int EventsReduced, int LastValue);
