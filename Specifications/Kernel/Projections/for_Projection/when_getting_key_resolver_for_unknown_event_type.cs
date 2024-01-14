// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.for_Projection;

public class when_getting_key_resolver_for_unknown_event_type : given.a_projection
{
    Exception result;

    void Because() => result = Catch.Exception(() => projection.GetKeyResolverFor(new("6ffcf259-2069-4e7b-bf60-006edbffaf8b", 1)));

    [Fact] void should_throw_missing_key_resolver_for_event_type() => result.ShouldBeOfExactType<MissingKeyResolverForEventType>();
}
