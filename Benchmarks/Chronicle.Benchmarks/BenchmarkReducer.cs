// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Reduces benchmark events into a simple counter read model.
/// </summary>
public class BenchmarkReducer : IReducerFor<BenchmarkReadModel>
{
    /// <summary>
    /// Applies a <see cref="BenchmarkEvent"/> to the current read model state.
    /// </summary>
    /// <param name="evt">The event being reduced.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated read model.</returns>
    public Task<BenchmarkReadModel?> OnBenchmarkEvent(BenchmarkEvent evt, BenchmarkReadModel? current, EventContext context)
    {
        _ = evt;
        _ = context;

        var readModel = current ?? new BenchmarkReadModel(0);
        return Task.FromResult<BenchmarkReadModel?>(readModel with { EventsProcessed = readModel.EventsProcessed + 1 });
    }
}
