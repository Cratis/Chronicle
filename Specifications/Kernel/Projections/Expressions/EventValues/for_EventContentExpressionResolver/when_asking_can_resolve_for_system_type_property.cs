// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Expressions.EventValues.for_EventContentExpressionResolver;

public class when_asking_can_resolve_for_system_type_property : Specification
{
    EventContentExpressionResolver resolvers;
    bool result;

    void Establish() => resolvers = new EventContentExpressionResolver();

    void Because() => result = resolvers.CanResolve("$someProperty");

    [Fact] void should_not_be_able_to_resolve() => result.ShouldBeFalse();
}
