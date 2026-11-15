// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Compares copying a value into read model state through the clone holder (before) with the direct list copy (after).
/// Run with --anyCategories StateValues --memory --job short --inProcess.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("StateValues")]
public class StateValuesCopyBenchmark
{
    object _value = null!;

    /// <summary>
    /// Gets or sets the shape of the value that is copied.
    /// </summary>
    [Params("Integer", "String", "ListOf5Strings", "ListOf20Expandos", "NestedExpando")]
    public string Shape { get; set; } = string.Empty;

    /// <summary>
    /// Builds the value to copy.
    /// </summary>
    [GlobalSetup]
    public void Setup() => _value = Shape switch
    {
        "Integer" => 42,
        "String" => "a string",
        "ListOf5Strings" => new List<object> { "a", "b", "c", "d", "e" },
        "ListOf20Expandos" => Enumerable.Range(0, 20).Select(i => (object)Expando(i)).ToList(),
        _ => Nested()
    };

    /// <summary>
    /// Copies the value the way it was done before: through a holder and the clone.
    /// </summary>
    /// <returns>The copy.</returns>
    [Benchmark(Baseline = true)]
    public object? Before() => _value switch
    {
        null => null,
        string => _value,
        ExpandoObject expandoObject => expandoObject.Clone(),
        System.Collections.IEnumerable => CopyThroughHolder(_value),
        _ => _value
    };

    /// <summary>
    /// Copies the value with <see cref="StateValues.Copy"/>.
    /// </summary>
    /// <returns>The copy.</returns>
    [Benchmark]
    public object? After() => StateValues.Copy(_value);

    static object? CopyThroughHolder(object value)
    {
        var holder = new ExpandoObject();
        ((IDictionary<string, object?>)holder)["value"] = value;
        return ((IDictionary<string, object?>)holder.Clone())["value"];
    }

    static ExpandoObject Expando(int i)
    {
        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)expando;
        dictionary["id"] = i;
        dictionary["name"] = $"item-{i}";
        dictionary["active"] = true;
        return expando;
    }

    static ExpandoObject Nested()
    {
        var expando = Expando(1);
        var dictionary = (IDictionary<string, object?>)expando;
        dictionary["child"] = Expando(2);
        dictionary["tags"] = new List<object> { "x", "y" };
        return expando;
    }
}
