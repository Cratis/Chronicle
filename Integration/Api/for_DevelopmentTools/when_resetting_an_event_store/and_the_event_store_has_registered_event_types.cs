// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.EventStores;
using context = Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store.and_the_event_store_has_registered_event_types.context;

namespace Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store;

/// <summary>
/// Verifies that registered event types are cleared when an event store is reset.
/// This test passes only when the Chronicle server is built in Development (Debug) mode.
/// </summary>
/// <param name="context">The spec context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_event_store_has_registered_event_types(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public const string EventStoreName = "dev-tools-reset-with-types";
        public const string EventTypeId = "a1b2c3d4-e5f6-7890-abcd-ef1234567801";

        public QueryResult? RegistrationsResult;
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
                        """{"type":"object","properties":{"Value":{"type":"string"}}}""")
                ]));
        }

        async Task Because()
        {
            await Client.PostAsync(
                $"/api/development-tools/reset-event-store/{EventStoreName}",
                content: null);

            // Re-add the store so the registrations endpoint has a known-good store to query against
            await Client.ExecuteCommand("/api/event-stores/add", new AddEventStore(EventStoreName));

            RegistrationsResult = await Client.ExecuteQuery<IEnumerable<EventTypeRegistration>>(
                $"/api/event-store/{EventStoreName}/types/registrations");

            Registrations = RegistrationsResult?.Data as IEnumerable<EventTypeRegistration>;
        }
    }

    [Fact] void should_succeed_querying_registrations_after_reset() =>
        Context.RegistrationsResult!.IsSuccess.ShouldBeTrue();

    [Fact] void should_have_no_registered_event_types_after_reset() =>
        Context.Registrations!.ShouldBeEmpty();
}
