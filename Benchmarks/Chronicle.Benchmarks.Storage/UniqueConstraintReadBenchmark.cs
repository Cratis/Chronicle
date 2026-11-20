// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// The storage commands one governed append costs for its unique-constraint claims. MongoDB prefetches the owners
/// of all claims in one batch (`IsAllowedMany`) and applies the changes as one batch; the SQL storage checks
/// and saves per claim. The gap between the two is what the benchmark reports (#26).
/// </summary>
[MemoryDiagnoser]
public class UniqueConstraintReadBenchmark : StorageBenchmark
{
    IUniqueConstraintsStorage _storage = null!;
    UniqueConstraintDefinition _definition = null!;
    long _next;

    /// <summary>Gets the claim counts of this run, for `ParamsSource`.</summary>
    public static IEnumerable<int> Claims => Profile.ConstraintBatchSizes;

    /// <summary>Gets or sets how many values one governed append claims.</summary>
    [ParamsSource(nameof(Claims))]
    public int ClaimCount { get; set; }

    /// <summary>Starts the host, creates the constraint storage and warms its tables outside measurement.</summary>
    /// <returns>The task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        await StartHost();
        (_storage, _definition) = await Host.CreateConstraints();
        await Claim(NextClaims(1, "warmup"));
    }

    /// <summary>Checks and then records <see cref="ClaimCount"/> fresh claims the way the kernel does for the provider.</summary>
    /// <returns>The number of claims recorded.</returns>
    [Benchmark]
    public Task<int> GovernedAppend() => Measure(Key("unique-constraint-claims", ClaimCount), count => count, async () =>
    {
        var claims = NextClaims(ClaimCount, "value");
        await Claim(claims.Claims, claims.First);
        return claims.Claims.Length;
    });

    (UniqueConstraintClaim[] Claims, long First) NextClaims(int count, string prefix)
    {
        var first = Interlocked.Add(ref _next, count) - count;
        var claims = Enumerable.Range(0, count)
            .Select(offset => new UniqueConstraintClaim(new EventSourceId($"source-{prefix}-{first + offset}"), new UniqueConstraintValue($"{prefix}-{first + offset}")))
            .ToArray();
        return (claims, first);
    }

    Task Claim((UniqueConstraintClaim[] Claims, long First) claims) => Claim(claims.Claims, claims.First);

    Task Claim(UniqueConstraintClaim[] claims, long first) =>
        _storage is IUniqueConstraintsBatchStorage batch ? ClaimInBatch(batch, claims, first) : ClaimPerClaim(claims, first);

    async Task ClaimInBatch(IUniqueConstraintsBatchStorage batch, UniqueConstraintClaim[] claims, long first)
    {
        var verdicts = await batch.IsAllowedMany(claims, _definition);
        if (verdicts.Any(_ => !_.IsAllowed)) throw new InvalidOperationException("A fresh claim was refused.");
        var changes = claims.Select((claim, index) => new UniqueConstraintIndexChange(UniqueConstraintIndexChangeKind.Save, claim.EventSourceId, (ulong)(first + index), string.Empty, claim.Value)).ToArray();
        var duplicates = await batch.ApplyChanges(_definition, changes);
        if (duplicates.Count > 0) throw new InvalidOperationException("A fresh claim was a duplicate.");
    }

    async Task ClaimPerClaim(UniqueConstraintClaim[] claims, long first)
    {
        foreach (var claim in claims)
        {
            var (allowed, _) = await _storage.IsAllowed(claim.EventSourceId, _definition, claim.Value);
            if (!allowed) throw new InvalidOperationException("A fresh claim was refused.");
        }

        for (var index = 0; index < claims.Length; index++)
        {
            await _storage.Save(claims[index].EventSourceId, _definition, (ulong)(first + index), claims[index].Value);
        }
    }
}
