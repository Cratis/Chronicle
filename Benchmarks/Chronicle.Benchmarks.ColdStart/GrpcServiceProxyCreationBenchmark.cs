// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSources;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Compares creating service proxies eagerly (as connect did before #5123) with the per-connection lazy container.
/// </summary>
/// <remarks>
/// Creating a proxy never dials, so the channel points at an unused loopback address and no kernel is needed.
/// <see cref="EagerConnect"/> is the baseline: it creates all 32 proxies directly through the factory.
/// <see cref="LazyConnect"/> mirrors what connect does now; the other lazy methods add what later paths touch.
/// Reflection and delegate compilation in setup allow access to the internal container without changing its API;
/// neither is part of a measured operation. Each operation creates a fresh container.
/// Allocated is the primary evidence; timings are load-sensitive. The cold job includes proxy type emission and its
/// Allocated column comes from a separate diagnoser run, not first-call allocations.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 30, warmupCount: 0, iterationCount: 1)]
[SimpleJob]
public class GrpcServiceProxyCreationBenchmark
{
    readonly InProcessAwareGrpcClientProxiesClientFactory _factory = new();
    GrpcChannel? _channel;
    CallInvoker? _callInvoker;
    Func<CallInvoker, ClientFactory, IServices> _createServices = null!;

    /// <summary>
    /// Creates the channel and compiles a factory for the internal service container.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _channel = GrpcChannel.ForAddress("http://127.0.0.1:1");
        _callInvoker = _channel.CreateCallInvoker();
        var type = typeof(ChronicleConnection).Assembly.GetType("Cratis.Chronicle.Connections.LazyServices", throwOnError: true)!;
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(CallInvoker), typeof(ClientFactory)],
            modifiers: null)!;
        var invoker = Expression.Parameter(typeof(CallInvoker));
        var factory = Expression.Parameter(typeof(ClientFactory));
        _createServices = Expression.Lambda<Func<CallInvoker, ClientFactory, IServices>>(
            Expression.New(constructor, invoker, factory), invoker, factory).Compile();
    }

    /// <summary>
    /// Disposes the channel.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => _channel?.Dispose();

    /// <summary>
    /// Creates every service eagerly as connect did before #5123: the 30 of the service container plus the two extra connection services.
    /// </summary>
    /// <returns>The number of services created.</returns>
    [Benchmark(Baseline = true)]
    public int EagerConnect()
    {
        var count = 0;
        count += Create<ICompliance>();
        count += Create<IEventStores>();
        count += Create<INamespaces>();
        count += Create<IRecommendations>();
        count += Create<Contracts.Patterns.IPatterns>();
        count += Create<IIdentities>();
        count += Create<Contracts.Sequences.IEventSequences>();
        count += Create<IEventTypes>();
        count += Create<IConstraints>();
        count += Create<IObservers>();
        count += Create<IFailedPartitions>();
        count += Create<IReactors>();
        count += Create<IReducers>();
        count += Create<IProjections>();
        count += Create<IWebhooks>();
        count += Create<IExternalServices>();
        count += Create<ICaptures>();
        count += Create<IEventStoreSubscriptions>();
        count += Create<Contracts.ReadModels.IReadModels>();
        count += Create<Contracts.ReadModels.IMaterializedReadModels>();
        count += Create<Contracts.ReadModelExplorer.IReadModelExplorer>();
        count += Create<IJobs>();
        count += Create<IEventSeeding>();
        count += Create<IUsers>();
        count += Create<IApplications>();
        count += Create<IServer>();
        count += Create<IConnectionService>();
        count += Create<IEventSources>();
        count += Create<Contracts.Security.IAdministrators>();
        count += Create<Contracts.Occurrences.IOccurrences>();
        count += Create<IConnectionService>(); // compatibility check
        count += Create<IConnectionService>(); // keep-alive
        return count;
    }

    /// <summary>
    /// Creates the lazy container and performs the compatibility-check and keep-alive connection service accesses that connect does now.
    /// </summary>
    /// <returns>The number of service accesses.</returns>
    [Benchmark]
    public int LazyConnect()
    {
        var services = _createServices(_callInvoker!, _factory);
        _ = services.Connections; // compatibility check
        _ = services.Connections; // keep-alive
        return 2;
    }

    /// <summary>
    /// Creates the lazy container and accesses only what connect plus the first append touch.
    /// </summary>
    /// <returns>The number of service accesses.</returns>
    [Benchmark]
    public int LazyAppendPath()
    {
        var services = _createServices(_callInvoker!, _factory);
        AccessAppendPath(services);
        return 5;
    }

    /// <summary>
    /// Accesses all 30 services plus the compatibility and keep-alive connection service accesses.
    /// </summary>
    /// <returns>The number of service accesses (not unique proxies).</returns>
    [Benchmark]
    public int LazyAllServices()
    {
        var services = _createServices(_callInvoker!, _factory);
        _ = services.Compliance;
        _ = services.EventStores;
        _ = services.Namespaces;
        _ = services.Recommendations;
        _ = services.Patterns;
        _ = services.Identities;
        _ = services.Sequences;
        _ = services.EventTypes;
        _ = services.Constraints;
        _ = services.Observers;
        _ = services.FailedPartitions;
        _ = services.Reactors;
        _ = services.Reducers;
        _ = services.Projections;
        _ = services.Webhooks;
        _ = services.ExternalServices;
        _ = services.Captures;
        _ = services.EventStoreSubscriptions;
        _ = services.ReadModels;
        _ = services.MaterializedReadModels;
        _ = services.ReadModelExplorer;
        _ = services.Jobs;
        _ = services.Seeding;
        _ = services.Users;
        _ = services.Applications;
        _ = services.Server;
        _ = services.Connections;
        _ = services.EventSources;
        _ = services.Administrators;
        _ = services.Occurrences;
        _ = services.Connections;
        _ = services.Connections;
        return 32;
    }

    static void AccessAppendPath(IServices services)
    {
        _ = services.EventStores;
        _ = services.Namespaces;
        _ = services.EventTypes;
        _ = services.Sequences;
        _ = services.Connections;
    }

    int Create<T>()
        where T : class
    {
        _ = _callInvoker!.CreateGrpcService<T>(_factory);
        return 1;
    }
}
