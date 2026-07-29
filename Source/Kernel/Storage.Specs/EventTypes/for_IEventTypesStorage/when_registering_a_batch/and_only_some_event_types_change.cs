// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes.for_IEventTypesStorage.when_registering_a_batch;

public class and_only_some_event_types_change : given.a_storage_without_its_own_batch_registration
{
    IEnumerable<EventTypeId> _result;

    void Establish() =>
        _inner.Register(Arg.Any<EventType>(), Arg.Any<JsonSchema>(), Arg.Any<EventTypeOwner>(), Arg.Any<EventTypeSource>())
            .Returns(callInfo => callInfo.ArgAt<EventType>(0).Id.Value == "changed-event");

    async Task Because() => _result = await _subject.Register(
    [
        EventTypeToRegisterFor("changed-event", [], 1),
        EventTypeToRegisterFor("unchanged-event", [], 1)
    ]);

    [Fact] void should_only_report_the_changed_event_type() => _result.ShouldContainOnly(new EventTypeId("changed-event"));
}
