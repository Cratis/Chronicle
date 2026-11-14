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
    readonly string _diagnosticCorrelation = Guid.NewGuid().ToString("N");

    void Diagnostic(string message) => Console.WriteLine($"DRIFT4783 {DateTimeOffset.UtcNow:O} scenario={GetType().Name} correlation={_diagnosticCorrelation} store={EventStore.Name} namespace={EventStore.Namespace} connectionId={EventStore.Connection.Lifecycle.ConnectionId} connected={EventStore.Connection.Lifecycle.IsConnected} {message}");

    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public override IEnumerable<Type> Reactors => [typeof(Original.SendWelcomeEmail)];

    async Task Establish()
    {
        await EventStore.Reactors.GetHandlerFor<Original.SendWelcomeEmail>().WaitTillSubscribed(TimeSpan.FromSeconds(30));
        Diagnostic($"established original={EventStore.Reactors.GetHandlerFor<Original.SendWelcomeEmail>().Id} WaitTillSubscribed=completed connection={EventStore.Connection.GetType().FullName} servicesProvider={Services.GetType().FullName} originalPolicy={Services.GetRequiredService<IChronicleClient>().Options.ArtifactIdentityDrift}");
    }

    protected async Task RegisterRenamed(bool disconnectOriginal, ArtifactIdentityDriftPolicy policy = ArtifactIdentityDriftPolicy.Warn)
    {
        Diagnostic($"register-renamed begin disconnectOriginal={disconnectOriginal} requestedPolicy={policy}");
        if (disconnectOriginal)
        {
            var original = EventStore.Reactors.GetHandlerFor<Original.SendWelcomeEmail>();
            EventStore.Reactors.Unregister(original.Id);

            // The captured handler still queries its observer after removal from the client's handler set.
            await original.WaitForState(ObserverRunningState.Disconnected, TimeSpan.FromSeconds(30));
            Diagnostic($"cleanup original={original.Id} Unregister=returned WaitForState=Disconnected-completed");
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
        Diagnostic($"renamedClient effectiveOptionsPolicy={_renamedClient.Options.ArtifactIdentityDrift} autoDiscover={_renamedClient.Options.AutoDiscoverAndRegister} artifactsProvider=RenamedArtifacts serviceProviderHash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Services)}");
        _renamedStore = await _renamedClient.GetEventStore(EventStore.Name, EventStore.Namespace);
        Diagnostic($"GetEventStore=returned registrationHasRun={_renamedStore.Registration.HasRun} effectiveStorePolicy={_renamedStore.GetType().GetField(\"_identityDriftPolicy\", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_renamedStore)} accessorHash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_renamedStore.Connection)} connectionType={_renamedStore.Connection.GetType().FullName} runtimeConnectionId={_renamedStore.Connection.Lifecycle.ConnectionId}");
        await _renamedStore.DiscoverAll();
        Diagnostic("DiscoverAll=returned explicitRegisterAll=begin");
        _failure = await Catch.Exception(_renamedStore.RegisterAll);
        Diagnostic($"explicitRegisterAll=returned failure={_failure?.ToString() ?? \"null\"} drift={string.Join(';', _renamedStore.Registration.IdentityDrift.Select(_ => $\"{_.Kind}:{_.PreviousId}->{_.CurrentId}\"))}");
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
