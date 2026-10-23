// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            _measuring = true;
        }
    }

    /// <summary>
    /// Closes the window atomically with the final counts.
    /// </summary>
    /// <returns>Commands and incoming grain invocations started in the window.</returns>
    internal (long MongoCommands, long GrainCalls) Stop()
    {
        lock (_gate)
        {
            _measuring = false;
            return (_mongoCommands, _grainCalls);
        }
    }

    /// <summary>
    /// Counts one driver command started in the window.
    /// </summary>
    internal void OnCommand()
    {
        lock (_gate)
        {
            if (_measuring) _mongoCommands++;
        }
    }
}
