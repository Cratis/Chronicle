// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_NullExpressionResolver;

public class when_trying_to_resolve_the_null_expression : given.an_appended_event
{
    NullExpressionResolver _resolver;
    object _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.Resolve(WellKnownExpressions.Null)(@event);

    [Fact] void should_resolve_to_a_value_provider_that_returns_no_value() => _result.ShouldBeNull();
}
