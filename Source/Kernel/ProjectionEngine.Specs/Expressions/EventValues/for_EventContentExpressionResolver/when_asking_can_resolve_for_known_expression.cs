// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEngine.Expressions.EventValues.for_EventContentExpressionResolver;

public class when_asking_can_resolve_for_known_expression : Specification
{
    EventContentExpressionResolver resolvers;
    bool result;

    void Establish() => resolvers = new EventContentExpressionResolver();

    void Because() => result = resolvers.CanResolve("someProperty");

    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
