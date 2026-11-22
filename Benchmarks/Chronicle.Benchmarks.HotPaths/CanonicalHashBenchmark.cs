// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Compares Stage B against faithful pre-Stage-A and Stage-A hash pipelines.
/// </summary>
[MemoryDiagnoser]
public class CanonicalHashBenchmark
{
    readonly EventHashCalculator _calculator = new();
    readonly EventTypeId _eventType = new("canonical-hash-event");
    readonly EventSourceId _eventSource = new("canonical-hash-source");
    ExpandoObject[] _contents = [];

    /// <summary>
    /// Gets or sets the event shape.
    /// </summary>
    [Params("Small", "Medium", "Nested")]
    public string Shape { get; set; } = "Small";

    /// <summary>
    /// Gets or sets the number of content generations hashed for each append.
    /// </summary>
    [Params(1, 3)]
    public int Generations { get; set; }

    /// <summary>
    /// Creates the event content outside the measured operations and verifies the baseline agrees.
    /// </summary>
    /// <exception cref="InvalidOperationException">The pipelines produce different hashes.</exception>
    [GlobalSetup]
    public void Setup()
    {
        _contents = Enumerable.Range(1, Generations).Select(CreateContent).ToArray();
        foreach (var content in _contents)
        {
            var current = _calculator.Calculate(_eventType, _eventSource, content);
            if (LegacyCanonicalHash.Calculate(_eventType, _eventSource, content) != current)
            {
                throw new InvalidOperationException("Canonical hash differs from the legacy baseline.");
            }
            if (StageACanonicalHash.Calculate(_eventType, _eventSource, content) != current)
            {
                throw new InvalidOperationException("Canonical hash differs from the Stage-A baseline.");
            }
        }
    }

    /// <summary>
    /// Hashes all content generations with the unchanged legacy pipeline.
    /// </summary>
    /// <returns>The last hash, to consume the result.</returns>
    [Benchmark]
    public EventHash Legacy()
    {
        var hash = EventHash.NotSet;
        foreach (var content in _contents) hash = LegacyCanonicalHash.Calculate(_eventType, _eventSource, content);
        return hash;
    }

    /// <summary>
    /// Hashes the same content generations with the Stage-A pipeline.
    /// </summary>
    /// <returns>The last hash, to consume the result.</returns>
    [Benchmark(Baseline = true)]
    public EventHash StageA()
    {
        var hash = EventHash.NotSet;
        foreach (var content in _contents) hash = StageACanonicalHash.Calculate(_eventType, _eventSource, content);
        return hash;
    }

    /// <summary>
    /// Hashes the same content generations with the direct Stage-B writer.
    /// </summary>
    /// <returns>The last hash, to consume the result.</returns>
    [Benchmark]
    public EventHash StageB()
    {
        var hash = EventHash.NotSet;
        foreach (var content in _contents) hash = _calculator.Calculate(_eventType, _eventSource, content);
        return hash;
    }

    static ExpandoObject Object(params (string Name, object? Value)[] properties)
    {
        var content = new ExpandoObject();
        var values = (IDictionary<string, object?>)content;
        foreach (var (name, value) in properties) values[name] = value;
        return content;
    }

    ExpandoObject CreateContent(int generation) => Shape switch
    {
        "Small" => Object(("name", "Jane"), ("generation", generation), ("active", true)),
        "Medium" => Object(Enumerable.Range(0, 24).Reverse().Select(index => ($"property{index:00}", (object?)$"value {index} <>& é 日本語")).Append(("generation", generation)).ToArray()),
        _ => Object(("z", Object(("items", new[] { Object(("z", 4), ("a", 3)), Object(("z", 2), ("a", 1)) }), ("empty", Array.Empty<object>()))), ("a", Object(("price", 123.4500m), ("generation", generation))))
    };
}
