// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_IEventTypesStorage.when_registering_a_batch;

public class and_an_event_type_has_migrations : given.a_storage_without_its_own_batch_registration
{
    async Task Because() =>
        await _subject.Register(
        [
            EventTypeToRegisterFor(
                "some-event",
                [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject())],
                1,
                2)
        ]);

    [Fact] void should_register_it_as_a_definition() =>
        _inner.Received(1).Register(Arg.Is<EventTypeDefinition>(_ => _.Id.Value == "some-event"));
    [Fact] void should_not_register_it_as_a_single_event_type() =>
        _inner.DidNotReceive().Register(
            Arg.Any<EventType>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<EventTypeOwner>(),
            Arg.Any<EventTypeSource>());
}
