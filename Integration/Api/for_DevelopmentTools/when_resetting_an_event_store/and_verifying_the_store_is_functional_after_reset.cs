// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.EventStores;
using context = Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store.and_verifying_the_store_is_functional_after_reset.context;

namespace Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store;

[Collection(ChronicleCollection.Name)]
public class and_verifying_the_store_is_functional_after_reset(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public const string EventStoreName = "dev-tools-reset-functional";
        public const string EventTypeId = "b2c3d4e5-f6a7-8901-bcde-f12345678901";

        public QueryResult? RegistrationsAfterResetResult;
        public IEnumerable<EventTypeRegistration>? RegistrationsAfterReset;
        public IEnumerable<string>? EventStoreNamesAfterReset;

        async Task Establish()
        {
            // Populate with initial state
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

            // Perform the reset
            await Client.PostAsync(
                $"/api/development-tools/reset-event-store/{EventStoreName}",
                content: null);
        }

        async Task Because()
        {
            // Re-create and repopulate the store after the reset
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

            RegistrationsAfterResetResult = await Client.ExecuteQuery<IEnumerable<EventTypeRegistration>>(
                $"/api/event-store/{EventStoreName}/types/registrations");

            RegistrationsAfterReset = RegistrationsAfterResetResult?.Data as IEnumerable<EventTypeRegistration>;

            var eventStoresResult = await Client.ExecuteQuery<IEnumerable<string>>("/api/event-stores");
            EventStoreNamesAfterReset = eventStoresResult?.Data as IEnumerable<string>;
        }
    }

    [Fact] void should_succeed_querying_registrations_after_re_create() =>
        Context.RegistrationsAfterResetResult!.IsSuccess.ShouldBeTrue();

    [Fact] void should_have_the_re_registered_event_type() =>
        Context.RegistrationsAfterReset!.ShouldContain(_ => _.Type.Id == context.EventTypeId);

    [Fact] void should_include_the_event_store_in_the_event_stores_list() =>
        Context.EventStoreNamesAfterReset.ShouldContain(context.EventStoreName);
}
