// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AppendBenchmark;

public class when_enumerating_cases_for_every_profile : Specification
{
    readonly Dictionary<string, string[]> _expected = [];
    readonly Dictionary<string, string[]> _timed = [];
    readonly Dictionary<string, string[]> _counted = [];
    BenchmarkProfile[] _profiles = default!;
    Type[] _benchmarks = default!;

    void Establish()
    {
        _profiles = [.. typeof(BenchmarkProfile).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(_ => _.PropertyType == typeof(BenchmarkProfile)).Select(_ => (BenchmarkProfile)_.GetValue(null)!)];
        _benchmarks = [.. typeof(AppendBenchmark).Assembly.GetTypes()
            .Where(_ => typeof(AppendBenchmark).IsAssignableFrom(_) && !_.IsAbstract)];
    }

    void Because()
    {
        var previous = StorageRunOptions.Current;
        try
        {
            foreach (var profile in _profiles)
            {
                StorageRunOptions.Current = StorageRunOptions.Parse(["--profile", profile.Name, "--providers", "sqlite,postgres,sqlserver,mongo"]);
                _expected[profile.Name] = ExpectedCases(profile);
                _timed[profile.Name] = [.. _benchmarks.SelectMany(type => BenchmarkConverter.TypeToBenchmarks(type, DefaultConfig.Instance).BenchmarksCases)
                    .Select(@case => Key(@case.Descriptor.Type, @case.Descriptor.WorkloadMethod.Name,
                        @case.Parameters.Items.Select(parameter => (parameter.Name, parameter.Value))))];
                _counted[profile.Name] = [.. _benchmarks.SelectMany(CountedCases)];
            }
        }
        finally
        {
            StorageRunOptions.Current = previous;
        }
    }

    [Fact] void should_discover_profiles() => _profiles.ShouldNotBeEmpty();
    [Fact] void should_discover_both_append_benchmarks() => _benchmarks.Length.ShouldEqual(2);
    [Fact] void should_generate_only_meaningful_timed_cases() => VerifyCases(_timed);
    [Fact] void should_count_exactly_the_same_reduced_cases() => VerifyCases(_counted);
    [Fact] void should_generate_twenty_eight_smoke_cases() => _timed["smoke"].Length.ShouldEqual(28);
    [Fact] void should_generate_thirty_six_short_cases() => _timed["short"].Length.ShouldEqual(36);
    [Fact] void should_generate_thirty_six_medium_cases() => _timed["medium"].Length.ShouldEqual(36);

    static string[] ExpectedCases(BenchmarkProfile profile) =>
        [.. Enum.GetValues<StorageProvider>().SelectMany(provider =>
            profile.BatchSizes.Select(size => Key(typeof(AppendBatchBenchmark), nameof(AppendBatchBenchmark.AppendBatch), [("Provider", provider), ("BatchSize", size)]))
                .Concat(profile.Concurrency.SelectMany(writers => new[]
                {
                    Key(typeof(ConcurrentAppendBenchmark), nameof(ConcurrentAppendBenchmark.ConcurrentOneSequence), [("Provider", provider), ("WriterCount", writers)]),
                    Key(typeof(ConcurrentAppendBenchmark), nameof(ConcurrentAppendBenchmark.ConcurrentManySequences), [("Provider", provider), ("WriterCount", writers)])
                })))];

    static IEnumerable<string> CountedCases(Type type) =>
        CountedPass.Combinations(type).SelectMany(combination => type.GetMethods()
            .Where(method => Attribute.IsDefined(method, typeof(BenchmarkAttribute)))
            .Select(method => Key(type, method.Name, combination.Select(parameter => (parameter.Property.Name, parameter.Value)))));

    static string Key(Type type, string method, IEnumerable<(string Name, object Value)> parameters) =>
        $"{type.Name}.{method}/{string.Join('/', parameters.OrderBy(_ => _.Name, StringComparer.Ordinal).Select(_ => $"{_.Name}={_.Value}"))}";

    void VerifyCases(Dictionary<string, string[]> actual)
    {
        foreach (var profile in _profiles)
        {
            string.Join('\n', actual[profile.Name].Order(StringComparer.Ordinal))
                .ShouldEqual(string.Join('\n', _expected[profile.Name].Order(StringComparer.Ordinal)));
        }
    }
}
