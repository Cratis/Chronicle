// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Attributes the cost of creating service proxies through the per-connection lazy container.
/// </summary>
/// <remarks>
/// Creating a proxy never dials, so the channel points at an unused loopback address and no kernel is needed.
/// Reflection and delegate compilation in setup allow access to the internal container without changing its API;
/// neither is part of a measured operation. Each operation creates a fresh container.
/// The cold job includes proxy type emission. Its Allocated column comes from a separate diagnoser run, not
/// first-call allocations. Timing comparisons remain subject to the machine's load.
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
    /// Accesses all 30 services plus the compatibility and keep-alive connection service accesses.
    /// </summary>
    /// <returns>The number of service accesses (not unique proxies).</returns>
    [Benchmark]
    public int AllServices()
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

    /// <summary>
    /// Accesses only what connect plus the first append touch.
    /// </summary>
    /// <returns>The number of service accesses.</returns>
    [Benchmark]
    public int AppendPath()
    {
        var services = _createServices(_callInvoker!, _factory);
        AccessAppendPath(services);
        return 5;
    }

    /// <summary>
    /// Accesses the services used by discovery and registration, plus the append path.
    /// </summary>
    /// <returns>The number of service accesses.</returns>
    [Benchmark]
    public int AutoRegisterPath()
    {
        var services = _createServices(_callInvoker!, _factory);
        AccessAppendPath(services);
        _ = services.Constraints;
        _ = services.Observers;
        _ = services.Reactors;
        _ = services.Reducers;
        _ = services.Projections;
        _ = services.Webhooks;
        _ = services.ReadModels;
        _ = services.Seeding;
        return 13;
    }

    static void AccessAppendPath(IServices services)
    {
        _ = services.EventStores;
        _ = services.Namespaces;
        _ = services.EventTypes;
        _ = services.Sequences;
        _ = services.Connections;
    }
}
