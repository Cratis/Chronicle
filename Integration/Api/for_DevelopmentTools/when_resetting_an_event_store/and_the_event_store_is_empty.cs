// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Api.EventStores;
using context = Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store.and_the_event_store_is_empty.context;

namespace Cratis.Chronicle.Integration.Api.for_DevelopmentTools.when_resetting_an_event_store;

/// <summary>
/// Verifies that resetting an event store removes it from the system.
/// This test passes only when the Chronicle server is built in Development (Debug) mode.
/// </summary>
/// <param name="context">The spec context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_event_store_is_empty(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public const string EventStoreName = "dev-tools-reset-target";

        public QueryResult? EventStoresAfterReset;
        public IEnumerable<string>? EventStoreNames;

        async Task Establish() =>
            await Client.ExecuteCommand(
                "/api/event-stores/add",
                new AddEventStore(EventStoreName));

        async Task Because()
        {
            await Client.PostAsync(
                $"/api/development-tools/reset-event-store/{EventStoreName}",
                content: null);

            EventStoresAfterReset = await Client.ExecuteQuery<IEnumerable<string>>("/api/event-stores");
            EventStoreNames = EventStoresAfterReset?.Data as IEnumerable<string>;
        }
    }

    [Fact] void should_succeed_querying_event_stores() => Context.EventStoresAfterReset!.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_include_the_reset_event_store() =>
        Context.EventStoreNames.ShouldNotContain(context.EventStoreName);
}
