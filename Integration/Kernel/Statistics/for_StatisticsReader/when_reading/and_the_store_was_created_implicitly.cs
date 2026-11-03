// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Statistics;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

using context = Cratis.Chronicle.Kernel.Integration.Statistics.for_StatisticsReader.when_reading.and_the_store_was_created_implicitly.context;
using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;
using KernelChronicleBuilder = Cratis.Chronicle.Configuration.IChronicleBuilder;
using KernelEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelObserverKey = Cratis.Chronicle.Concepts.Observation.ObserverKey;
using SignalingObserverStorage = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.PatternCaptureFixture.SignalingObserverStorage;

namespace Cratis.Chronicle.Kernel.Integration.Statistics.for_StatisticsReader.when_reading;

[Collection(given.StatisticsCollection.Name)]
public class and_the_store_was_created_implicitly(context context) : Given<context>(context)
{
    public class context(given.StatisticsFixture fixture) : Specification<given.StatisticsFixture>(fixture)
    {
        string _storeName;
        IChronicleServicesAccessor _services;
        IStatisticsReader _statistics;
        public List<EventTypeStatistics> InitialRows = [];
        public EventTypeStatistics[] FirstNamespaceRows;
        public EventTypeStatistics[] SecondNamespaceRows;
        public EventTypeStatistics[] StoreRows;
        public string EventTypeId;

        public override bool AutoDiscoverArtifacts => false;
        public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];

        protected override Action<KernelChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
        {
            builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
            builder.SiloBuilder.ConfigureServices(services =>
                services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.ObserverState, (provider, _) =>
                    ActivatorUtilities.CreateInstance<SignalingObserverStorage>(provider)));
        };

        async Task Establish()
        {
            _storeName = $"statistics-{Guid.NewGuid():N}";
            (await Services.GetRequiredService<IStorage>().HasEventStore(_storeName)).ShouldBeFalse();
            _services = (IChronicleServicesAccessor)Services.GetRequiredService<IChronicleConnection>();
            _statistics = new StatisticsReader(_services.Services.ReadModels, Services.GetRequiredService<IGrainFactory>(), Services.GetRequiredService<IEventStoreInitialization>());
        }

        async Task Because()
        {
            foreach (var tenant in new[] { "first-tenant", "second-tenant" })
            {
                // Discovery creates storage without an explicit EnsureEventStore or client registration.
                await _services.Services.Observers.GetObservers(new AllObserversRequest { EventStore = _storeName, Namespace = tenant });
                (await Services.GetRequiredService<IStorage>().HasEventStore(_storeName)).ShouldBeTrue();
                InitialRows.AddRange(await ReadNamespace(tenant));
            }

            await AppendAndWait("first-tenant", 2);
            await AppendAndWait("second-tenant", 1);
            FirstNamespaceRows = await ReadNamespace("first-tenant");
            SecondNamespaceRows = await ReadNamespace("second-tenant");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            StoreRows = (await _statistics.GetEventTypeStatisticsForEventStore(_storeName, timeout.Token)).ToArray();
        }

        async Task AppendAndWait(string tenant, int count)
        {
            var store = await ChronicleClient.GetEventStore(_storeName, tenant);
            await store.DiscoverAll();
            await store.RegisterAll();
            (await store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
            EventTypeId = store.EventTypes.GetEventTypeFor(typeof(CustomerNamed)).Id.Value;
            var observerStorage = (SignalingObserverStorage)Services.GetRequiredKeyedService<IGrainStorage>(WellKnownGrainStorageProviders.ObserverState);
            var key = new KernelObserverKey("$system.statistics.event-types", _storeName, tenant, KernelEventSequenceId.Log);
            for (var index = 0; index < count; index++)
            {
                var append = await store.EventLog.Append("customer", new CustomerNamed($"Customer {index}"));
                append.IsSuccess.ShouldBeTrue();
                await observerStorage.WaitForHandled(key, append.SequenceNumber.Value);
            }
        }

        async Task<EventTypeStatistics[]> ReadNamespace(string tenant)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return (await _statistics.GetEventTypeStatistics(_storeName, tenant, timeout.Token)).ToArray();
        }
    }

    [Fact] void should_resolve_statistics_before_client_registration_or_appends() => Context.InitialRows.ShouldBeEmpty();
    [Fact] void should_count_the_first_namespaces_appends() => Context.FirstNamespaceRows.Select(row => (row.EventType, row.Namespace, row.Count)).ShouldEqual([(Context.EventTypeId, "first-tenant", 2)]);
    [Fact] void should_count_the_second_namespaces_appends_independently() => Context.SecondNamespaceRows.Select(row => (row.EventType, row.Namespace, row.Count)).ShouldEqual([(Context.EventTypeId, "second-tenant", 1)]);
    [Fact] void should_include_both_namespaces_in_store_statistics() => Context.StoreRows.OrderBy(row => row.Namespace).Select(row => (row.EventType, row.Namespace, row.Count)).ShouldEqual([(Context.EventTypeId, "first-tenant", 2), (Context.EventTypeId, "second-tenant", 1)]);
}
