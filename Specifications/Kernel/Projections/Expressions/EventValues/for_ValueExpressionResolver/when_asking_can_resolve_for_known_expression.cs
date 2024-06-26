// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Expressions.EventValues.for_ValueExpressionResolver;

public class when_asking_can_resolve_for_known_expression : Specification
{
    ValueExpressionResolver resolvers;
    bool result;

    void Establish() => resolvers = new();

    void Because() => result = resolvers.CanResolve("$value(42)");

    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
