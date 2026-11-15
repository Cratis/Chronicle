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
    readonly UniqueConstraintKeyedEncoder _encoder = new(_key);
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
        switch (Shape)
        {
            case "SingleString":
                _v1Properties = [new("email", "Alice@Example.com")];
                _v2Components = [new("Alice@Example.com")];
                break;
            case "ThreeWithMiddleNull":
                _v1Properties = [new("first", "Alice"), new("last", "Smith")];
                _v2Components = [new("Alice"), new(null), new("Smith")];
                break;
            case "GuidAndInt64":
                _v1Properties = [new("id", guid.ToString()), new("number", "1234567890123")];
                _v2Components = [new(guid, "guid"), new(1234567890123L, "int64")];
                break;
            default:
                _v1Properties = [new("first", "Alice"), new("last", "Smith")];
                _v2Components = [new("Alice"), new("Smith")];
                break;
        }
    }

    /// <summary>
    /// Computes the v1 value.
    /// </summary>
    /// <returns>The value.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public UniqueConstraintValue V1Value() => new(_v1Properties.GetValue(_ignoreCasing));

    /// <summary>
    /// Computes the value with the archived, unoptimised v2 encoder.
    /// </summary>
    /// <returns>The value, or no claim.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public Option<UniqueConstraintValue> ArchivedV2Value() => ArchivedUniqueConstraintEncoder.Encode(_key, _v2Components, _ignoreCasing);

    /// <summary>
    /// Computes the v2 value.
    /// </summary>
    /// <returns>The value, or no claim.</returns>
    [Benchmark]
    [BenchmarkCategory("Value")]
    public Option<UniqueConstraintValue> V2Value() => _encoder.EncodeValue(_v2Components, _ignoreCasing);

    sealed class UniqueConstraintEncodingConfiguration : ManualConfig
    {
        public UniqueConstraintEncodingConfiguration() => AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance));
    }
}
