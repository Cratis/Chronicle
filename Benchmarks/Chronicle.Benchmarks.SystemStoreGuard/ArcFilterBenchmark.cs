// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// Measures the Arc query and command filters for a non-System store, with the filter absent and present.
/// </summary>
[MemoryDiagnoser]
[MedianColumn]
[MinColumn]
[MaxColumn]
[InProcess]
[WarmupCount(3)]
[IterationCount(10)]
public class ArcFilterBenchmark : IDisposable
{
    static readonly Task<QueryResult> _noQueryVerdict = Task.FromResult<QueryResult>(null!);
    static readonly Task<CommandResult> _noCommandVerdict = Task.FromResult<CommandResult>(null!);
    readonly SystemStoreAuthorizationProbe _probe = new(false);
    readonly SystemStoreQueryFilter _queryFilter = new();
    readonly SystemStoreCommandFilter _commandFilter = new();
    readonly StoreEntitlementCommandFilter _entitlementFilter = new();
    EntitledCaller _entitled = null!;
    EntitledCaller _unentitledAudited = null!;
    EntitledCaller _unentitledEnforced = null!;
    CommandContext _entitledCommand = null!;
    CommandContext _auditedCommand = null!;
    CommandContext _refusedCommand = null!;
    QueryContext _query = null!;
    CommandContext _command = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the guard filter is present.
    /// </summary>
    [Params(false, true)]
    public bool Guarded { get; set; }

    /// <summary>
    /// Gets the number of roster calls made; it must stay zero for non-System callers.
    /// </summary>
    public int RosterCalls => _probe.Calls;

    /// <summary>
    /// Creates the contexts for a read model query and a command targeting a non-System store.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } })
            .AddSingleton<ISystemStoreAccess>(new SystemStoreAccessVerdict(new AuthorizationDecisions(_probe, Options.Create(new ChronicleOptions()), NullLogger<AuthorizationDecisions>.Instance)))
            .BuildServiceProvider();

        var arguments = new QueryArguments { ["eventStore"] = "bench", ["namespace"] = "Default" };
        _query = new QueryContext(
            "Cratis.Chronicle.Observation.ObserverInformation.AllObservers", Execution.CorrelationId.New(), Paging.NotPaged, Sorting.None, arguments, ServiceProvider: services);
        var retry = new RetryPartition("bench", "Default", "observer", "event-log", "partition");
        _command = new CommandContext(Execution.CorrelationId.New(), retry.GetType(), retry, [], new CommandContextValues(), ServiceProvider: services);
        _entitled = new(true, true);
        _unentitledAudited = new(false, false);
        _unentitledEnforced = new(false, true);
        _entitledCommand = CommandFor(_entitled, retry);
        _auditedCommand = CommandFor(_unentitledAudited, retry);
        _refusedCommand = CommandFor(_unentitledEnforced, retry);
    }

    /// <summary>
    /// An application-store command by a caller whose grant is in the snapshot, with enforcement on.
    /// </summary>
    /// <returns>The filter task.</returns>
    [Benchmark]
    public Task<CommandResult> EntitledAppend() => _entitlementFilter.OnExecution(_entitledCommand);

    /// <summary>
    /// An application-store command by a caller without a grant, with enforcement off (audited).
    /// </summary>
    /// <returns>The filter task.</returns>
    [Benchmark]
    public Task<CommandResult> AuditedUnentitledAppend() => _entitlementFilter.OnExecution(_auditedCommand);

    /// <summary>
    /// An application-store command by a caller without a grant, with enforcement on (refused).
    /// </summary>
    /// <returns>The filter task.</returns>
    [Benchmark]
    public Task<CommandResult> RefusedUnentitledAppend() => _entitlementFilter.OnExecution(_refusedCommand);

    /// <summary>
    /// A query through the filter chain.
    /// </summary>
    /// <returns>The filter task.</returns>
    [Benchmark]
    public Task<QueryResult> Query() => Guarded ? _queryFilter.OnPerform(_query) : _noQueryVerdict;

    /// <summary>
    /// A command through the filter chain.
    /// </summary>
    /// <returns>The filter task.</returns>
    [Benchmark]
    public Task<CommandResult> Command() => Guarded ? _commandFilter.OnExecution(_command) : _noCommandVerdict;

    /// <summary>
    /// Fails the run when a non-System call asked for the roster.
    /// </summary>
    /// <exception cref="InvalidOperationException">A roster call was made.</exception>
    [GlobalCleanup]
    public void Verify()
    {
        Dispose();
        if (_probe.Calls != 0) throw new InvalidOperationException($"Non-System calls made {_probe.Calls} roster calls.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the published caller snapshots.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _entitled?.Dispose();
        _unentitledAudited?.Dispose();
        _unentitledEnforced?.Dispose();
    }

    static CommandContext CommandFor(EntitledCaller caller, RetryPartition command) =>
        new(Execution.CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: caller.Services);
}
