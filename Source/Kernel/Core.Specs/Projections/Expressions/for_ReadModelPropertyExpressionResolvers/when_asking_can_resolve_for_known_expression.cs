// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Expressions.for_ReadModelPropertyExpressionResolvers;

public class when_asking_can_resolve_for_known_expression : given.read_model_property_expression_resolvers
{
    bool _result;

    void Because() => _result = _resolvers.CanResolve(string.Empty, $"{WellKnownExpressions.Add}({WellKnownExpressions.EventSourceId})");

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
