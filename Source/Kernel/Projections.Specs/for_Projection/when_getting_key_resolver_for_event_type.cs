// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection;

public class when_getting_key_resolver_for_event_type : given.a_projection
{
    static EventType event_type = new("993888cc-a9c5-4d56-ae21-f732159feec7", 1);
    KeyResolver expected;
    KeyResolver result;

    void Establish()
    {
        expected = keyResolvers.FromEventSourceId;
        projection.SetEventTypesWithKeyResolvers(
            [
                    new EventTypeWithKeyResolver(event_type, expected)
            ],
            [event_type],
            new Dictionary<EventType, ProjectionOperationType>());
    }

    void Because() => result = projection.GetKeyResolverFor(event_type);

    [Fact] void should_return_the_key_resolver() => result.ShouldEqual(expected);
}
