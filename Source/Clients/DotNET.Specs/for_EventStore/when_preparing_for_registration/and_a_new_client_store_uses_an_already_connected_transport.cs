// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_a_new_client_store_uses_an_already_connected_transport : given.an_explicit_store
{
    ChronicleClient _client;
    IEventStore _created;

    async Task Establish()
    {
        await _connection.Lifecycle.Connected();
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("alias"));
        _client = new(_connection, _options, _artifacts, _provider);
    }

    async Task Because() => _created = await _client.GetEventStore("current", "default");

    void Destroy() => _client.Dispose();

    [Fact] void should_register_the_new_store_without_needing_another_connected_event() => _created.Registration.IsSuccess.ShouldBeTrue();
    [Fact] void should_send_the_new_store_options_snapshot() => _payload.Types.Single().Type.Id.ShouldEqual("alias");
}
