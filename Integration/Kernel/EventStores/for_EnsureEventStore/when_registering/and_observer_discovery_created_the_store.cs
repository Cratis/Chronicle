// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

using context = Cratis.Chronicle.Kernel.Integration.EventStores.for_EnsureEventStore.when_registering.and_observer_discovery_created_the_store.context;
using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;
using KernelEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelObserverKey = Cratis.Chronicle.Concepts.Observation.ObserverKey;
using SignalingObserverStorage = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.PatternCaptureFixture.SignalingObserverStorage;

namespace Cratis.Chronicle.Kernel.Integration.EventStores.for_EnsureEventStore.when_registering;

[Collection(given.ProvisioningCollection.Name)]
public class and_observer_discovery_created_the_store(context context) : Given<context>(context)
{
    public class context(given.ProvisioningFixture fixture) : Specification<given.ProvisioningFixture>(fixture)
    {
        IChronicleServicesAccessor _services;
        IEventStore _store;
        string _storeName;
        public bool StoreExistedAfterDiscovery;
        public bool CompletionSucceeded;
        public bool StatisticsHandledFirstAppend;
        public bool DefinitionsAreUnique;
        public bool Initialized;
        public int ProjectionRegistrations;

        public override bool AutoDiscoverArtifacts => false;
        public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];

        protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
        {
            builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
            builder.SiloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(provider => new given.CountingKernelProjections(ActivatorUtilities.CreateInstance<KernelProjections>(provider)));
                services.AddSingleton<IKernelProjections>(provider => provider.GetRequiredService<given.CountingKernelProjections>());
                services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.ObserverState, (provider, _) =>
                    ActivatorUtilities.CreateInstance<SignalingObserverStorage>(provider));
            });
        };

        async Task Establish()
        {
            _storeName = $"discovered-{Guid.NewGuid():N}";
            _services = (IChronicleServicesAccessor)Services.GetRequiredService<IChronicleConnection>();
            var storage = Services.GetRequiredService<IStorage>();
            (await storage.HasEventStore(_storeName)).ShouldBeFalse();
            (await _services.Services.Observers.GetObservers(new AllObserversRequest { EventStore = _storeName, Namespace = "new-tenant" })).ShouldBeEmpty();
            StoreExistedAfterDiscovery = await storage.HasEventStore(_storeName);
        }

        async Task Because()
        {
            // Race the first ensures before any client registration has initialized this implicitly created store.
            await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                _services.Services.EventStores.EnsureEventStore(new EnsureEventStoreRequest { Name = _storeName }).EnsureSuccess()));
            _store = await ChronicleClient.GetEventStore(_storeName, "new-tenant");
            await _store.DiscoverAll();
            await _store.RegisterAll();
            (await _store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

            var append = await _store.EventLog.Append("customer", new CustomerNamed("First"));
            append.IsSuccess.ShouldBeTrue();
            CompletionSucceeded = (await append.WaitForCompletion(TimeSpan.FromSeconds(30))).IsSuccess;
            var observerStorage = (SignalingObserverStorage)Services.GetRequiredKeyedService<IGrainStorage>(WellKnownGrainStorageProviders.ObserverState);
            var key = new KernelObserverKey("$system.statistics.event-types", _storeName, _store.Namespace.Value, KernelEventSequenceId.Log);
            StatisticsHandledFirstAppend = await observerStorage.WaitForHandled(key, append.SequenceNumber.Value) == append.SequenceNumber.Value;

            await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                _services.Services.EventStores.EnsureEventStore(new EnsureEventStoreRequest { Name = _storeName }).EnsureSuccess()));
            var definitions = (await Services.GetRequiredService<IStorage>().GetEventStore(_storeName).Observers.GetAll()).ToArray();
            DefinitionsAreUnique = definitions.Length > 0 && definitions.DistinctBy(definition => definition.Identifier).Count() == definitions.Length;
            Initialized = await Services.GetRequiredService<IStorage>().GetEventStore(_storeName).IsInitialized();
            ProjectionRegistrations = Services.GetRequiredService<given.CountingKernelProjections>().CallsFor(_storeName);
        }
    }

    [Fact] void should_have_provisioned_the_store_through_discovery() => Context.StoreExistedAfterDiscovery.ShouldBeTrue();
    [Fact] void should_complete_the_first_append() => Context.CompletionSucceeded.ShouldBeTrue();
    [Fact] void should_have_statistics_handle_the_first_append() => Context.StatisticsHandledFirstAppend.ShouldBeTrue();
    [Fact] void should_keep_one_definition_per_observer_after_concurrent_ensures() => Context.DefinitionsAreUnique.ShouldBeTrue();
    [Fact] void should_initialize_projections_once_despite_concurrent_ensures_and_reconnects() => Context.ProjectionRegistrations.ShouldEqual(1);
    [Fact] void should_durably_record_completed_initialization() => Context.Initialized.ShouldBeTrue();
}
