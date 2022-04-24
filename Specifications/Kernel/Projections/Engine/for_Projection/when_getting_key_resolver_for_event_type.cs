// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Events.Projections.for_Projection;

public class when_getting_key_resolver_for_event_type : Specification
{
    static EventType event_type = new("993888cc-a9c5-4d56-ae21-f732159feec7", 1);
    Projection projection;
    KeyResolver expected;
    KeyResolver result;

    void Establish()
    {
        expected = KeyResolvers.FromEventSourceId;
        projection = new Projection(
            "0b7325dd-7a25-4681-9ab7-c387a6073547",
            string.Empty,
            string.Empty,
            string.Empty,
            new Model(string.Empty, new JsonSchema()),
            true,
            Array.Empty<IProjection>());
        projection.SetEventTypesWithKeyResolvers(new EventTypeWithKeyResolver[]
        {
                new EventTypeWithKeyResolver(event_type, expected)
        });
    }

    void Because() => result = projection.GetKeyResolverFor(event_type);

    [Fact] void should_return_the_key_resolver() => result.ShouldEqual(expected);
}
