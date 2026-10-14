// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Integration.for_EventSeeding.when_a_global_declaration_is_cleared_before_a_new_namespace.context;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

[Collection(ChronicleCollection.Name)]
public class when_a_global_declaration_is_cleared_before_a_new_namespace(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        readonly ClearedDeclarationSeedObserver _observer = new();
        public IReadOnlyList<AppendedEvent> ExistingEvents;
        public IReadOnlyList<AppendedEvent> FutureEvents;

        public override IEnumerable<Type> EventTypes => [typeof(OfficeOpened)];
        public override IEnumerable<Type> Reactors => [typeof(ClearedDeclarationSeedObserver)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(_observer);

        async Task Because()
        {
            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var entry = new Cratis.Chronicle.Contracts.Seeding.SeedingEntry
            {
                EventSourceId = "removed-office",
                EventTypeId = EventStore.EventTypes.GetEventTypeFor(typeof(OfficeOpened)).Id.Value,
                Content = JsonSerializer.Serialize(new OfficeOpened("Removed"), ChronicleClient.Options.JsonSerializerOptions),
                Tags = [],
                EventSourceType = EventSourceType.Default.Value,
                EventStreamType = EventStreamType.All.Value,
                EventStreamId = EventStreamId.Default
            };
            await services.Seeding.SeedEvents(new SeedEventsRequest
            {
                EventStore = EventStore.Name,
                GlobalByEventType = [new EventTypeSeedEntries { EventTypeId = entry.EventTypeId, Entries = [entry] }],
                GlobalByEventSource = [new EventSourceSeedEntries { EventSourceId = entry.EventSourceId, Entries = [entry] }],
                NamespacedEntries = [],
                GlobalSetIsAuthoritative = true,
                GlobalSetOwner = "seed-program"
            }).EnsureSuccess();

            // Observe the declared seed before clearing it, including if namespace creation applies it asynchronously.
            await _observer.RemovedSeedObserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
            (await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(1);

            await services.Seeding.SeedEvents(new SeedEventsRequest
            {
                EventStore = EventStore.Name,
                GlobalByEventType = [],
                GlobalByEventSource = [],
                NamespacedEntries = [],
                GlobalSetIsAuthoritative = true,
                GlobalSetOwner = "seed-program"
            }).EnsureSuccess();
            var stored = await services.Seeding.GetGlobalSeedData(new GetGlobalSeedDataRequest { EventStore = EventStore.Name }).EnsureSuccess();
            stored.ByEventSource.ShouldBeEmpty();

            // Retain a positive control: observing this seed proves the new namespace's asynchronous
            // global seeding completed before asserting that the removed seed did not arrive.
            EventStore.Seeding.For("retained-office", [new OfficeOpened("Retained")]);
            await EventStore.Seeding.Register();
            var future = await Services.GetRequiredService<IChronicleClient>().GetEventStore(EventStore.Name, "future");
            await future.WaitForRegistration();
            await _observer.RetainedSeedObserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
            FutureEvents = await future.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            ExistingEvents = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        }
    }

    [Fact] void should_preserve_the_already_appended_event() => Context.ExistingEvents.Select(_ => _.Content).ShouldContain(new OfficeOpened("Removed"));
    [Fact] void should_apply_only_the_remaining_seed_to_the_new_namespace() => Context.FutureEvents.Select(_ => _.Content).ShouldContainOnly([new OfficeOpened("Retained")]);
}
