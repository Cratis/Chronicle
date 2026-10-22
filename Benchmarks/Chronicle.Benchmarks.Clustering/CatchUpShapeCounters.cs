// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver.Core.Events;
using Orleans;
using KernelJobs = KernelCore::Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Benchmarks.Clustering;

internal sealed class CatchUpShapeCounters : IIncomingGrainCallFilter, IOutgoingGrainCallFilter
{
    readonly ConcurrentDictionary<string, long> _mongo = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, long> _jobCalls = new(StringComparer.Ordinal);
    long _incoming;
    long _outgoing;
    long _catchUpStarts;
    long _replayCalls;
    int _enabled;
    string _observerId = string.Empty;

    public IGrainFactory Grains { get; private set; } = null!;

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IIncomingGrainCallFilter>(provider =>
        {
            Grains = provider.GetRequiredService<IGrainFactory>();
            return this;
        });
        services.AddSingleton<IOutgoingGrainCallFilter>(this);
        services.RemoveAll<Storage.MongoDB.IMongoDBClientManager>();
        services.AddSingleton<Storage.MongoDB.IMongoDBClientManager>(provider =>
            new CatchUpShapeMongoClients(new Storage.MongoDB.MongoDBClientManager(provider.GetRequiredService<Arc.MongoDB.IMongoDBClientFactory>()), this));
    }

    public void ExpectObserver(string observerId) => _observerId = observerId;

    public void Start()
    {
        _mongo.Clear();
        _jobCalls.Clear();
        Interlocked.Exchange(ref _incoming, 0);
        Interlocked.Exchange(ref _outgoing, 0);
        Interlocked.Exchange(ref _catchUpStarts, 0);
        Interlocked.Exchange(ref _replayCalls, 0);
        Volatile.Write(ref _enabled, 1);
    }

    public void Stop() => Volatile.Write(ref _enabled, 0);

    public void OnCommand(CommandStartedEvent command)
    {
        if (Volatile.Read(ref _enabled) == 0) return;
        var collection = command.CommandName == "getMore"
            ? command.Command.GetValue("collection", "(database)")
            : command.Command.GetValue(command.CommandName, "(database)");
        var name = collection.IsString ? collection.AsString : "(database)";
        var bucket = $"{command.DatabaseNamespace.DatabaseName}/{name}/{command.CommandName}";
        _mongo.AddOrUpdate(bucket, 1, (_, count) => count + 1);
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (Volatile.Read(ref _enabled) != 0)
        {
            Interlocked.Increment(ref _incoming);
            if (context.Grain is KernelJobs.CatchUpObserver or KernelJobs.ReplayObserver)
            {
                _jobCalls.AddOrUpdate($"{context.Grain.GetType().Name}.{context.MethodName}", 1, (_, count) => count + 1);
                if (context.Grain is KernelJobs.CatchUpObserver && context.MethodName == "Start" &&
                    context.Request.GetArgument(0) is KernelJobs.CatchUpObserverRequest request && request.ObserverKey.ObserverId.Value == _observerId)
                {
                    Interlocked.Increment(ref _catchUpStarts);
                }
                if (context.Grain is KernelJobs.ReplayObserver) Interlocked.Increment(ref _replayCalls);
            }
        }

        await context.Invoke();
    }

    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (Volatile.Read(ref _enabled) != 0) Interlocked.Increment(ref _outgoing);
        await context.Invoke();
    }

    public object Snapshot(int events) => new
    {
        IncomingGrainCalls = Interlocked.Read(ref _incoming),
        OutgoingGrainCalls = Interlocked.Read(ref _outgoing),
        CatchUpObserverStarts = Interlocked.Read(ref _catchUpStarts),
        ReplayObserverCalls = Interlocked.Read(ref _replayCalls),
        JobCallsByTypeAndMethod = _jobCalls.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value),
        MongoCommands = _mongo.Values.Sum(),
        MongoCommandsPerEvent = (double)_mongo.Values.Sum() / events,
        MongoCommandsByDatabaseCollectionAndName = _mongo.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => new { Count = pair.Value, CommandsPerEvent = (double)pair.Value / events })
    };

    public void VerifyJobShape()
    {
        if (Interlocked.Read(ref _catchUpStarts) == 0 || Interlocked.Read(ref _replayCalls) != 0)
        {
            throw new InvalidOperationException("Expected CatchUpObserver.Start for the diagnostic observer and no ReplayObserver activity in the measured window.");
        }
    }
}
