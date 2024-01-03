// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;

namespace Aksio.Cratis.Kernel.Projections.Expressions.ModelProperties.for_SetExpressionResolver;

public class when_asking_can_resolve_for_set_expression : Specification
{
    Mock<IEventValueProviderExpressionResolvers> event_value_resolvers;
    SetExpressionResolver resolver;
    bool result;

    void Establish()
    {
        event_value_resolvers = new();
        event_value_resolvers.Setup(_ => _.CanResolve("something")).Returns(true);
        resolver = new(event_value_resolvers.Object);
    }

    void Because() => result = resolver.CanResolve(string.Empty, "something");

    [Fact] void should_ask_event_value_resolvers() => event_value_resolvers.Verify(_ => _.CanResolve("something"), Once);
    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
