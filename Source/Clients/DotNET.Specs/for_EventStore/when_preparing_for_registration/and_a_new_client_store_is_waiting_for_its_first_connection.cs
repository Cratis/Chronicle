// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_a_new_client_store_is_waiting_for_its_first_connection : given.an_explicit_store
{
    ChronicleClient _client;
    IEventStore _created;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("alias"));

        // Connect can return before the first keep-alive arrives; the watchdog continues connecting.
        _connection.Connect().Returns(Task.CompletedTask);
        _client = new(_connection, _options, _artifacts, _provider);
    }

    async Task Because() => _created = await _client.GetEventStore("current", "default");

    void Destroy() => _client.Dispose();

    [Fact] void should_return_the_prepared_store() => _created.EventTypes.GetEventTypeFor(typeof(PlainEvent)).Id.Value.ShouldEqual("alias");
    [Fact] void should_leave_registration_pending() => _created.Registration.HasRun.ShouldBeFalse();
    [Fact] void should_not_start_registration_effects() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
}
