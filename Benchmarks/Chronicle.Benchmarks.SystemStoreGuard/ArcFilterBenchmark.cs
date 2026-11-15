// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Security;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>Measures Arc filters including request scope creation and first service resolution.</summary>
[MemoryDiagnoser]
[Config(typeof(ColdPathConfig))]
public class ArcFilterBenchmark : IDisposable
{
    static readonly CommandResult _noCommandResult = null!;
    static readonly QueryResult _noQueryResult = null!;
    readonly QueryArguments _arguments = new() { ["eventStore"] = "bench", ["namespace"] = "Default" };
    readonly Sequences.AppendMany _append = new("bench", "Default", "event-log", "source", []);
    readonly RetryPartition _retry = new("bench", "Default", "observer", "event-log", "partition");
    EntitledCaller _entitled = null!;
    EntitledCaller _unentitledAudited = null!;
    EntitledCaller _unentitledEnforced = null!;

    /// <summary>Gets or sets whether the authorization filter is present.</summary>
    [Params(false, true)]
    public bool Guarded { get; set; }

    /// <summary>Gets the number of roster calls for the measured ordinary-store paths.</summary>
    public int RosterCalls => _entitled.RosterCalls + _unentitledAudited.RosterCalls + _unentitledEnforced.RosterCalls;

    /// <summary>Builds the production authorization registrations and grants outside measurement.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _entitled = new(true, true);
        _unentitledAudited = new(false, false);
        _unentitledEnforced = new(false, true);
    }

    /// <summary>Prepares fresh requests outside the measured iteration.</summary>
    [IterationSetup]
    public void PrepareRequests()
    {
        _entitled.PrepareRequests();
        _unentitledAudited.PrepareRequests();
        _unentitledEnforced.PrepareRequests();
    }

    /// <summary>Measures a scoped ordinary query filter invocation.</summary>
    /// <returns>The filter result.</returns>
    [Benchmark]
    public async Task<QueryResult> Query()
    {
        await using var scope = _entitled.Services.CreateAsyncScope();
        _entitled.NextRequest(scope.ServiceProvider);
        var context = new QueryContext("Cratis.Chronicle.Observation.ObserverInformation.AllObservers", CorrelationId.New(), Paging.NotPaged, Sorting.None, _arguments, ServiceProvider: scope.ServiceProvider);
        return Guarded ? await scope.ServiceProvider.GetRequiredService<SystemStoreQueryFilter>().OnPerform(context) : _noQueryResult;
    }

    /// <summary>Measures a scoped ordinary command filter invocation.</summary>
    /// <returns>The filter result.</returns>
    [Benchmark]
    public async Task<CommandResult> Command()
    {
        await using var scope = _entitled.Services.CreateAsyncScope();
        _entitled.NextRequest(scope.ServiceProvider);
        return Guarded ? await scope.ServiceProvider.GetRequiredService<SystemStoreCommandFilter>().OnExecution(CommandFor(scope.ServiceProvider, _retry)) : _noCommandResult;
    }

    /// <summary>Measures an entitled append including first resolution in a fresh scope.</summary>
    /// <returns>The command result.</returns>
    [Benchmark]
    public Task<CommandResult> EntitledAppend() => Append(_entitled);

    /// <summary>Measures an unentitled append in audit mode.</summary>
    /// <returns>The command result.</returns>
    [Benchmark]
    public Task<CommandResult> AuditedUnentitledAppend() => Append(_unentitledAudited);

    /// <summary>Measures an unentitled append in enforcement mode.</summary>
    /// <returns>The command result.</returns>
    [Benchmark]
    public Task<CommandResult> RefusedUnentitledAppend() => Append(_unentitledEnforced);

    /// <summary>Disposes the callers and verifies that ordinary store calls made no roster calls.</summary>
    [GlobalCleanup]
    public void Verify() => Dispose();

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the caller snapshots and service providers.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _entitled?.Dispose();
        _unentitledAudited?.Dispose();
        _unentitledEnforced?.Dispose();
    }

    static CommandContext CommandFor(IServiceProvider services, object command) =>
        new(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: services);

    async Task<CommandResult> Append(EntitledCaller caller)
    {
        await using var scope = caller.Services.CreateAsyncScope();
        caller.NextRequest(scope.ServiceProvider);
        var context = CommandFor(scope.ServiceProvider, _append);
        return Guarded ? await scope.ServiceProvider.GetRequiredService<StoreEntitlementCommandFilter>().OnExecution(context) : _noCommandResult;
    }
}
