// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.EventStores;
using context = Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_all.and_system_is_functional_after_reset.context;

namespace Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_all;

[Collection(ChronicleCollection.Name)]
public class and_system_is_functional_after_reset(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public const string EventStoreName = "reset-all-functional";
        public const string EventTypeId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

        public QueryResult? EventStoresResult;
        public IEnumerable<string>? EventStoreNames;
        public QueryResult? RegistrationsResult;
        public IEnumerable<EventTypeRegistration>? Registrations;

        async Task Establish()
        {
            // Create an event store and populate it before resetting
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

            // Perform the full reset
            await Client.PostAsync("/api/development-tools/reset-all", content: null);
        }

        async Task Because()
        {
            // Re-create the event store and register types again
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

            EventStoresResult = await Client.ExecuteQuery<IEnumerable<string>>("/api/event-stores");
            EventStoreNames = EventStoresResult?.Data as IEnumerable<string>;

            RegistrationsResult = await Client.ExecuteQuery<IEnumerable<EventTypeRegistration>>(
                $"/api/event-store/{EventStoreName}/types/registrations");
            Registrations = RegistrationsResult?.Data as IEnumerable<EventTypeRegistration>;
        }
    }

    [Fact] void should_succeed_querying_event_stores() =>
        Context.EventStoresResult!.IsSuccess.ShouldBeTrue();

    [Fact] void should_include_the_re_created_event_store() =>
        Context.EventStoreNames.ShouldContain(context.EventStoreName);

    [Fact] void should_succeed_querying_registrations() =>
        Context.RegistrationsResult!.IsSuccess.ShouldBeTrue();

    [Fact] void should_have_the_re_registered_event_type() =>
        Context.Registrations!.ShouldContain(_ => _.Type.Id == context.EventTypeId);
}
