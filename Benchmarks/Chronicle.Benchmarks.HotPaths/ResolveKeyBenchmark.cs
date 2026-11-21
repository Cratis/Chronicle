// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Compares rebuilding the key for an event without array indexers (before) with returning the key as is (after).
/// Run with --anyCategories ResolveKey --memory --job short --inProcess.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("ResolveKey")]
public class ResolveKeyBenchmark
{
    readonly Key _key = new("the-key", ArrayIndexers.NoIndexers);

    /// <summary>
    /// The old behavior, rebuilding the key and its indexers.
    /// </summary>
    /// <returns>The key.</returns>
    [Benchmark(Baseline = true)]
    public Key Rebuild() => _key with
    {
        ArrayIndexers = new ArrayIndexers(_key.ArrayIndexers.All.Select(_ => _))
    };

    /// <summary>
    /// The new behavior, returning the key when there are no indexers.
    /// </summary>
    /// <returns>The key.</returns>
    [Benchmark]
    public Key ReturnAsIs()
    {
        var key = _key;
        if (key.ArrayIndexers.IsEmpty)
        {
            return key;
        }

        return key with
        {
            ArrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Select(_ => _))
        };
    }
}
