// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;

namespace Aksio.Cratis.Kernel.Projections.Expressions.ModelProperties.for_SubtractExpressionResolver;

public class when_asking_can_resolve_for_add_expression : Specification
{
    Mock<IEventValueProviderExpressionResolvers> event_value_resolvers;
    SubtractExpressionResolver resolver;
    bool result;

    void Establish()
    {
        event_value_resolvers = new();
        resolver = new(event_value_resolvers.Object);
    }

    void Because() => result = resolver.CanResolve(string.Empty, "$subtract(something)");

    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
