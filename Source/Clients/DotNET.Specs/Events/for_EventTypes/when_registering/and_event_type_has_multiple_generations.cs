// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_event_type_has_multiple_generations : given.all_dependencies
{
    [EventType("multi-gen-event", generation: 1)]
    record MultiGenEventV1(string Message);

    [EventType("multi-gen-event", generation: 2)]
    record MultiGenEventV2(string Subject, string Body);

    EventTypes _subject;
    RegisterEventTypesRequest _capturedRequest;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(MultiGenEventV1), typeof(MultiGenEventV2)]);
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

    [Fact] void should_send_one_registration() => _capturedRequest.Types.Count.ShouldEqual(1);
    [Fact] void should_register_both_generation_schemas() => _capturedRequest.Types[0].Generations.Count.ShouldEqual(2);
    [Fact] void should_include_generation_1() => _capturedRequest.Types[0].Generations.ShouldContain(_ => _.Generation == 1);
    [Fact] void should_include_generation_2() => _capturedRequest.Types[0].Generations.ShouldContain(_ => _.Generation == 2);
    [Fact] void should_use_latest_generation_as_type() => _capturedRequest.Types[0].Type.Generation.ShouldEqual(2u);
}
