// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.EventStores;
using context = Cratis.Chronicle.Integration.Api.for_EventStoreQueries.when_getting_event_stores.context;

namespace Cratis.Chronicle.Integration.Api.for_EventStoreQueries;

[Collection(ChronicleCollection.Name)]
public class when_getting_event_stores(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public QueryResult Result;
        public IEnumerable<EventStoreNames> Data;

        Task Establish() => Client.ExecuteCommand("/api/event-stores/ensure-event-store", new EnsureEventStore("testing"));

        async Task Because()
        {
            Result = await Client.ExecuteQuery<IEnumerable<EventStoreNames>>("/api/event-stores/all-event-stores");
            Data = Result.Data as IEnumerable<EventStoreNames>;
        }
    }

    [Fact] void should_succeed_query() => Context.Result.IsSuccess.ShouldBeTrue();

    [Fact] void should_return_system_event_store() => Context.Data.Select(_ => _.Name).ShouldContain(EventStoreName.System.Value);

    // Other specs in this collection ensure their own event stores and the fixture does not wipe them between specs,
    // so the list holds every event store created so far - not only the one this spec ensured.
    [Fact] void should_return_event_store_ensured_by_the_spec() => Context.Data.Select(_ => _.Name).ShouldContain("testing");
}
