// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Expressions.EventValues.for_EventSourceIdExpressionResolver;

public class when_asking_can_resolve_for_event_source_id : Specification
{
    EventSourceIdExpressionResolver resolvers;
    bool result;

    void Establish() => resolvers = new EventSourceIdExpressionResolver();

    void Because() => result = resolvers.CanResolve("$eventSourceId");

    [Fact] void should_be_able_to_resolve() => result.ShouldBeTrue();
}
