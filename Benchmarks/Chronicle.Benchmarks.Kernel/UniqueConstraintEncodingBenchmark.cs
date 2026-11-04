// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Compares the cost of computing a unique constraint value with the v1 encoding and the v2 keyed encoding.
/// </summary>
/// <remarks>
/// The v1 baseline starts from the extracted property values, which is what v1 holds after extraction.
/// The v2 encoder starts from the schema-typed components. Value extraction is not part of this benchmark.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[CategoriesColumn]
[Config(typeof(UniqueConstraintEncodingConfiguration))]
public class UniqueConstraintEncodingBenchmark
{
    static readonly byte[] _key = Encoding.ASCII.GetBytes("benchmark-key-0123456789abcdef!!");
    UniqueConstraintPropertyAndValue[] _v1Properties = [];
    UniqueConstraintComponent[] _v2Components = [];
    bool _ignoreCasing;

    /// <summary>
    /// Gets or sets the shape of the constraint under test.
    /// </summary>
    [Params("SingleString", "TwoStrings", "ThreeWithMiddleNull", "GuidAndInt64", "TwoStringsIgnoreCasing")]
    public string Shape { get; set; } = string.Empty;

    /// <summary>
    /// Builds the v1 and v2 inputs for the shape.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var guid = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");
        _ignoreCasing = Shape == "TwoStringsIgnoreCasing";
        (_v1Properties, _v2Components) = Shape switch
        {
            "SingleString" => (
                [new("email", "Alice@Example.com")],
                [new UniqueConstraintComponent("Alice@Example.com")]),
            "ThreeWithMiddleNull" => (
                [new("first", "Alice"), new("last", "Smith")],
                [new UniqueConstraintComponent("Alice"), new(null), new("Smith")]),
            "GuidAndInt64" => (
                [new("id", guid.ToString()), new("number", "1234567890123")],
                [new UniqueConstraintComponent(guid, "guid"), new(1234567890123L, "int64")]),
            _ => (
                [new("first", "Alice"), new("last", "Smith")],
                [new UniqueConstraintComponent("Alice"), new("Smith")])
        };
    }

    /// <summary>
    /// Computes the v1 value.
    /// </summary>
    /// <returns>The value.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public UniqueConstraintValue V1Value() => new(_v1Properties.GetValue(_ignoreCasing));

    /// <summary>
    /// Computes the v2 value.
    /// </summary>
    /// <returns>The value, or no claim.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public Option<UniqueConstraintValue> V2Value() => UniqueConstraintEncoder.Encode(_key, _v2Components, _ignoreCasing);

    sealed class UniqueConstraintEncodingConfiguration : ManualConfig
    {
        public UniqueConstraintEncodingConfiguration() => AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance));
    }
}

/// <summary>
/// Compares the cost of computing the scope of a unique constraint with the v1 scope key and the v2 scope token.
/// </summary>
/// <remarks>
/// This is a separate class from <see cref="UniqueConstraintEncodingBenchmark"/> so the scope cases do not repeat for every value shape.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(UniqueConstraintEncodingScopeConfiguration))]
public class UniqueConstraintEncodingBenchmarkScope
{
    static readonly byte[] _key = Encoding.ASCII.GetBytes("benchmark-key-0123456789abcdef!!");
    readonly ConstraintScope _unscoped = ConstraintScope.None;
    readonly ConstraintScope _streamIdScope = new(EventStreamId: "branch-1");
    readonly EventStreamId _streamId = "branch-1";
    readonly ResolvedConstraintScope _resolved = new(EventStreamId: "branch-1");

    /// <summary>
    /// Computes the v1 key for a constraint without scope.
    /// </summary>
    /// <returns>The key.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ScopeUnscoped")]
    public string V1ScopeUnscoped() => _unscoped.BuildScopeKey(null, null, null);

    /// <summary>
    /// Computes the v2 token for a constraint without scope.
    /// </summary>
    /// <returns>The token.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeUnscoped")]
    public UniqueConstraintScopeToken V2ScopeUnscoped() => UniqueConstraintScopeToken.Encode(_key, null);

    /// <summary>
    /// Computes the v1 key for a constraint scoped by event stream id.
    /// </summary>
    /// <returns>The key.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ScopeStreamId")]
    public string V1ScopeStreamId() => _streamIdScope.BuildScopeKey(null, null, _streamId);

    /// <summary>
    /// Computes the v2 token for a constraint scoped by event stream id.
    /// </summary>
    /// <returns>The token.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeStreamId")]
    public UniqueConstraintScopeToken V2ScopeStreamId() => UniqueConstraintScopeToken.Encode(_key, _resolved);

    sealed class UniqueConstraintEncodingScopeConfiguration : ManualConfig
    {
        public UniqueConstraintEncodingScopeConfiguration() => AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance));
    }
}
