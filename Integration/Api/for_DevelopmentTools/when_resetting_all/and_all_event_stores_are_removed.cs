// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.EventStores;
using context = Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_all.and_all_event_stores_are_removed.context;

namespace Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_all;

[Collection(ChronicleCollection.Name)]
public class and_all_event_stores_are_removed(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public const string EventStoreName = "reset-all-data-wipe";
        public const string EventTypeId = "d1e2f3a4-b5c6-7890-abcd-ef0011223344";

        public QueryResult? EventStoresAfterReset;
        public IEnumerable<string>? EventStoreNames;
        public QueryResult? RegistrationsAfterReset;
        public IEnumerable<EventTypeRegistration>? Registrations;

        async Task Establish()
        {
            await Client.ExecuteCommand("/api/event-stores/add", new AddEventStore(EventStoreName));
            await Client.ExecuteCommand(
                $"/api/event-store/{EventStoreName}/types",
                new RegisterEventTypes(
                [
                    new EventTypeRegistration(
                        new EventType(EventTypeId, 1, false),
                        EventTypeOwner.Client,
                        EventTypeSource.Code,
                        """{"type":"object","properties":{"Name":{"type":"string"}}}""")
                ]));
        }

        async Task Because()
        {
            await Client.PostAsync("/api/development-tools/reset-all", content: null);

            EventStoresAfterReset = await Client.ExecuteQuery<IEnumerable<string>>("/api/event-stores");
            EventStoreNames = EventStoresAfterReset?.Data as IEnumerable<string>;

            // Re-create the store to query its type registrations (databases were dropped)
            await Client.ExecuteCommand("/api/event-stores/add", new AddEventStore(EventStoreName));
            RegistrationsAfterReset = await Client.ExecuteQuery<IEnumerable<EventTypeRegistration>>(
                $"/api/event-store/{EventStoreName}/types/registrations");
            Registrations = RegistrationsAfterReset?.Data as IEnumerable<EventTypeRegistration>;
        }
    }

    [Fact] void should_succeed_querying_event_stores() => Context.EventStoresAfterReset!.IsSuccess.ShouldBeTrue();

    [Fact] void should_have_system_event_store_after_reset() =>
        Context.EventStoreNames.ShouldContain("System");

    [Fact] void should_succeed_querying_registrations() =>
        Context.RegistrationsAfterReset!.IsSuccess.ShouldBeTrue();

    [Fact] void should_have_no_event_type_registrations_after_reset() =>
        Context.Registrations!.Where(_ => _.Type.Id == context.EventTypeId).ShouldBeEmpty();
}
