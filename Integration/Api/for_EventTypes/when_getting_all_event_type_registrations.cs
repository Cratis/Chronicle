// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.EventTypes;
using context = Cratis.Chronicle.Integration.Api.for_EventTypes.when_getting_all_event_type_registrations.context;

namespace Cratis.Chronicle.Integration.Api.for_EventTypes;

[Collection(ChronicleCollection.Name)]
public class when_getting_all_event_type_registrations(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public QueryResult Result;
        public IEnumerable<EventTypeDetails> Data;

        async Task Establish()
        {
            // Every artifact carrying an EventStoreName is checked against the stores that exist, so the store has
            // to be created before anything can be registered in it - the same order a real client connects in.
            await Client.ExecuteCommand("/api/event-stores/ensure-event-store", new EnsureEventStore("test-store-registrations"));

            await Client.ExecuteCommand("/api/event-types/register-event-types", new RegisterEventTypes(
            "test-store-registrations",
            [
                new EventTypeRegistration
                {
                    Type = new EventType { Id = "26f57829-c3a0-45ca-b1fc-2e05e6e54b8e", Generation = 1, Tombstone = false },
                    Owner = EventTypeOwner.Client,
                    Source = EventTypeSource.Code,
                    Schema = """{"type":"object","properties":{"Content":{"type":"string"}}}"""
                },
                new EventTypeRegistration
                {
                    Type = new EventType { Id = "8f7b4a3c-d2e1-4f9a-b8c7-1d6e3f2a4b5c", Generation = 1, Tombstone = false },
                    Owner = EventTypeOwner.Client,
                    Source = EventTypeSource.Code,
                    Schema = """{"type":"object","properties":{"Value":{"type":"integer"}}}"""
                }
            ],
            false));
        }

        async Task Because()
        {
            Result = await Client.ExecuteQuery<IEnumerable<EventTypeDetails>>("/api/event-types/all-event-types?eventStore=test-store-registrations");
            Data = Result.Data as IEnumerable<EventTypeDetails>;
        }
    }

    [Fact] void should_succeed_query() => Context.Result.IsSuccess.ShouldBeTrue();

    [Fact] void should_return_registered_event_types() => Context.Data.Count().ShouldBeGreaterThanOrEqual(2);

    [Fact] void should_include_first_event_type() => Context.Data.ShouldContain(_ => _.Type.Id == "26f57829-c3a0-45ca-b1fc-2e05e6e54b8e");

    [Fact] void should_include_second_event_type() => Context.Data.ShouldContain(_ => _.Type.Id == "8f7b4a3c-d2e1-4f9a-b8c7-1d6e3f2a4b5c");

    [Fact] void should_have_schema_for_first_event_type() => Context.Data.First(_ => _.Type.Id == "26f57829-c3a0-45ca-b1fc-2e05e6e54b8e").Schema.ShouldNotBeNull();
}
