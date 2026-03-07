// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_ValueExpressionResolver;

public class when_asking_can_resolve_for_known_expression : Specification
{
    ValueExpressionResolver _resolvers;
    bool _result;

    void Establish() => _resolvers = new();

    void Because() => _result = _resolvers.CanResolve($"{WellKnownExpressions.Value}(42)");

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
