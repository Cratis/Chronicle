// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;
using NJsonSchema;

namespace Cratis.Events.Projections.for_Projection
{
    public class when_getting_key_resolver_for_event_type : Specification
    {
        static EventType event_type = new("993888cc-a9c5-4d56-ae21-f732159feec7", 1);
        Projection projection;
        ValueProvider<Event> expected;
        ValueProvider<Event> result;

        void Establish()
        {
            expected = EventValueProviders.FromEventSourceId;
            projection = new Projection(
                "0b7325dd-7a25-4681-9ab7-c387a6073547",
                string.Empty,
                string.Empty,
                new Model(string.Empty, new JsonSchema()),
                new[] {
                    new EventTypeWithKeyResolver(event_type, expected)
                },
                Array.Empty<IProjection>());
        }

        void Because() => result = projection.GetKeyResolverFor(event_type);

        [Fact] void should_return_the_key_resolver() => result.ShouldEqual(expected);
    }
}
