// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypes.when_discovering;

public class and_multiple_generations_of_same_event_type_exist : given.all_dependencies
{
    [EventType("multi-gen-event", generation: 1)]
    record MultiGenEventV1(string Message);

    [EventType("multi-gen-event", generation: 2)]
    record MultiGenEventV2(string Subject, string Body);

    EventTypes _subject;

    void Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(MultiGenEventV1), typeof(MultiGenEventV2)]);
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because() => await _subject.Discover();

    [Fact] void should_not_throw() => true.ShouldBeTrue();
    [Fact] void should_have_both_generations() => _subject.All.Count.ShouldEqual(2);
    [Fact] void should_have_generation_1() => _subject.All.ShouldContain(_ => _.Id.Value == "multi-gen-event" && _.Generation.Value == 1);
    [Fact] void should_have_generation_2() => _subject.All.ShouldContain(_ => _.Id.Value == "multi-gen-event" && _.Generation.Value == 2);
    [Fact] void should_have_both_clr_types() => _subject.AllClrTypes.Count.ShouldEqual(2);
}
