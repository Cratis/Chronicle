// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.for_Projection;

public class when_getting_key_resolver_for_event_type_registered_as_public : given.a_projection
{
    static EventType event_type = new("993888cc-a9c5-4d56-ae21-f732159feec7", 1, IsPublic: true);
    KeyResolver expected;
    KeyResolver result;

    void Establish()
    {
        expected = KeyResolvers.FromEventSourceId;
        projection.SetEventTypesWithKeyResolvers(
            new EventTypeWithKeyResolver[]
            {
                    new EventTypeWithKeyResolver(event_type, expected)
            },
            new[] { event_type });
    }

    void Because() => result = projection.GetKeyResolverFor(new EventType(event_type.Id, event_type.Generation));

    [Fact] void should_return_the_key_resolver() => result.ShouldEqual(expected);
}
