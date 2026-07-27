// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_Projection;

public class when_an_event_type_does_not_resolve_to_the_event_source_id : given.a_projection
{
    static EventType _eventSourceKeyed = new("f1a2b3c4-d5e6-4708-9a1b-2c3d4e5f6a7b", 1);
    static EventType _collapsing = new("0a1b2c3d-4e5f-4061-8273-8495a6b7c8d9", 1);

    void Because() => projection.SetEventTypesWithKeyResolvers(
        [
            new EventTypeWithKeyResolver(_eventSourceKeyed, keyResolvers.FromEventSourceId, ResolvesToEventSourceId: true),
            new EventTypeWithKeyResolver(_collapsing, keyResolvers.FromEventSourceId, ResolvesToEventSourceId: false)
        ],
        [_eventSourceKeyed, _collapsing],
        new Dictionary<EventType, ProjectionOperationType>());

    [Fact] void should_not_be_event_source_keyed() => projection.IsEventSourceKeyed.ShouldBeFalse();
}
