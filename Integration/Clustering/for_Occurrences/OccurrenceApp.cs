// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

/// <summary>
/// One independent application host and connection against a real silo, using the shipped delivery loop.
/// </summary>
public sealed class OccurrenceApp : IAsyncDisposable
{
    readonly ServiceProvider _transport;
    readonly IHost _host;
    readonly CapableOccurrenceConnection _connection;
    bool _disposed;

    OccurrenceApp(ServiceProvider transport, IHost host, CapableOccurrenceConnection connection)
    {
        _transport = transport;
        _host = host;
        _connection = connection;
    }

    public IEventStore Store { get; private set; } = null!;
    public Contracts.Occurrences.IOccurrences Service => _connection.Services.Occurrences;
    public string ConnectionId => _connection.Lifecycle.ConnectionId.Value;

    public static async Task<OccurrenceApp> Start(IServiceProvider kernel, OccurrenceSignals signals, string activation, ActivationScope scope = ActivationScope.Application, bool handlers = true, bool lifecycle = true, LifecycleScope lifecycleScope = LifecycleScope.Application)
    {
        var options = new ChronicleClientOptions { EventStore = Constants.EventStore, ActivationId = new(activation), ActivationScope = scope, LifecycleScope = lifecycleScope };
        var services = new ServiceCollection();
        services.AddSingleton(kernel.GetRequiredService<IGrainFactory>());
        services.AddSingleton(kernel.GetRequiredService<ILocalSiloDetails>());
        services.AddSingleton<IServices>(_ => kernel.GetRequiredService<IServices>());
        services.AddSingleton(kernel.GetRequiredService<ILoggerFactory>());
        services.AddLogging();
        services.AddSingleton(Options.Create<ChronicleOptions>(options));
        services.AddInProcessChronicleClient(new DefaultClientArtifactsProvider(new OccurrenceDiscovery()), Constants.EventStore);
        var transport = services.BuildServiceProvider();
        var innerStore = transport.GetRequiredService<IEventStore>();
        var inner = innerStore.Connection;
        var api = ((IChronicleServicesAccessor)inner).Services;
        var advertised = await api.Connections.CheckCompatibility(new()
        {
            ClientType = ".NET",
            ClientVersion = Contracts.ProtocolVersion.Current,
            ProtocolVersion = Contracts.ProtocolVersion.Current,
            DescriptorSet = Contracts.WireContractDescriptorSet.Bytes.ToArray()
        });
        advertised.IsCompatible.ShouldBeTrue();
        var connection = new CapableOccurrenceConnection(inner, advertised.Capabilities.ToArray());
        var types = new List<Type> { typeof(OccurrenceWork), typeof(OccurrenceWorkCompleted) };
        if (handlers) types.Add(typeof(WorkHandler));
        if (lifecycle) types.Add(typeof(StartupHandler));
        var artifacts = new DefaultClientArtifactsProvider(new OccurrenceDiscovery(types.ToArray()));
        var host = new HostBuilder().ConfigureServices(collection =>
        {
            collection.AddLogging();
            collection.AddSingleton(signals);
            collection.AddSingleton<Connections.IChronicleConnection>(connection);
            collection.Configure<ChronicleAspNetCoreOptions>(configured =>
            {
                configured.EventStore = options.EventStore;
                configured.ActivationId = options.ActivationId;
                configured.ActivationScope = options.ActivationScope;
                configured.LifecycleScope = options.LifecycleScope;
            });
            collection.AddCratisChronicleClient(new ChronicleBuilder(collection, new ConfigurationManager(), artifacts)
            {
                NamespaceResolver = new DefaultEventStoreNamespaceResolver()
            });
        }).Build();
        var app = new OccurrenceApp(transport, host, connection);
        try
        {
            await host.StartAsync().WaitAsync(TimeSpan.FromSeconds(90));
            app.Store = await host.Services.GetRequiredService<IChronicleClient>().GetEventStore(Constants.EventStore).WaitAsync(TimeSpan.FromSeconds(90));
            (await app.Store.WaitForRegistration().WaitAsync(TimeSpan.FromSeconds(90))).IsSuccess.ShouldBeTrue();
            return app;
        }
        catch
        {
            host.Dispose();
            await transport.DisposeAsync();
            throw;
        }
    }

    public async Task<OccurrenceSnapshotResponse[]> WaitFor(string trigger, Func<OccurrenceSnapshotResponse[], bool> condition, bool application = false, string? @namespace = null)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        return await Service.ObserveOccurrences(new()
        {
            EventStore = Constants.EventStore,
            Namespace = @namespace ?? EventStoreNamespaceName.Default.Value,
            ApplicationScope = application,
            Trigger = trigger
        }).Select(result => result.EnsureSuccess().ToArray()).FirstAsync(condition).ToTask(deadline.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _connection.Lifecycle.Disconnected();
        if (Store?.Occurrences is IAsyncDisposable occurrences) await occurrences.DisposeAsync();
        _host.Dispose();
        await _transport.DisposeAsync();
    }
}
