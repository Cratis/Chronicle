// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Integration.for_EventSeeding.when_a_namespace_is_created_after_global_seeding.context;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

[Collection(ChronicleCollection.Name)]
public class when_a_namespace_is_created_after_global_seeding(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        readonly FutureNamespaceSeedObserver _observer = new();
        public IReadOnlyList<AppendedEvent> DefaultEvents;
        public IReadOnlyList<AppendedEvent> FutureEvents;

        public override IEnumerable<Type> EventTypes => [typeof(OfficeOpened)];
        public override IEnumerable<Type> Reactors => [typeof(FutureNamespaceSeedObserver)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(_observer);

        async Task Establish()
        {
            if (ChronicleFixture.Options.Mode == ChronicleRuntimeMode.InProcess)
            {
                // The in-process fixture removes the server startup task. Reproduce its System-store
                // registration here so the durable notification has a schema and a subscribed consumer.
                await Services.GetRequiredService<Cratis.Chronicle.EventTypes.IEventTypes>()
                    .DiscoverAndRegister(Concepts.EventStoreName.System);
                await Services.GetRequiredService<Observation.Reactors.Kernel.IReactors>()
                    .DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default);
            }
        }

        async Task Because()
        {
            EventStore.Namespace.ShouldEqual(EventStoreNamespaceName.Default);
            var source = Guid.NewGuid().ToString();
            EventStore.Seeding.For(source, [new OfficeOpened("Bergen")]);
            await EventStore.Seeding.Register();
            DefaultEvents = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            DefaultEvents.Count.ShouldEqual(1);

            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var globalSeeds = await services.Seeding.GetGlobalSeedData(new Cratis.Chronicle.Contracts.Seeding.GetGlobalSeedDataRequest
            {
                EventStore = EventStore.Name.Value
            }).EnsureSuccess();
            globalSeeds.ByEventSource.Single().Entries.Count.ShouldEqual(1);

            var request = new EnsureNamespaceRequest { EventStore = EventStore.Name.Value, Namespace = "future" };
            var ensured = await services.Namespaces.EnsureNamespace(request);
            ensured.ExceptionMessages.ShouldBeEmpty();
            ensured.IsSuccess.ShouldBeTrue();
            var future = await Services.GetRequiredService<IChronicleClient>().GetEventStore(EventStore.Name, "future");
            await future.WaitForRegistration();

            // Catch-up delivers even a seed appended before the new namespace's client reactor registered.
            await _observer.SeedObserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
            (await services.Namespaces.EnsureNamespace(request)).IsSuccess.ShouldBeTrue();
            FutureEvents = await future.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        }
    }

    [Fact] void should_seed_the_default_namespace() => Context.DefaultEvents.Count.ShouldEqual(1);
    [Fact] void should_seed_the_future_namespace_once() => Context.FutureEvents.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_seeded_content() => Context.FutureEvents.Single().Content.ShouldEqual(new OfficeOpened("Bergen"));
}
