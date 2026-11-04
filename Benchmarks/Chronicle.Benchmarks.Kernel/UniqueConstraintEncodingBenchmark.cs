// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Concepts.Events;
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
    readonly UniqueConstraintKeyedEncoder _encoder = new(_key);
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
    /// Computes the token for a constraint without scope with the archived, unoptimised v2 encoder.
    /// </summary>
    /// <returns>The token text.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeUnscoped")]
    public string ArchivedV2ScopeUnscoped() => ArchivedUniqueConstraintEncoder.EncodeScope(_key, null).Value;

    /// <summary>
    /// Computes the v2 token for a constraint without scope.
    /// </summary>
    /// <returns>The token text.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeUnscoped")]
    public string V2ScopeUnscoped() => _encoder.EncodeScope(null).Value;

    /// <summary>
    /// Computes the v1 key for a constraint scoped by event stream id.
    /// </summary>
    /// <returns>The key.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ScopeStreamId")]
    public string V1ScopeStreamId() => _streamIdScope.BuildScopeKey(null, null, _streamId);

    /// <summary>
    /// Computes the token for a constraint scoped by event stream id with the archived, unoptimised v2 encoder.
    /// </summary>
    /// <returns>The token text.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeStreamId")]
    public string ArchivedV2ScopeStreamId() => ArchivedUniqueConstraintEncoder.EncodeScope(_key, _resolved).Value;

    /// <summary>
    /// Computes the v2 token for a constraint scoped by event stream id.
    /// </summary>
    /// <returns>The token text.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeStreamId")]
    public string V2ScopeStreamId() => _encoder.EncodeScope(_resolved).Value;

    sealed class UniqueConstraintEncodingScopeConfiguration : ManualConfig
    {
        public UniqueConstraintEncodingScopeConfiguration() => AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance));
    }
}

/// <summary>
/// The first, unoptimised v2 encoder, kept as the "archived v2" column of the benchmark.
/// It builds a <see cref="MemoryStream"/> preimage, allocates a byte array per component and hashes through hex strings.
/// </summary>
static class ArchivedUniqueConstraintEncoder
{
    public static Option<UniqueConstraintValue> Encode(ReadOnlySpan<byte> key, IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing)
    {
        var preimage = GetPreimage(components, ignoreCasing);
        if (components.All(_ => _.Value is null))
        {
            return Option<UniqueConstraintValue>.None();
        }
        var hash = HMACSHA256.HashData(key, preimage);
        return new UniqueConstraintValue(Convert.ToHexString(hash).ToLowerInvariant());
    }

    public static UniqueConstraintScopeToken EncodeScope(ReadOnlySpan<byte> key, ResolvedConstraintScope? scope)
    {
        using var target = new MemoryStream();
        target.Write("chronicle/scope/v2\0"u8);
        WriteText(target, scope?.EventSourceType?.Value);
        WriteText(target, scope?.EventStreamType?.Value);
        WriteText(target, scope?.EventStreamId?.Value);
        var hash = HMACSHA256.HashData(key, target.ToArray());
        return new(Convert.ToHexString(hash).ToLowerInvariant()[..32]);
    }

    static byte[] GetPreimage(IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing)
    {
        using var target = new MemoryStream();
        target.Write("chronicle/unique/v2\0"u8);
        WriteCount(target, components.Count);
        foreach (var component in components)
        {
            WriteComponent(target, component, ignoreCasing);
        }
        return target.ToArray();
    }

    static void WriteCount(Stream target, int count)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes, checked((uint)count));
        target.Write(bytes);
    }

    static void WriteText(Stream target, string? text)
    {
        if (text is null)
        {
            target.WriteByte(0x00);
            return;
        }
        var bytes = Encoding.UTF8.GetBytes(text);
        target.WriteByte(0x01);
        WriteCount(target, bytes.Length);
        target.Write(bytes);
    }

    static void WriteComponent(Stream target, UniqueConstraintComponent component, bool ignoreCasing)
    {
        UniqueConstraintCanonicalText.ValidateFormat(component.Format);
        if (component.Value is IEnumerable collection and not string)
        {
            var elements = collection.Cast<object?>().ToArray();
            target.WriteByte(0x02);
            WriteCount(target, elements.Length);
            foreach (var element in elements)
            {
                WriteScalar(target, element, component, ignoreCasing);
            }
            return;
        }
        WriteScalar(target, component.Value, component, ignoreCasing);
    }

    static void WriteScalar(Stream target, object? value, UniqueConstraintComponent component, bool ignoreCasing)
    {
        var text = value is null ? null : UniqueConstraintCanonicalText.For(value, component.Format, component.IsInSchema, ignoreCasing);
        WriteText(target, text);
    }
}
