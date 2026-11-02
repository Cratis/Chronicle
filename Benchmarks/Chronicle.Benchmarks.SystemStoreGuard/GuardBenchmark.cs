// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// Measures the routing primitive and the verdict for non-System, System administrator and System non-administrator callers.
/// </summary>
[MemoryDiagnoser]
[MedianColumn]
[MinColumn]
[MaxColumn]
[InProcess]
[WarmupCount(3)]
[IterationCount(10)]
public class GuardBenchmark
{
    readonly SystemStoreAuthorizationProbe _admin = new(true);
    readonly SystemStoreAuthorizationProbe _nonAdmin = new(false);
    readonly ClaimsPrincipal _caller = new(new ClaimsIdentity([new Claim("sub", "caller")], "bench"));
    readonly string _store = "bench";
    readonly string _namespace = "Default";
    readonly string _sequence = "event-log";
    SystemStoreAccessVerdict _adminAccess = null!;
    SystemStoreAccessVerdict _nonAdminAccess = null!;

    /// <summary>
    /// Gets the number of roster calls made for non-System callers; it must stay zero.
    /// </summary>
    public int NonSystemRosterCalls => _admin.Calls + _nonAdmin.Calls;

    /// <summary>
    /// Creates the access primitives.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _adminAccess = new(_admin);
        _nonAdminAccess = new(_nonAdmin);
    }

    /// <summary>
    /// No guard: the cost of reading the same values.
    /// </summary>
    /// <returns>The combined length.</returns>
    [Benchmark(Baseline = true)]
    public int Unguarded() => _store.Length + _namespace.Length + _sequence.Length;

    /// <summary>
    /// The guard for a non-System call: the check and the separator scan.
    /// </summary>
    /// <returns>The violation index.</returns>
    [Benchmark]
    public int NonSystem() => SystemStoreAccess.IsSystem(_store) ? -2 : SystemStoreAccess.FirstSeparatorViolation(_store, _namespace, _sequence);

    /// <summary>
    /// Fails the run when a non-System call asked for the roster.
    /// </summary>
    /// <exception cref="InvalidOperationException">A roster call was made.</exception>
    [GlobalCleanup]
    public void Verify()
    {
        // SystemAdmin and SystemNonAdmin legitimately call the roster; the non-System path must not.
        _admin.Reset();
        _nonAdmin.Reset();
        _ = SystemStoreAccess.IsSystem(_store) ? _adminAccess.IsAllowed(_caller) : Task.CompletedTask;
        if (NonSystemRosterCalls != 0) throw new InvalidOperationException("A non-System call asked for the roster.");
    }

    /// <summary>
    /// The verdict for an administrator addressing System.
    /// </summary>
    /// <returns>The verdict.</returns>
    [Benchmark]
    public Task<bool> SystemAdmin() => SystemStoreAccess.IsSystem("System") ? _adminAccess.IsAllowed(_caller) : Task.FromResult(true);

    /// <summary>
    /// The verdict for a non-administrator addressing System.
    /// </summary>
    /// <returns>The verdict.</returns>
    [Benchmark]
    public Task<bool> SystemNonAdmin() => SystemStoreAccess.IsSystem("SYSTEM") ? _nonAdminAccess.IsAllowed(_caller) : Task.FromResult(true);
}
