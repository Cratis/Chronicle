// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Expressions.EventValues.for_EventSourceIdExpressionResolver;

public class when_trying_to_resolve_valid_event_source_id_expression : given.an_appended_event
{
    EventSourceIdExpressionResolver _resolver;
    object _result;

    void Establish() => _resolver = new();

    void Because() => _result = _resolver.Resolve(WellKnownExpressions.EventSourceId)(@event);

    [Fact] void should_resolve_to_a_value_provider_that_gets_event_source_id() => _result.ShouldEqual(@event.Context.EventSourceId.Value);
}
