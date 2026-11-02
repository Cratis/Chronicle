// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_AppendBenchmark;

public class when_selecting_artifacts : Specification
{
    IClientArtifactsProvider _artifacts = null!;

    void Because() => _artifacts = AppendBenchmark.ArtifactsProvider;

    [Fact] void should_register_no_reactors() => _artifacts.Reactors.ShouldBeEmpty();
    [Fact] void should_keep_the_existing_projection() => _artifacts.Projections.ShouldContain(typeof(BenchmarkProjection));
    [Fact] void should_keep_the_benchmark_events() => _artifacts.EventTypes.ShouldContain(typeof(BenchmarkEvent));
}
