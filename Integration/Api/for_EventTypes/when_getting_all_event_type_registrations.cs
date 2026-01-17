// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Api.Events;
using context = Cratis.Chronicle.Integration.Api.for_EventTypes.when_getting_all_event_type_registrations.context;

namespace Cratis.Chronicle.Integration.Api.for_EventTypes;

[Collection(ChronicleCollection.Name)]
public class when_getting_all_event_type_registrations(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.an_http_client(fixture)
    {
        public ISubject<IEnumerable<EventTypeRegistration>> Observable;
        public IEnumerable<EventTypeRegistration> Result;

        async Task Establish()
        {
            await Client.ExecuteCommand("/api/event-store/test-store-registrations/types", new RegisterEventTypes(
            [
                new EventTypeRegistration(
                    new EventType("26f57829-c3a0-45ca-b1fc-2e05e6e54b8e", 1, false),
                    EventTypeOwner.Client,
                    EventTypeSource.Code,
                    """{"type":"object","properties":{"Content":{"type":"string"}}}"""),
                new EventTypeRegistration(
                    new EventType("8f7b4a3c-d2e1-4f9a-b8c7-1d6e3f2a4b5c", 1, false),
                    EventTypeOwner.Client,
                    EventTypeSource.Code,
                    """{"type":"object","properties":{"Value":{"type":"integer"}}}""")
            ]));
        }

        async Task Because()
        {
            Observable = Client.Observe<IEnumerable<EventTypeRegistration>>("/api/event-store/test-store-registrations/types/schemas");
            Result = await Observable.FirstAsync().Timeout(TimeSpan.FromSeconds(10));
        }
    }

    [Fact] void should_return_registered_event_types() => Context.Result.Count().ShouldBeGreaterThanOrEqual(2);

    [Fact] void should_include_first_event_type() => Context.Result.ShouldContain(_ => _.Type.Id == "26f57829-c3a0-45ca-b1fc-2e05e6e54b8e");

    [Fact] void should_include_second_event_type() => Context.Result.ShouldContain(_ => _.Type.Id == "8f7b4a3c-d2e1-4f9a-b8c7-1d6e3f2a4b5c");

    [Fact] void should_have_schema_for_first_event_type() => Context.Result.First(_ => _.Type.Id == "26f57829-c3a0-45ca-b1fc-2e05e6e54b8e").Schema.ShouldNotBeNull();
}
