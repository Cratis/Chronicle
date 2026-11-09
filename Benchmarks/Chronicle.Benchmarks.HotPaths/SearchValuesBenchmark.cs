// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Compares <c>IndexOfAny(char[])</c> with <see cref="SearchValues{T}"/> for locating a number's exponent, the search done by
/// the sequence content comparison. Run with --filter '*SearchValuesBenchmark*' --runtimes net10.0 net11.0 --memory.
/// </summary>
[MemoryDiagnoser]
public class SearchValuesBenchmark
{
    static readonly SearchValues<char> _exponent = SearchValues.Create(['e', 'E']);

    /// <summary>
    /// Gets or sets the number token searched.
    /// </summary>
    [Params("42", "123456.789012", "1.234567890123456789e-12")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Searches with a collection expression array.
    /// </summary>
    /// <returns>The index of the exponent marker, or -1.</returns>
    [Benchmark(Baseline = true)]
    public int IndexOfAnyArray() => Token.IndexOfAny(['e', 'E']);

    /// <summary>
    /// Searches with a cached <see cref="SearchValues{T}"/>.
    /// </summary>
    /// <returns>The index of the exponent marker, or -1.</returns>
    [Benchmark]
    public int IndexOfAnySearchValues() => Token.AsSpan().IndexOfAny(_exponent);
}
