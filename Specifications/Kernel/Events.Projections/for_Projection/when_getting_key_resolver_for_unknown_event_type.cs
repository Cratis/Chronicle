// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Schema;

namespace Cratis.Events.Projections.for_Projection
{
    public class when_getting_key_resolver_for_unknown_event_type : Specification
    {
        Projection projection;
        Exception result;

        void Establish()
        {
            projection = new Projection(
                "0b7325dd-7a25-4681-9ab7-c387a6073547",
                new Model(string.Empty, new JSchema()),
                Array.Empty<EventTypeWithKeyResolver>());
        }

        void Because() => result = Catch.Exception(() => projection.GetKeyResolverFor("6ffcf259-2069-4e7b-bf60-006edbffaf8b"));

        [Fact] void should_throw_missing_key_resolver_for_event_type() => result.ShouldBeOfExactType<MissingKeyResolverForEventType>();
    }
}
