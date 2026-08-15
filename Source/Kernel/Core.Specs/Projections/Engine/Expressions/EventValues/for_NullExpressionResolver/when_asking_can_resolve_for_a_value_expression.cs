// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_NullExpressionResolver;

/// <summary>
/// A clear takes no operand. Claiming a constant would hand every $value(...) to a resolver that ignores what it
/// was given and clears the member instead.
/// </summary>
public class when_asking_can_resolve_for_a_value_expression : Specification
{
    NullExpressionResolver _resolver;
    bool _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.CanResolve($"{WellKnownExpressions.Value}(null)");

    [Fact] void should_not_be_able_to_resolve() => _result.ShouldBeFalse();
}
