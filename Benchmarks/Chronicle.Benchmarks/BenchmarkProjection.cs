// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks;

public record BenchmarkProjectionModel(string Id, string Name, int Value, DateTimeOffset Timestamp);

public class BenchmarkProjection : IProjectionFor<BenchmarkProjectionModel>
{
    public void Define(IProjectionBuilderFor<BenchmarkProjectionModel> builder)
    {
        builder
            .From<BenchmarkEvent>(e => e
                .UsingKey(evt => evt.Name)
                .Set(m => m.Name).To(evt => evt.Name)
                .Set(m => m.Value).To(evt => evt.Value)
                .Set(m => m.Timestamp).To(evt => evt.Timestamp));
    }
}
