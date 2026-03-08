// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_ValueExpressionResolver;

public class when_trying_to_resolve_valid_value_expression : given.an_appended_event
{
    ValueExpressionResolver _resolver;
    object _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.Resolve($"{WellKnownExpressions.Value}(42)")(@event);

    [Fact] void should_resolve_to_a_value_provider_that_returns_the_value() => _result.ShouldEqual("42");
}
