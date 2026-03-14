// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.EventStores;
using context = Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store.and_other_event_stores_are_not_affected.context;

namespace Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store;

[Collection(ChronicleCollection.Name)]
public class and_other_event_stores_are_not_affected(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public const string StoreToReset = "isolation-target";
        public const string StoreToKeep = "isolation-survivor";
        public const string SurvivorEventTypeId = "c3d4e5f6-a7b8-9012-cdef-123456789abc";

        public QueryResult? EventStoresResult;
        public IEnumerable<string>? EventStoreNames;
        public QueryResult? SurvivorRegistrationsResult;
        public IEnumerable<EventTypeRegistration>? SurvivorRegistrations;

        async Task Establish()
        {
            // Create two event stores with types
            await Client.ExecuteCommand("/api/event-stores/add", new AddEventStore(StoreToReset));
            await Client.ExecuteCommand("/api/event-stores/add", new AddEventStore(StoreToKeep));

            await Client.ExecuteCommand(
                $"/api/event-store/{StoreToKeep}/types",
                new RegisterEventTypes(
                [
                    new EventTypeRegistration(
                        new EventType(SurvivorEventTypeId, 1, false),
                        EventTypeOwner.Client,
                        EventTypeSource.Code,
                        """{"type":"object","properties":{"Value":{"type":"integer"}}}""")
                ]));
        }

        async Task Because()
        {
            // Reset only one of the two stores
            await Client.PostAsync(
                $"/api/development-tools/reset-event-store/{StoreToReset}",
                content: null);

            EventStoresResult = await Client.ExecuteQuery<IEnumerable<string>>("/api/event-stores");
            EventStoreNames = EventStoresResult?.Data as IEnumerable<string>;

            SurvivorRegistrationsResult = await Client.ExecuteQuery<IEnumerable<EventTypeRegistration>>(
                $"/api/event-store/{StoreToKeep}/types/registrations");
            SurvivorRegistrations = SurvivorRegistrationsResult?.Data as IEnumerable<EventTypeRegistration>;
        }
    }

    [Fact] void should_succeed_querying_event_stores() =>
        Context.EventStoresResult!.IsSuccess.ShouldBeTrue();

    [Fact] void should_not_include_the_reset_event_store() =>
        Context.EventStoreNames.ShouldNotContain(context.StoreToReset);

    [Fact] void should_still_include_the_surviving_event_store() =>
        Context.EventStoreNames.ShouldContain(context.StoreToKeep);

    [Fact] void should_succeed_querying_survivor_registrations() =>
        Context.SurvivorRegistrationsResult!.IsSuccess.ShouldBeTrue();

    [Fact] void should_still_have_the_survivor_event_type() =>
        Context.SurvivorRegistrations!.ShouldContain(_ => _.Type.Id == context.SurvivorEventTypeId);
}
