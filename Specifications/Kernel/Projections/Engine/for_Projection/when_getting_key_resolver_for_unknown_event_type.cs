// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Events.Projections.for_Projection;

public class when_getting_key_resolver_for_unknown_event_type : Specification
{
    Projection projection;
    Exception result;

    void Establish()
    {
        projection = new Projection(
            "0b7325dd-7a25-4681-9ab7-c387a6073547",
            string.Empty,
            string.Empty,
            string.Empty,
            new Model(string.Empty, new JsonSchema()),
            true,
            Array.Empty<IProjection>());
    }

    void Because() => result = Catch.Exception(() => projection.GetKeyResolverFor(new("6ffcf259-2069-4e7b-bf60-006edbffaf8b", 1)));

    [Fact] void should_throw_missing_key_resolver_for_event_type() => result.ShouldBeOfExactType<MissingKeyResolverForEventType>();
}
