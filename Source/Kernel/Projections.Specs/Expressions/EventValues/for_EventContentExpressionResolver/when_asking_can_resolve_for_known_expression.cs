// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Expressions.EventValues.for_EventContentExpressionResolver;

public class when_asking_can_resolve_for_known_expression : Specification
{
    EventContentExpressionResolver _resolvers;
    bool _result;

    void Establish() => _resolvers = new EventContentExpressionResolver();

    void Because() => _result = _resolvers.CanResolve("SomeProperty");

    [Fact] void should_be_able_to_resolve() => _result.ShouldBeTrue();
}
