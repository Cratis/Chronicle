// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_Projection;

public class when_all_event_types_resolve_to_the_event_source_id : given.a_projection
{
    static EventType _eventType = new("b0c8d4a2-1e3f-4a5b-8c6d-7e9f0a1b2c3d", 1);

    void Because() => projection.SetEventTypesWithKeyResolvers(
        [
            new EventTypeWithKeyResolver(_eventType, keyResolvers.FromEventSourceId, ResolvesToEventSourceId: true)
        ],
        [_eventType],
        new Dictionary<EventType, ProjectionOperationType>());

    [Fact] void should_be_event_source_keyed() => projection.IsEventSourceKeyed.ShouldBeTrue();
}
