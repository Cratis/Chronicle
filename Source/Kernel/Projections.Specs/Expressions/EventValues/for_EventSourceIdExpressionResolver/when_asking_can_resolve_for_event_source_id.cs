// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Expressions.EventValues.for_EventSourceIdExpressionResolver;

public class when_asking_can_resolve_for_event_source_id : Specification
{
    EventSourceIdExpressionResolver _resolvers;
    bool _result;

    void Establish() => _resolvers = new EventSourceIdExpressionResolver();

    void Because() => _result = _resolvers.CanResolve(WellKnownExpressions.EventSourceId);

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
