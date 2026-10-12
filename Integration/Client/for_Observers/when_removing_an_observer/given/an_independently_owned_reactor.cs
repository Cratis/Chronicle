// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Integration.for_Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Integration.for_Observers.when_removing_an_observer.given;

public class an_independently_owned_reactor(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
#pragma warning disable CA2213 // Destroy disposes these through the specification lifecycle.
    ServiceProvider? _ownerServices;
    protected ReactorHandler _managementHandler;
#pragma warning restore CA2213
    protected IChronicleClient _ownerClient;
    protected IEventStore _ownerStore;
    protected IReactorHandler _ownerHandler;
    public ReactorWithoutDelay Reactor = default!;
    public ObserverId ObserverId = default!;

    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

    /// <summary>
    /// Gets no reactors: management must never discover or re-register the owner's subscription.
    /// </summary>
    public override IEnumerable<Type> Reactors => [];

    async Task Establish()
    {
        Reactor = new();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Reactor);
        services.AddSingleton(Services.GetRequiredService<IGrainFactory>());
        services.AddSingleton(Services.GetRequiredService<ILocalSiloDetails>());
        services.AddSingleton(Services.GetRequiredService<IServices>());
        services.AddSingleton(Services.GetRequiredService<ILoggerFactory>());
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions
        {
            DefaultSinkTypeId = ChronicleClient.Options.DefaultSinkTypeId
        }));
        services.AddInProcessChronicleClient(new OwnerArtifacts(), EventStore.Name);
        _ownerServices = services.BuildServiceProvider();
        _ownerClient = _ownerServices.GetRequiredService<IChronicleClient>();
        _ownerStore = await _ownerClient.GetEventStore(EventStore.Name, EventStore.Namespace);
        _ownerStore.Connection.ShouldNotEqual(EventStore.Connection);
        _ownerStore.Connection.Lifecycle.ConnectionId.ShouldNotEqual(EventStore.Connection.Lifecycle.ConnectionId);
        await _ownerStore.WaitForRegistration();
        _ownerHandler = _ownerStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
        await _ownerHandler.WaitTillSubscribed();
        await _ownerHandler.WaitTillActive();
        ObserverId = _ownerHandler.Id.Value;

        // This handle queries through management without registering another subscription.
        _managementHandler = new ReactorHandler(
            EventStore,
            _ownerHandler.Id,
            _ownerHandler.ReactorType,
            _ownerHandler.EventSequenceId,
            _ownerHandler.EventTypes,
            ChronicleClient.CausationManager,
            Services.GetRequiredService<IIdentityProvider>());
    }

    protected async Task<bool> IsListed()
    {
        var observers = await EventStore.Observers.GetAll();
        return observers.Any(observer => observer.Id == ObserverId);
    }

    protected void DisposeOwner() => _ownerServices!.Dispose();

    void Destroy()
    {
        _managementHandler?.Dispose();
        _ownerServices?.Dispose();
    }

    sealed class OwnerArtifacts() : DefaultClientArtifactsProvider(new NoAssemblies())
    {
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorWithoutDelay)];
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
