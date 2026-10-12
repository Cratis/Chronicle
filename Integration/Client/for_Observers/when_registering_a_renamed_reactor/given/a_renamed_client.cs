// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using Cratis.Chronicle.Integration.for_Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Observers.when_registering_a_renamed_reactor.given;

public class a_renamed_client(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
#pragma warning disable CA2213 // Destroy disposes the client through the specification lifecycle.
    ChronicleClient? _renamedClient;
#pragma warning restore CA2213
    protected IEventStore _renamedStore;
    protected Exception? _failure;

    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public override IEnumerable<Type> Reactors => [typeof(Original.SendWelcomeEmail)];

    async Task Establish() => await EventStore.Reactors.GetHandlerFor<Original.SendWelcomeEmail>().WaitTillSubscribed(TimeSpan.FromSeconds(30));

    protected async Task RegisterRenamed(bool disconnectOriginal, ArtifactIdentityDriftPolicy policy = ArtifactIdentityDriftPolicy.Warn)
    {
        if (disconnectOriginal)
        {
            var original = EventStore.Reactors.GetHandlerFor<Original.SendWelcomeEmail>();
            EventStore.Reactors.Unregister(original.Id);

            // The captured handler still queries its observer after removal from the client's handler set.
            await original.WaitForState(ObserverRunningState.Disconnected, TimeSpan.FromSeconds(30));
        }

        _renamedClient = new ChronicleClient(
            new BorrowedConnection(EventStore.Connection),
            new ChronicleOptions
            {
                AutoDiscoverAndRegister = false,
                ArtifactIdentityDrift = policy,
                DefaultSinkTypeId = Services.GetRequiredService<IChronicleClient>().Options.DefaultSinkTypeId
            },
            artifactsProvider: new RenamedArtifacts(),
            serviceProvider: Services);

        // Register explicitly: a failure during GetEventStore would leave its lifecycle callback attached to the
        // shared connection after the faulted store is evicted, registering the renamed identity in a later fact.
        _renamedStore = await _renamedClient.GetEventStore(EventStore.Name, EventStore.Namespace);
        await _renamedStore.DiscoverAll();
        _failure = await Catch.Exception(_renamedStore.RegisterAll);
    }

    void Destroy()
    {
        _renamedClient?.EvictEventStores();
        _renamedClient?.Dispose();
    }

    sealed class RenamedArtifacts() : DefaultClientArtifactsProvider(new NoAssemblies())
    {
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(Renamed.SendWelcomeEmail)];
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
