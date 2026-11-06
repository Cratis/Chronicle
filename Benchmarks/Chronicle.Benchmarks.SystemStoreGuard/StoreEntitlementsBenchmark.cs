// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>Measures ordinary application reads against a per-silo snapshot.</summary>
[MemoryDiagnoser]
[InProcess]
public class StoreEntitlementsBenchmark : IDisposable
{
    readonly PrincipalKey _holder = new(PrincipalKey.LocalIssuer, "app");
    readonly ClaimsPrincipal _caller = new(new ClaimsIdentity([new Claim("sub", "app")], "Bearer"));
    readonly AuthorizationScope _scope = AuthorizationScope.ForEventStore("bench");
    readonly SystemStoreAuthorizationProbe _administrator = new(false);
    readonly IGrainFactory _grains = Substitute.For<IGrainFactory>();
    StoreEntitlementsCache _cache = null!;
    AuthorizationDecisions _decisions = null!;

    /// <summary>Gets or sets the number of other explicitly enrolled principals.</summary>
    [Params(8, 64, 512)]
    public int Principals { get; set; }

    /// <summary>Builds and publishes the snapshot before measurement.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var grants = Enumerable.Range(0, Principals).Select(index => new EventStoreAccessGrant(new(PrincipalKey.LocalIssuer, $"other-{index}"), "bench", null, StoreAccess.Read, EventStoreAccessGrantSource.Command)).ToList();
        grants.Add(new(_holder, "bench", null, StoreAccess.Read, EventStoreAccessGrantSource.Command));
        _cache = new(_grains, NullLogger<StoreEntitlementsCache>.Instance, TimeProvider.System);
        _cache.Publish(new(1, grants));
        _decisions = new(_administrator, Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = true, EnforceStoreEntitlements = true } }), NullLogger<AuthorizationDecisions>.Instance, _cache);
    }

    /// <summary>Looks up a grant without allocations or remote calls.</summary>
    /// <returns>Whether the grant covers the read.</returns>
    [Benchmark(Baseline = true)]
    public bool SnapshotRead() => _cache.Snapshot.Allows(_holder, _scope.EventStore!, null, StoreAccess.Read);

    /// <summary>Decides an application read using only local state.</summary>
    /// <returns>The synchronously completed decision.</returns>
    [Benchmark]
    public Task<AuthorizationDecision> ApplicationRead() => _decisions.Decide(_caller, WellKnownCapabilities.ReadEventStore, _scope);

    /// <summary>Verifies that neither authoritative dependency was consulted.</summary>
    /// <exception cref="InvalidOperationException">An authoritative call was made.</exception>
    [GlobalCleanup]
    public void Verify()
    {
        Dispose();
        if (_administrator.Calls != 0 || _grains.ReceivedCalls().Any()) throw new InvalidOperationException("An ordinary application read consulted an authoritative dependency.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the published snapshot.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _cache?.Dispose();
    }
}
