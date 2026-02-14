// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Expressions.EventValues.for_EventContextPropertyExpressionResolver;

public class when_trying_to_resolve_valid_event_context_expression : given.an_appended_event
{
    EventContextPropertyExpressionResolver _resolver;
    object _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.Resolve($"{WellKnownExpressions.EventContext}(Occurred)")(@event);

    [Fact] void should_resolve_to_a_value_provider_that_gets_value_from_event_context() => ((DateTimeOffset)_result).ShouldEqual(occurred);
}
