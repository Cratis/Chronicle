// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Compares the MongoDB sink's cache identity before #5427 (the BSON value's string form) against after it (SinkCacheKey.For).
/// </summary>
/// <remarks>
/// A pure in-process micro-benchmark; it needs neither Docker nor the Chronicle container.
/// </remarks>
[MemoryDiagnoser]
public class SinkCacheKeyBenchmark
{
    const int PrefetchKeyCount = 1000;

    BsonValue _value = BsonNull.Value;
    BsonValue[] _keys = [];

    /// <summary>
    /// Gets or sets the kind of key measured.
    /// </summary>
    [Params("String", "Int64", "Guid", "CompositeStrings", "Decimal", "CompositeDecimal")]
    public string KeyKind { get; set; } = "String";

    /// <summary>
    /// Builds the key and the set of keys for the prefetch grouping.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _value = CreateKey(KeyKind, 0);
        _keys = [.. Enumerable.Range(0, PrefetchKeyCount).Select(index => CreateKey(KeyKind, index))];
    }

    /// <summary>
    /// The cache identity before #5427.
    /// </summary>
    /// <returns>The identity.</returns>
    [Benchmark(Baseline = true)]
    public string Old() => _value.ToString()!;

    /// <summary>
    /// The cache identity after #5427.
    /// </summary>
    /// <returns>The identity.</returns>
    [Benchmark]
    public string New() => SinkCacheKey.For(_value);

    /// <summary>
    /// Groups the keys by BSON value, as the prefetch did before #5427.
    /// </summary>
    /// <returns>The number of groups.</returns>
    [Benchmark]
    public int PrefetchGroupOld() => _keys.GroupBy(key => key).Count();

    /// <summary>
    /// Groups the keys by SinkCacheKey.Identity. Mirrors the grouping in SinkPagePrefetch.Prefetch.
    /// </summary>
    /// <returns>The number of groups.</returns>
    [Benchmark]
    public int PrefetchGroupNew() => _keys.GroupBy(SinkCacheKey.Identity).Count();

    static BsonValue CreateKey(string kind, int index) => kind switch
    {
        "String" => new BsonString(new Guid(index, 0, 0, new byte[8]).ToString()),
        "Int64" => new BsonInt64(index),
        "Guid" => new Guid(index, 0, 0, new byte[8]).ToBsonValue(),
        "CompositeStrings" => new BsonDocument { { "first", $"tenant-{index}" }, { "second", $"entity-{index}" } },
        "Decimal" => new BsonDecimal128(Decimal128.Parse($"{index}.50")),
        "CompositeDecimal" => new BsonDocument { { "first", $"tenant-{index}" }, { "amount", new BsonDecimal128(Decimal128.Parse($"{index}.50")) } },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
