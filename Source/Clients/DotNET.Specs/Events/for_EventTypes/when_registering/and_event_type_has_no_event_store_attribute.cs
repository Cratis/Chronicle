// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_event_type_has_no_event_store_attribute : given.all_dependencies
{
    [EventType]
    record LocalEvent(string Name);

    EventTypes _subject;
    RegisterEventTypesRequest _capturedRequest;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(LocalEvent)]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);

        _eventTypesService
            .When(_ => _.Register(Arg.Any<RegisterEventTypesRequest>()))
            .Do(call => _capturedRequest = call.Arg<RegisterEventTypesRequest>());
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_have_empty_event_store_in_the_registration() =>
        _capturedRequest.Types[0].EventStore.ShouldEqual(string.Empty);
}
