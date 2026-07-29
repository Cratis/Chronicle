// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_IEventTypesStorage.when_registering_a_batch;

public class and_an_event_type_has_a_single_generation_without_migrations : given.a_storage_without_its_own_batch_registration
{
    IEnumerable<EventTypeId> _result;

    void Establish() =>
        _inner.Register(Arg.Any<EventType>(), Arg.Any<JsonSchema>(), Arg.Any<EventTypeOwner>(), Arg.Any<EventTypeSource>())
            .Returns(true);

    async Task Because() => _result = await _subject.Register([EventTypeToRegisterFor("some-event", [], 1)]);

    [Fact] void should_register_it_as_a_single_event_type() =>
        _inner.Received(1).Register(
            Arg.Is<EventType>(_ => _.Id.Value == "some-event" && _.Generation.Value == 1),
            Arg.Any<JsonSchema>(),
            EventTypeOwner.Client,
            EventTypeSource.Code);
    [Fact] void should_not_register_it_as_a_definition() =>
        _inner.DidNotReceive().Register(Arg.Any<EventTypeDefinition>());
    [Fact] void should_report_it_as_mutated() => _result.ShouldContainOnly(new EventTypeId("some-event"));
}
