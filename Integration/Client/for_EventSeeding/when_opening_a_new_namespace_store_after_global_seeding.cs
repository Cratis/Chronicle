// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Integration.for_EventSeeding.when_opening_a_new_namespace_store_after_global_seeding.context;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

[Collection(ChronicleCollection.Name)]
public class when_opening_a_new_namespace_store_after_global_seeding(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        readonly FutureNamespaceSeedObserver _observer = new();
        public IReadOnlyList<AppendedEvent> FutureEvents;

        public override IEnumerable<Type> EventTypes => [typeof(OfficeOpened)];
        public override IEnumerable<Type> Reactors => [typeof(FutureNamespaceSeedObserver)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(_observer);

        async Task Because()
        {
            EventStore.Seeding.For("global-office", [new OfficeOpened("Bergen")]);
            EventStore.Seeding.ForNamespace(EventStoreNamespaceName.Default).For("sample-office", [new OfficeOpened("Sample")]);
            await EventStore.Seeding.Register();
            (await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(2);

            var client = Services.GetRequiredService<IChronicleClient>();
            var future = await client.GetEventStore(EventStore.Name, "future");
            await future.WaitForRegistration();
            await _observer.SeedObserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await future.RegisterAll();
            FutureEvents = await future.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        }
    }

    [Fact] void should_apply_the_global_seed_once_without_an_application_write() => Context.FutureEvents.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_global_seed() => Context.FutureEvents.Single().Content.ShouldEqual(new OfficeOpened("Bergen"));
    [Fact] void should_not_apply_the_default_namespaces_sample_seed() => Context.FutureEvents.Any(_ => _.Content.Equals(new OfficeOpened("Sample"))).ShouldBeFalse();
}
