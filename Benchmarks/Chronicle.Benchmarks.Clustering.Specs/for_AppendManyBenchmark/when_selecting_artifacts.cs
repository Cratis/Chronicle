// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_AppendManyBenchmark;

public class when_selecting_artifacts : Specification
{
    IClientArtifactsProvider _artifacts = null!;

    void Because() => _artifacts = AppendManyBenchmark.ArtifactsProvider;

    [Fact] void should_register_only_the_pre_profile_reactor() => _artifacts.Reactors.ShouldContainOnly(typeof(BenchmarkReactor));
    [Fact] void should_register_only_the_pre_profile_projection() => _artifacts.Projections.ShouldContainOnly(typeof(BenchmarkProjection));
    [Fact] void should_register_only_the_pre_profile_model_bound_projections() => _artifacts.ModelBoundProjections.ShouldContainOnly(typeof(BenchmarkMaterializedReadModel), typeof(BenchmarkPassiveReadModel));
    [Fact] void should_register_only_the_pre_profile_reducers() => _artifacts.Reducers.ShouldContainOnly(typeof(BenchmarkReducer), typeof(BenchmarkPassiveReducer));
    [Fact] void should_register_no_read_model_reactors() => _artifacts.ReadModelReactors.ShouldBeEmpty();
    [Fact] void should_keep_the_benchmark_events() => _artifacts.EventTypes.ShouldContain(typeof(BenchmarkEvent));
}
