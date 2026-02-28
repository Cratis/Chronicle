// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_Projection;

public class when_getting_key_resolver_for_event_type : given.a_projection
{
    static EventType _eventType = new("993888cc-a9c5-4d56-ae21-f732159feec7", 1);
    KeyResolver _expected;
    KeyResolver _result;

    void Establish()
    {
        _expected = keyResolvers.FromEventSourceId;
        projection.SetEventTypesWithKeyResolvers(
            [
                    new EventTypeWithKeyResolver(_eventType, _expected)
            ],
            [_eventType],
            new Dictionary<EventType, ProjectionOperationType>());
    }

    void Because() => _result = projection.GetKeyResolverFor(_eventType);

    [Fact] void should_return_the_key_resolver() => _result.ShouldEqual(_expected);
}
