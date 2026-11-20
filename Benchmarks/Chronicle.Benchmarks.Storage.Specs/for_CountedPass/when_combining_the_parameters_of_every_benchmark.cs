// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_CountedPass;

public class when_combining_the_parameters_of_every_benchmark : Specification
{
    Dictionary<Type, int> _counts = default!;
    Type[] _benchmarks = default!;

    void Establish() => StorageRunOptions.Current = StorageRunOptions.Parse(["--profile", "smoke", "--providers", "sqlite,postgres,mongo"]);

    void Because()
    {
        _benchmarks = [.. typeof(StorageBenchmark).Assembly.GetTypes().Where(_ => _.IsSubclassOf(typeof(StorageBenchmark)) && !_.IsAbstract)];
        _counts = _benchmarks.ToDictionary(_ => _, _ => CountedPass.Combinations(_).Count);
    }

    [Fact] void should_find_the_benchmarks() => _benchmarks.ShouldNotBeEmpty();
    [Fact] void should_produce_at_least_one_combination_per_provider_for_each() => _counts.Values.All(_ => _ >= 3).ShouldBeTrue();
}
