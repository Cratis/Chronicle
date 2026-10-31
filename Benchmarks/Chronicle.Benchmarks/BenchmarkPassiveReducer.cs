// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Reduces <see cref="BenchmarkInstanceRecorded"/> events into a <see cref="BenchmarkPassiveReducedReadModel"/>.
/// </summary>
public class BenchmarkPassiveReducer : IReducerFor<BenchmarkPassiveReducedReadModel>
{
    /// <summary>
    /// Applies a <see cref="BenchmarkInstanceRecorded"/> event to the current read model state.
    /// </summary>
    /// <param name="evt">The event being reduced.</param>
    /// <param name="current">The current read model state.</param>
    /// <returns>The updated read model.</returns>
    public BenchmarkPassiveReducedReadModel OnInstanceRecorded(BenchmarkInstanceRecorded evt, BenchmarkPassiveReducedReadModel? current) =>
        new((current?.EventsReduced ?? 0) + 1, evt.Value);
}
