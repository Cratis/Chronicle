// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints.V2;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Compares the cost of computing the scope of a unique constraint with the v1 scope key and the v2 scope token.
/// </summary>
/// <remarks>
/// The "ScopeStreamId" cases repeat one scope, which the keyed encoder serves from its cache. The "ScopeStreamIdMiss" cases use a different scope on every call.
/// This is a separate class from <see cref="UniqueConstraintEncodingBenchmark"/> so the scope cases do not repeat for every value shape.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(UniqueConstraintEncodingScopeConfiguration))]
public class UniqueConstraintEncodingBenchmarkScope
{
    /// <summary>
    /// The number of distinct scopes, which is more than the keyed encoder caches, so the cycling miss cases never hit its cache.
    /// </summary>
    const int MissCount = 1 << 16;
    static readonly byte[] _key = Encoding.ASCII.GetBytes("benchmark-key-0123456789abcdef!!");
    readonly UniqueConstraintKeyedEncoder _encoder = new(_key);
    readonly ConstraintScope _unscoped = ConstraintScope.None;
    readonly ConstraintScope _streamIdScope = new(EventStreamId: "branch-1");
    readonly EventStreamId _streamId = "branch-1";
    readonly ResolvedConstraintScope _resolved = new(EventStreamId: "branch-1");
    readonly EventStreamId[] _missIds = Enumerable.Range(0, MissCount).Select(index => new EventStreamId($"stream-{index}")).ToArray();
    readonly ResolvedConstraintScope[] _missScopes = Enumerable.Range(0, MissCount).Select(index => new ResolvedConstraintScope(EventStreamId: $"stream-{index}")).ToArray();
    int _miss;

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

    /// <summary>
    /// Computes the v1 key for a different event stream id on every call.
    /// </summary>
    /// <returns>The key.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ScopeStreamIdMiss")]
    public string V1ScopeStreamIdMiss() => _streamIdScope.BuildScopeKey(null, null, _missIds[NextMiss()]);

    /// <summary>
    /// Computes the token with the archived, unoptimised v2 encoder for a different event stream id on every call.
    /// </summary>
    /// <returns>The token text.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeStreamIdMiss")]
    public string ArchivedV2ScopeStreamIdMiss() => ArchivedUniqueConstraintEncoder.EncodeScope(_key, _missScopes[NextMiss()]).Value;

    /// <summary>
    /// Computes the v2 token for a different event stream id on every call, so the scope cache never hits.
    /// </summary>
    /// <returns>The token text.</returns>
    [Benchmark]
    [BenchmarkCategory("ScopeStreamIdMiss")]
    public string V2ScopeStreamIdMiss() => _encoder.EncodeScope(_missScopes[NextMiss()]).Value;

    int NextMiss() => _miss = (_miss + 1) & (MissCount - 1);

    sealed class UniqueConstraintEncodingScopeConfiguration : ManualConfig
    {
        public UniqueConstraintEncodingScopeConfiguration() => AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance));
    }
}
