// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Attributes the cost of creating the gRPC service proxies that a connect creates today.
/// </summary>
/// <remarks>
/// Creating a proxy never dials, so the channel points at an unused loopback address and no kernel is needed.
/// The cold job (one launch per measurement, no warmup) includes proxy type emission and protobuf-net model work.
/// The default job gives the warm cost and Allocated per connect. BenchmarkDotNet reports Allocated from a separate
/// diagnoser run, so the cold job's Allocated is not the first-call allocation; the probe measures that.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 30, warmupCount: 0, iterationCount: 1)]
[SimpleJob]
public class GrpcServiceProxyCreationBenchmark
{
    readonly InProcessAwareGrpcClientProxiesClientFactory _factory = new();
    GrpcChannel? _channel;
    CallInvoker? _callInvoker;

    /// <summary>
    /// Creates the channel the proxies are created over.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _channel = GrpcChannel.ForAddress("http://127.0.0.1:1");
        _callInvoker = _channel.CreateCallInvoker();
    }

    /// <summary>
    /// Disposes the channel.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup() => _channel?.Dispose();

    /// <summary>
    /// Creates every service a connect creates: the 30 of the service container plus the two extra connection services.
    /// </summary>
    /// <returns>The number of services created.</returns>
    [Benchmark]
    public int AllServices()
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
    /// Creates only what connect plus the first append touch.
    /// </summary>
    /// <returns>The number of services created.</returns>
    [Benchmark]
    public int AppendPath()
    {
        var count = 0;
        count += Create<IEventStores>();
        count += Create<INamespaces>();
        count += Create<IEventTypes>();
        count += Create<Contracts.Sequences.IEventSequences>();
        count += Create<IConnectionService>();
        return count;
    }

    /// <summary>
    /// Creates the services the discover and register chain touches (derived by grepping
    /// <c>Services.&lt;Name&gt;</c> in the registration code of Source/Clients/DotNET), plus the append path.
    /// </summary>
    /// <returns>The number of services created.</returns>
    [Benchmark]
    public int AutoRegisterPath()
    {
        var count = AppendPath();
        count += Create<IConstraints>();
        count += Create<IObservers>();
        count += Create<IReactors>();
        count += Create<IReducers>();
        count += Create<IProjections>();
        count += Create<IWebhooks>();
        count += Create<Contracts.ReadModels.IReadModels>();
        count += Create<IEventSeeding>();
        return count;
    }

    int Create<T>()
        where T : class
    {
        _ = _callInvoker!.CreateGrpcService<T>(_factory);
        return 1;
    }
}
