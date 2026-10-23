// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Counts work started only within the replay measurement window.
/// </summary>
internal sealed class ReplayShapeCounters : IIncomingGrainCallFilter
{
    readonly object _gate = new();
    bool _measuring;
    long _mongoCommands;
    long _grainCalls;
    long _flushForCalls;
    long _getMoreCommands;
    Dictionary<long, long> _sinkBulks = [];
    long _ungroupedSinkBulks;

    /// <inheritdoc/>
    public Task Invoke(IIncomingGrainCallContext context)
    {
        var isFlushFor = context.InterfaceMethod?.Name == nameof(KernelCore::Cratis.Chronicle.Observation.IObserverService.FlushFor);
        lock (_gate)
        {
            if (_measuring)
            {
                _grainCalls++;
                if (isFlushFor) _flushForCalls++;
            }
        }
        return context.Invoke();
    }

    /// <summary>
    /// Registers counters only for this fixture.
    /// </summary>
    /// <param name="services">The silo service collection.</param>
    internal void Configure(IServiceCollection services)
    {
        services.AddSingleton<IIncomingGrainCallFilter>(this);
        services.RemoveAll<Storage.MongoDB.IMongoDBClientManager>();
        services.AddSingleton<Storage.MongoDB.IMongoDBClientManager>(provider =>
            new ReplayShapeMongoClients(new Storage.MongoDB.MongoDBClientManager(provider.GetRequiredService<Arc.MongoDB.IMongoDBClientFactory>()), this));
    }

    /// <summary>
    /// Opens a fresh measurement window.
    /// </summary>
    internal void Start()
    {
        lock (_gate)
        {
            _mongoCommands = 0;
            _grainCalls = 0;
            _flushForCalls = 0;
            _getMoreCommands = 0;
            _sinkBulks = [];
            _ungroupedSinkBulks = 0;
            _measuring = true;
        }
    }

    /// <summary>
    /// Closes the window atomically with the final counts.
    /// </summary>
    /// <returns>Commands and incoming grain invocations started in the window.</returns>
    internal (long MongoCommands, long GrainCalls, long FlushForCalls, long EventCursorPages, IReadOnlyList<long> SinkBulkOperations) Stop()
    {
        lock (_gate)
        {
            _measuring = false;
            var bulks = _sinkBulks.Values.ToList();
            for (var i = 0; i < _ungroupedSinkBulks; i++) bulks.Add(0);
            return (_mongoCommands, _grainCalls, _flushForCalls, _getMoreCommands + 1, bulks);
        }
    }

    /// <summary>
    /// Counts one driver command started in the window.
    /// </summary>
    /// <param name="started">The started command.</param>
    internal void OnCommand(CommandStartedEvent started)
    {
        var operations = SinkOperations(started);
        var isEventPage = started.CommandName == "getMore" && IsEventLog(started.Command);
        lock (_gate)
        {
            if (!_measuring) return;
            _mongoCommands++;
            if (isEventPage) _getMoreCommands++;
            if (operations <= 0) return;
            if (started.OperationId is { } id) _sinkBulks[id] = _sinkBulks.GetValueOrDefault(id) + operations;
            else _ungroupedSinkBulks++;
        }
    }

    static bool IsEventLog(BsonDocument command) =>
        command.TryGetValue("collection", out var collection) && collection.IsString && collection.AsString.Contains("event-log", StringComparison.OrdinalIgnoreCase);

    static long SinkOperations(CommandStartedEvent started)
    {
        var arrayName = started.CommandName switch
        {
            "insert" => "documents",
            "update" => "updates",
            "delete" => "deletes",
            _ => null
        };
        if (arrayName is null) return 0;
        if (!started.Command.TryGetValue(started.CommandName, out var target) || !target.IsString) return 0;
        if (!target.AsString.Contains(nameof(ReplayShapeReadModel), StringComparison.OrdinalIgnoreCase)) return 0;
        return started.Command.TryGetValue(arrayName, out var operations) && operations.IsBsonArray ? operations.AsBsonArray.Count : 0;
    }
}
