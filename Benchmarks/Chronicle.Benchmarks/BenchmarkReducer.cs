// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Chronicle.Benchmarks;

public record BenchmarkReadModel(int EventsProcessed);

public class BenchmarkReducer : IReducerFor<BenchmarkReadModel>
{
    public Task<BenchmarkReadModel?> OnBenchmarkEvent(BenchmarkEvent evt, BenchmarkReadModel? current, EventContext context)
    {
        var readModel = current ?? new BenchmarkReadModel(0);
        return Task.FromResult<BenchmarkReadModel?>(readModel with { EventsProcessed = readModel.EventsProcessed + 1 });
    }
}
