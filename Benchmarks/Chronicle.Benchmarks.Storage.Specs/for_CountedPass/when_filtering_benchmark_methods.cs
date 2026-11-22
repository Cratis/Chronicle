// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_CountedPass;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_filtering_benchmark_methods : Specification
{
    long _selected;
    long _excluded;
    bool _cutShort;

    void Establish() => Measurements.Clear();

    async Task Because()
    {
        _cutShort = await CountedPass.Run([typeof(FilterCase)], 2, "*FilterCase.Concurrent*");
        _selected = Measurements.For("selected").Operations;
        _excluded = Measurements.For("excluded").Operations;
    }

    void Destroy() => Measurements.Clear();

    [Fact] void should_run_every_selected_iteration() => _selected.ShouldEqual(2L);
    [Fact] void should_not_run_unmatched_methods() => _excluded.ShouldEqual(0L);
    [Fact] void should_complete_the_filtered_pass() => _cutShort.ShouldBeFalse();

    public class FilterCase
    {
        [Benchmark]
        public void Concurrent() => Measurements.Record("selected", new(1, 1, new(0, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(1)], 0));

        [Benchmark]
        public void Other() => Measurements.Record("excluded", new(1, 1, new(0, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(1)], 0));
    }
}
