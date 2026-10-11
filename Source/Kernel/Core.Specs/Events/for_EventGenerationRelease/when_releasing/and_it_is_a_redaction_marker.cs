// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_it_is_a_redaction_marker : given.a_release_boundary
{
    void Establish() => _event = _event with { Context = _event.Context with { EventType = new(GlobalEventTypes.Redaction, 1) } };

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_keep_the_marker() => _result[0].ShouldEqual(_event);
    [Fact] async Task should_not_read_a_definition() => await _eventTypes.DidNotReceive().GetDefinition(Arg.Any<EventTypeId>());
}
