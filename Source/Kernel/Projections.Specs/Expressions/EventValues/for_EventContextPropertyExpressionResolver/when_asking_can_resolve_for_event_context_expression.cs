// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Expressions.EventValues.for_EventContextPropertyExpressionResolver;

public class when_asking_can_resolve_for_event_context_expression : Specification
{
    EventContextPropertyExpressionResolver _resolver;
    bool _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.CanResolve($"{WellKnownExpressions.EventContext}(Something)");

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
