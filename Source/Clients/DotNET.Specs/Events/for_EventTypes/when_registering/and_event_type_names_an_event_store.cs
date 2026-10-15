// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_event_type_names_an_event_store : given.all_dependencies
{
    [EventType]
    [EventStore("some-service")]
    record TheEvent(string Name);

    EventTypes _subject;
    RegisterEventTypesRequest _capturedRequest;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(TheEvent)]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);

        _eventTypesService
            .When(_ => _.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()))
            .Do(call => _capturedRequest = call.Arg<RegisterEventTypesRequest>());
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_register_it_as_public_for_the_named_event_store() =>
        _capturedRequest.Types.ElementAt(0).Visibility.ShouldEqual(Contracts.Events.EventTypeVisibility.Public);
}
