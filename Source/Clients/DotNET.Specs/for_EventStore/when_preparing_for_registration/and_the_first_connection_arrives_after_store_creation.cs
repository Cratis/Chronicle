// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_the_first_connection_arrives_after_store_creation : given.an_explicit_store
{
    ChronicleClient _client;
    IEventStore _created;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("alias"));
        _connection.Connect().Returns(Task.CompletedTask);
        _client = new(_connection, _options, _artifacts, _provider);
        _created = await _client.GetEventStore("current", "default");
    }

    async Task Because() => await _connection.Lifecycle.Connected();

    void Destroy() => _client.Dispose();

    [Fact] void should_register_the_prepared_store_on_connection() => _created.Registration.IsSuccess.ShouldBeTrue();
    [Fact] void should_send_the_store_options_snapshot() => _payload.Types.Single().Type.Id.ShouldEqual("alias");
}
