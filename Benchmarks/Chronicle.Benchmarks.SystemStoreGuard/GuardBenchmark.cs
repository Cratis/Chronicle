// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// Measures the routing primitive and the verdict for non-System, System administrator and System non-administrator callers.
/// </summary>
[MemoryDiagnoser]
[MedianColumn]
[MinColumn]
[MaxColumn]
[Config(typeof(ColdPathConfig))]
public class GuardBenchmark
{
    readonly SystemStoreAuthorizationProbe _admin = new(true);
    readonly SystemStoreAuthorizationProbe _nonAdmin = new(false);
    readonly string _store = "bench";
    readonly string _namespace = "Default";
    readonly string _sequence = "event-log";
    HttpContext[] _requests = [];
    int _next;
    SystemStoreAccessVerdict _adminAccess = null!;
    SystemStoreAccessVerdict _nonAdminAccess = null!;

    /// <summary>
    /// Gets the number of roster calls made for non-System callers; it must stay zero.
    /// </summary>
    public int NonSystemRosterCalls => _admin.Calls + _nonAdmin.Calls;

    /// <summary>
    /// Creates the access primitives.
    /// </summary>
    /// <returns>A task that completes when the verdicts have been verified.</returns>
    /// <exception cref="InvalidOperationException">The configured callers received unexpected verdicts.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _adminAccess = new(Decisions(_admin));
        _nonAdminAccess = new(Decisions(_nonAdmin));
        var caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "bench"));
        if (!await _adminAccess.IsAllowed(caller) || await _nonAdminAccess.IsAllowed(caller))
        {
            throw new InvalidOperationException("The configured callers received unexpected verdicts.");
        }
        _admin.Reset();
        _nonAdmin.Reset();
    }

    /// <summary>Creates fresh callers outside the measured operation.</summary>
    [IterationSetup]
    public void PrepareRequests()
    {
        _next = 0;
        _requests = new HttpContext[EntitledCaller.RequestCount];
        for (var index = 0; index < _requests.Length; index++)
        {
            _requests[index] = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "bench")) };
        }
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
        _ = SystemStoreAccess.IsSystem(_store) ? _adminAccess.IsAllowed(_requests[0].User) : Task.CompletedTask;
        if (NonSystemRosterCalls != 0) throw new InvalidOperationException("A non-System call asked for the roster.");
    }

    /// <summary>
    /// The verdict for an administrator addressing System.
    /// </summary>
    /// <returns>The verdict.</returns>
    [Benchmark]
    public Task<bool> SystemAdmin() => SystemStoreAccess.IsSystem("System") ? _adminAccess.IsAllowed(_requests[_next++].User) : Task.FromResult(true);

    /// <summary>
    /// The verdict for a non-administrator addressing System.
    /// </summary>
    /// <returns>The verdict.</returns>
    [Benchmark]
    public Task<bool> SystemNonAdmin() => SystemStoreAccess.IsSystem("SYSTEM") ? _nonAdminAccess.IsAllowed(_requests[_next++].User) : Task.FromResult(true);

    static AuthorizationDecisions Decisions(SystemStoreAuthorizationProbe probe)
    {
        // The administrator check is resolved from the caller's request services, as in the server.
        var services = new ServiceCollection().AddSingleton<IAdministratorAuthorization>(probe).BuildServiceProvider();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { RequestServices = services });
        return new(Options.Create(new ChronicleOptions()), NullLogger<AuthorizationDecisions>.Instance, httpContextAccessor: accessor);
    }
}
