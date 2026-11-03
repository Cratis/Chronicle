// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Kernel.Integration.Setup.for_background_startup;

public sealed class StartupClient : IAsyncDisposable
{
    readonly ServiceProvider _provider;
    bool _disposed;

    StartupClient(ServiceProvider provider, IEventStore store)
    {
        _provider = provider;
        Store = store;
    }

    public IEventStore Store { get; }
    public string ConnectionId => Store.Connection.Lifecycle.ConnectionId.Value;

    public static async Task<StartupClient> Start(IServiceProvider kernel)
    {
        var services = new ServiceCollection();
        services.AddSingleton(kernel.GetRequiredService<IGrainFactory>());
        services.AddSingleton(kernel.GetRequiredService<ILocalSiloDetails>());
        services.AddSingleton<IServices>(_ => kernel.GetRequiredService<IServices>());
        services.AddSingleton(kernel.GetRequiredService<ILoggerFactory>());
        services.AddLogging();
        services.AddSingleton(Options.Create<ChronicleOptions>(new ChronicleClientOptions { EventStore = BackgroundStartupFixture.EventStore }));
        services.AddInProcessChronicleClient(new DefaultClientArtifactsProvider(new EventDiscovery()), BackgroundStartupFixture.EventStore);
        var provider = services.BuildServiceProvider();
        try
        {
            var store = await provider.GetRequiredService<IChronicleClient>().GetEventStore(BackgroundStartupFixture.EventStore)
                .WaitAsync(BackgroundStartupFixture.OperationDeadline);
            (await store.WaitForRegistration().WaitAsync(BackgroundStartupFixture.OperationDeadline)).IsSuccess.ShouldBeTrue();
            return new(provider, store);
        }
        catch
        {
            await provider.DisposeAsync();
            throw;
        }
    }

    public async Task<IReactorHandler> Register(string observer, ConcurrentQueue<StartupDeliveryRecorded> callbacks)
    {
        var definition = Store.Reactors.Define(observer, reactor => reactor.On<StartupEvent>(async (input, context) =>
        {
            var receipt = new StartupDeliveryRecorded(observer, input.Token, context.SequenceNumber.Value);
            callbacks.Enqueue(receipt);

            // Ordinary append: every invocation creates another durable row, including a duplicate invocation.
            var result = await Store.EventLog.Append(context.EventSourceId, receipt)
                .WaitAsync(BackgroundStartupFixture.OperationDeadline);
            result.IsSuccess.ShouldBeTrue();
        }));
        var handler = await Store.Reactors.Register(definition).WaitAsync(BackgroundStartupFixture.OperationDeadline);
        await handler.WaitTillSubscribed(BackgroundStartupFixture.OperationDeadline);
        return handler;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await Store.Connection.Lifecycle.Disconnected().WaitAsync(BackgroundStartupFixture.OperationDeadline);
        await _provider.DisposeAsync();
    }

    sealed class EventDiscovery : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<Assembly> Assemblies => [typeof(StartupEvent).Assembly];
        public IEnumerable<Type> DefinedTypes => [typeof(StartupEvent), typeof(StartupDeliveryRecorded)];
        public void Initialize() { }
    }
}
