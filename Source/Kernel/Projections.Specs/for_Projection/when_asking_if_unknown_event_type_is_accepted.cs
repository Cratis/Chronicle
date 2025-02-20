// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection;

public class when_asking_if_unknown_event_type_is_accepted : given.a_projection
{
    static EventType event_type = new("993888cc-a9c5-4d56-ae21-f732159feec7", 1);
    bool result;

    void Establish()
    {
        projection.SetEventTypesWithKeyResolvers(
            [
                    new EventTypeWithKeyResolver(event_type, keyResolvers.FromEventSourceId)
            ],
            [event_type],
            new Dictionary<EventType, ProjectionOperationType>());
    }

    void Because() => result = projection.Accepts(new EventType("5f6e10c6-f687-4e1c-b9a9-4007810c48da", 1));

    [Fact] void should_not_accept_it() => result.ShouldBeFalse();
}
