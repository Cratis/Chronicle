// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Counts work started only within the replay measurement window.
/// </summary>
internal sealed class ReplayShapeCounters : IIncomingGrainCallFilter
{
    readonly object _gate = new();
    readonly Dictionary<string, long> _mongoFindCommandsByTag = new(StringComparer.Ordinal)
    {
        ["Sink.FindOrDefault"] = 0,
        ["Sink.TryFindRootKeyByChildValue"] = 0,
        ["EventSequenceStorage.TryGetLastInstanceOfAny"] = 0,
        ["EventSequenceStorage.GetHeadSequenceNumber"] = 0,
        ["EventSequenceStorage.GetRange"] = 0,
        ["Other"] = 0
    };
    bool _measuring;
    long _mongoCommands;
    long _grainCalls;

    /// <inheritdoc/>
    public Task Invoke(IIncomingGrainCallContext context)
    {
        lock (_gate)
        {
            if (_measuring) _grainCalls++;
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
            foreach (var tag in _mongoFindCommandsByTag.Keys.ToArray())
            {
                _mongoFindCommandsByTag[tag] = 0;
            }
            _measuring = true;
        }
    }

    /// <summary>
    /// Closes the window atomically with the final counts.
    /// </summary>
    /// <returns>Commands, incoming grain invocations, and find commands grouped by call-site comment started in the window.</returns>
    internal (long MongoCommands, long GrainCalls, IReadOnlyDictionary<string, long> MongoFindCommandsByTag) Stop()
    {
        lock (_gate)
        {
            _measuring = false;
            return (_mongoCommands, _grainCalls, new Dictionary<string, long>(_mongoFindCommandsByTag, StringComparer.Ordinal));
        }
    }

    /// <summary>
    /// Counts one driver command started in the window.
    /// </summary>
    /// <param name="command">The command started by the MongoDB driver.</param>
    internal void OnCommand(CommandStartedEvent command)
    {
        lock (_gate)
        {
            if (!_measuring) return;
            _mongoCommands++;
            if (command.CommandName != "find") return;
            var tag = command.Command.TryGetValue("comment", out var comment) && comment.IsString
                ? comment.AsString
                : "Other";
            if (!_mongoFindCommandsByTag.TryGetValue(tag, out var count))
            {
                tag = "Other";
                count = _mongoFindCommandsByTag[tag];
            }
            _mongoFindCommandsByTag[tag] = count + 1;
        }
    }
}
