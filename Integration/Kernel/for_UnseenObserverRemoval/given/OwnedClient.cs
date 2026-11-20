// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// A client of its own, with its own connection to the kernel, that owns the observers it is given. Disposing it is the client
/// going away: its subscriptions end and nothing re-registers them.
/// </summary>
public sealed class OwnedClient : IAsyncDisposable
{
    readonly ServiceProvider _services;

    OwnedClient(ServiceProvider services, IEventStore store)
    {
        _services = services;
        Store = store;
    }

    /// <summary>
    /// Gets the <see cref="IEventStore"/> of the client.
    /// </summary>
    public IEventStore Store { get; }

    /// <summary>
    /// Starts a client that owns the given reactors and reducers and waits until each is subscribed.
    /// </summary>
    /// <param name="kernelServices">The services of the kernel the client connects to.</param>
    /// <param name="defaultSinkTypeId">The default sink of the client.</param>
    /// <param name="eventStore">The event store the client connects to.</param>
    /// <param name="namespace">The namespace the client connects to.</param>
    /// <param name="observers">The reactor and reducer instances the client owns.</param>
    /// <returns>The started client.</returns>
    public static async Task<OwnedClient> Start(
        IServiceProvider kernelServices,
        Sinks.SinkTypeId defaultSinkTypeId,
        string eventStore,
        string @namespace,
        params object[] observers)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        foreach (var observer in observers)
        {
            services.AddSingleton(observer.GetType(), observer);
        }

        services.AddSingleton(kernelServices.GetRequiredService<IGrainFactory>());
        services.AddSingleton(kernelServices.GetRequiredService<ILocalSiloDetails>());
        services.AddSingleton(kernelServices.GetRequiredService<IConnectionOwnership>());
        services.AddSingleton(kernelServices.GetRequiredService<IServices>());
        services.AddSingleton(kernelServices.GetRequiredService<ILoggerFactory>());
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions { DefaultSinkTypeId = defaultSinkTypeId }));
        services.AddInProcessChronicleClient(new Artifacts(observers.Select(_ => _.GetType()).ToArray()), eventStore);

        var provider = services.BuildServiceProvider();
        try
        {
            var store = await provider.GetRequiredService<IChronicleClient>().GetEventStore(eventStore, @namespace);
            await store.WaitForRegistration();
            var owned = new OwnedClient(provider, store);
            foreach (var observer in observers)
            {
                await owned.WaitTillActive(observer);
            }

            return owned;
        }
        catch
        {
            await provider.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Gets the handler of a reactor the client owns.
    /// </summary>
    /// <typeparam name="TReactor">The type of the reactor.</typeparam>
    /// <returns>The <see cref="IReactorHandler"/>.</returns>
    public IReactorHandler Reactor<TReactor>()
        where TReactor : IReactor => Store.Reactors.GetHandlerFor<TReactor>();

    /// <summary>
    /// Gets the handler of a reducer the client owns.
    /// </summary>
    /// <typeparam name="TReducer">The type of the reducer.</typeparam>
    /// <returns>The <see cref="IReducerHandler"/>.</returns>
    public IReducerHandler Reducer<TReducer>()
        where TReducer : IReducer => Store.Reducers.GetHandlerFor<TReducer>();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _services.DisposeAsync();

    async Task WaitTillActive(object observer)
    {
        if (observer is IReactor)
        {
            var handler = Store.Reactors.GetHandlerFor(observer.GetType());
            await handler.WaitTillSubscribed();
            await handler.WaitTillActive();
        }
        else if (observer is IReducer)
        {
            var handler = Store.Reducers.GetHandlerFor(observer.GetType());
            await handler.WaitTillSubscribed();
            await handler.WaitTillActive();
        }
    }

    sealed class Artifacts(Type[] observers) : DefaultClientArtifactsProvider(new NoAssemblies())
    {
        public override IEnumerable<Type> EventTypes => [typeof(UnseenEvent)];

        public override IEnumerable<Type> Reactors => observers.Where(_ => typeof(IReactor).IsAssignableFrom(_));

        public override IEnumerable<Type> Reducers => observers.Where(_ => typeof(IReducer).IsAssignableFrom(_));

        public override IEnumerable<Type> Projections => observers.Where(_ => _.GetInterfaces().Any(@interface => @interface.Name == "IProjection"));
    }

    sealed class NoAssemblies : ICanProvideAssembliesForDiscovery
    {
        public IEnumerable<Assembly> Assemblies => [];

        public IEnumerable<Type> DefinedTypes => [];

        public void Initialize()
        {
        }
    }
}
